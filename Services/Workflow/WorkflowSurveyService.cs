using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PEXC.Case.DataAccess;
using PEXC.Case.DataContracts;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;
using PEXC.Case.Services.Infrastructure;
using PEXC.Common.Logging.Extensions;
using PEXC.Common.ServiceBus.Contracts;
using PEXC.Document.Client;
using PEXC.Document.DataContracts.V1;
using PEXC.MailDistribution;
using PEXC.MailDistribution.Contracts;
using PEXC.Document.Client.Constants;
using static PEXC.Case.Services.Workflow.CaseDocumentHelper;
using PermissionScope = PEXC.Case.Domain.PermissionScope;

namespace PEXC.Case.Services.Workflow;

public class WorkflowSurveyService : IWorkflowSurveyService
{
    private readonly IDocumentServiceClient _documentService;
    private readonly ISingleCaseRepository _caseRepository;
    private readonly WorkflowSurveyOptions _options;
    private readonly IPermissionService _permissionService;
    private readonly IMailDistributionService _mailDistributionService;
    private readonly IProfileMapper _profileMapper;
    private readonly ITaxonomyServiceFactory _taxonomyServiceFactory;
    private readonly IEventDistributionService _eventDistributionService;
    private readonly ILogger<WorkflowSurveyService> _logger;

    public WorkflowSurveyService(
        IDocumentServiceClient documentService,
        ISingleCaseRepository caseRepository,
        IOptions<WorkflowSurveyOptions> options,
        IPermissionService permissionService,
        IMailDistributionService mailDistributionService,
        IProfileMapper profileMapper,
        ITaxonomyServiceFactory taxonomyServiceFactory,
        IEventDistributionService eventDistributionService,
        ILogger<WorkflowSurveyService> logger)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _caseRepository = caseRepository ?? throw new ArgumentNullException(nameof(caseRepository));
        _options = options.Value;
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _mailDistributionService = mailDistributionService ?? throw new ArgumentNullException(nameof(mailDistributionService));
        _profileMapper = profileMapper ?? throw new ArgumentNullException(nameof(profileMapper));
        _taxonomyServiceFactory = taxonomyServiceFactory ?? throw new ArgumentNullException(nameof(taxonomyServiceFactory));
        _eventDistributionService = eventDistributionService ?? throw new ArgumentNullException(nameof(eventDistributionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task TriggerSurvey(CaseEntity caseEntity, UserInfo serviceUserInfo, string correlationId)
    {
        _logger.LogInformation("Opening scheduled survey..");
        await PatchCaseWithState(
            caseEntity.Id,
            caseEntity.Key,
            CaseState.SurveyOpened,
            serviceUserInfo,
            correlationId);
        _logger.LogInformation("Scheduled survey opened.");
    }

    public async Task ScheduleSurvey(CaseEntity caseEntity, UserInfo serviceUserInfo, string correlationId)
    {
        var properties = new Dictionary<string, object?>
        {
            { LoggerConsts.CorrelationIdProperty, correlationId},
            { LoggerConsts.IdProperty, caseEntity.Id },
            { LoggerConsts.CaseCodeProperty, caseEntity.CaseName },
            { nameof(CaseEntity.EndDate) , caseEntity.EndDate},
            { nameof(CaseEntity.ETag) , caseEntity.ETag},
            { nameof(CaseEntity.Timestamp) , caseEntity.Timestamp},
        };

        var _ = _logger.BeginScope(properties);

        _logger.LogInformation("Scheduling event...");

        var messageDto = new AsbMessageDto(
            Guid.TryParse(correlationId, out var correlationIdParsed) ? correlationIdParsed : Guid.Empty,
            caseEntity);

        var sequenceNumber = await _eventDistributionService.ScheduleEvent(
            messageDto,
            _options.TriggerSurveyQueue,
            new DateTimeOffset(caseEntity.EndDate!.Value, TimeSpan.Zero),
            properties, 
            Utils.TypeAwareSerialize
        );

        _logger.LogInformation("Event scheduled. SequenceNumber: {sequenceNumber}", sequenceNumber);
    }

    public async Task StartSurvey(CaseEntity caseEntity, UserInfo serviceUserInfo, string correlationId)
    {
        await PatchCaseWithState(caseEntity.Id, caseEntity.Key, CaseState.SurveyOpening, serviceUserInfo,
            correlationId);
        _logger.LogInformation("Opening a survey");

        var createDirectoryInfo =
            new CreateDirectoryDto(
                CreateDirectoryName(caseEntity.CaseCode, caseEntity.CaseName!, caseEntity.UniqueId));

        var directoryInfo = await _documentService.CreateDirectory(createDirectoryInfo, correlationId);

        _logger.LogInformation(
            "SharePoint directory has been created. DirectoryInfo:{directoryInfo}",
            directoryInfo);

        var isConfidentialCase = IsConfidentialCase(caseEntity);
        
        var state = (isConfidentialCase, caseEntity.EndDate) switch
        {
            (true, { }) => CaseState.Published,
            (false, { } endDate) when endDate.ToUniversalTime() <= DateTime.UtcNow => CaseState.SurveyOpened,
            (false, { } endDate) when endDate.ToUniversalTime() > DateTime.UtcNow => CaseState.SurveyScheduled,
            (_, _) => throw new NotSupportedException($"Cannot set state for {isConfidentialCase} , {caseEntity.EndDate}")
        };

        await PatchCaseWithState(
            caseEntity.Id,
            caseEntity.Key,
            state,
            serviceUserInfo,
            correlationId,
            new KeyValuePair<string, object?>(nameof(CaseEntity.SharePointDirectory).ToCamelCase(), directoryInfo),
            new KeyValuePair<string, object?>(nameof(CaseEntity.FinalMaterialAvailable).ToCamelCase(),
                isConfidentialCase
                    ? FinalMaterialAvailable.NAConfidentialCase
                    : FinalMaterialAvailable.NoPendingSubmission));

        _logger.LogInformation(state switch
        {
            CaseState.SurveyScheduled => "Survey has enddate set in future, set in scheduled state",
            CaseState.Published => "Processing confidential case. Survey will not be started and SharePoint directory will not be created. Closing the case.",
            _ => "Survey has been opened",
        });
    }

    public async Task CloseSurvey(CaseEntity caseEntity, UserInfo serviceUserInfo, string correlationId)
    {
        _logger.LogInformation("Closing the survey...");

        var permissions = caseEntity.Permissions ?? new List<Permission>();
        await RemovePermission(
            caseEntity.SharePointDirectory!,
            permissions,
            caseEntity.ManagerEcode!,
            PermissionType.SurveyAccess,
            correlationId);

        var finalMaterialAvailable = caseEntity.FinalMaterialAvailable;
        if (ShouldChangeFinalMaterialWhenClosingSurvey(caseEntity))
        {
            finalMaterialAvailable = FinalMaterialAvailable.Yes;
        }

        _logger.LogInformation("Adding a 'SurveyClosed' e-mail to the queue");
        await QueueSurveyClosedEmail(caseEntity, correlationId);
        _logger.LogInformation("'SurveyClosed' e-mail has been added to the queue");

        _logger.LogInformation("Updating case data...");
        await PatchCaseWithState(
            caseEntity.Id,
            caseEntity.Key,
            CaseState.SurveyClosed,
            serviceUserInfo,
            correlationId,
            new KeyValuePair<string, object?>(nameof(CaseEntity.FinalMaterialAvailable).ToCamelCase(), finalMaterialAvailable),
            new KeyValuePair<string, object?>(nameof(CaseEntity.Permissions).ToCamelCase(), permissions));

        _logger.LogInformation("Survey has been closed.");
    }

    public async Task UpdateSurvey(CaseEntity caseEntity, UserInfo serviceUserInfo, string correlationId)
    {
        _logger.LogInformation("Updating the survey.");
        if (IsDeletedCaseWithActiveSharePointAccess(caseEntity))
        {
            await CloseSurveyForDeletedCase(caseEntity, serviceUserInfo, correlationId);
        }
        else if (ShouldGrantPermissionForCaseManager(caseEntity))
        {
            await GrantPermissionForCaseManagerAndSendEmail(caseEntity, serviceUserInfo, correlationId);
        }
        else
        {
            _logger.LogInformation("Case Manager has not changed");
        }

        _logger.LogInformation("Survey has been updated.");
    }

    public Task<CaseEntity?> GetCase(string caseId, string key) => _caseRepository.GetCase(caseId, key);

    private static bool IsDeletedCaseWithActiveSharePointAccess(CaseEntity e)
        => e.IsDeleted
           && e.Permissions?
               .FirstOrDefault(p => p.IsActive && p.PermissionType == PermissionType.SurveyAccess) != null;

    private static bool ShouldGrantPermissionForCaseManager(CaseEntity e) =>
        !e.IsDeleted && !string.Equals(e.Permissions
            ?.FirstOrDefault(p => p.IsActive && p.PermissionType == PermissionType.SurveyAccess)
            ?.EmployeeEcode, e.ManagerEcode, StringComparison.OrdinalIgnoreCase);

    private async Task<ICollection<Permission>> RemoveActiveSharePointAccess(CaseEntity caseEntity, string correlationId)
    {
        _logger.LogInformation("Removing permission for the case manager");
        var permissions = caseEntity.Permissions ?? new List<Permission>();
        var permissionToRemove = permissions
            .FirstOrDefault(p => p.IsActive && p.PermissionType == PermissionType.SurveyAccess);

        if (permissionToRemove != null)
        {
            await RemovePermission(
                caseEntity.SharePointDirectory!,
                permissions,
                permissionToRemove.EmployeeEcode,
                PermissionType.SurveyAccess,
                correlationId);
        }
        _logger.LogInformation("The permission for the case manager has been removed");
        return permissions;
    }

    private async Task CloseSurveyForDeletedCase(CaseEntity caseEntity, UserInfo serviceUserInfo, string correlationId)
    {
        var permissions = await RemoveActiveSharePointAccess(caseEntity, correlationId);

        await PatchCase(
            caseEntity.Id,
            caseEntity.Key,
            serviceUserInfo,
            correlationId,
            new Dictionary<string, object?>
            {
                { nameof(CaseEntity.Permissions).ToCamelCase(), permissions },
            });
        _logger.LogInformation("Case Manager permission to the SP directory for deleted case has been removed.");
    }

    private async Task GrantPermissionForCaseManagerAndSendEmail(CaseEntity caseEntity, UserInfo serviceUserInfo, string correlationId)
    {
        _logger.LogInformation("Granting permission for the case manager");

        var permissions = await RemoveActiveSharePointAccess(caseEntity, correlationId);
        var grantedPermission = await GrantPermissionsToDirectory(
            caseEntity.ManagerEcode!,
            caseEntity.SharePointDirectory!.DriveId,
            caseEntity.SharePointDirectory!.DirectoryId,
            PermissionType.SurveyAccess,
            correlationId);

        if (grantedPermission != null
            && permissions.FirstOrDefault(p =>
                p.PermissionId == grantedPermission.PermissionId
                && p.IsActive && p.PermissionType == PermissionType.SurveyAccess) == null)
        {
            permissions.Add(grantedPermission);
        }

        _logger.LogInformation("Adding a 'SurveyOpened' e-mail to the queue");
        await QueueSurveyOpenedEmail(caseEntity, correlationId);
        _logger.LogInformation("'SurveyOpened' e-mail has been added to the queue");

        await PatchCase(
            caseEntity.Id,
            caseEntity.Key,
            serviceUserInfo,
            correlationId,
            new Dictionary<string, object?> { { nameof(CaseEntity.Permissions).ToCamelCase(), permissions } });

        _logger.LogInformation("The permission for the case manager has been granted");
    }

    private static bool ShouldChangeFinalMaterialWhenClosingSurvey(CaseEntity caseEntity)
        => caseEntity.FinalMaterialAvailable == FinalMaterialAvailable.NoPendingSubmission &&
           caseEntity.DataConfirmation;

    private async Task RemovePermission(
        SharePointDirectoryEntity sharePointDirectory,
        List<Permission> permissions,
        string employeeCode,
        PermissionType permissionType,
        string correlationId)
    {
        _logger.LogInformation(
            "Removing permission to directory for employeeCode:{employeeCode}. DirectoryInfo:{sharePointDirectory}",
            employeeCode, sharePointDirectory);

        var permission = permissions
            .FirstOrDefault(p => p.EmployeeEcode == employeeCode && p.IsActive && p.PermissionType == permissionType);

        if (permission == null)
        {
            _logger.LogInformation("The permission for the employee was not found.");
            return;
        }

        if (permission.PermissionScope == PermissionScope.Group)
        {
            permissions[permissions.IndexOf(permission)] = permission with { IsActive = false };
            _logger.LogInformation(
                "The permission for the user will not be removed because it results from belonging to a group. Permission:{permission}",
                permission);

            return;
        }

        await _permissionService
            .RemovePermission(
                sharePointDirectory.DriveId,
                sharePointDirectory.DirectoryId,
                permission.PermissionId,
                correlationId);

        permissions[permissions.IndexOf(permission)] = permission with { IsActive = false };
        _logger.LogInformation("Permission has been removed. Permission:{permission}", permission);
    }

    private async Task<Permission?> GrantPermissionsToDirectory(
        string employeeCode,
        string driveId,
        string directoryId,
        PermissionType permissionType,
        string correlationId)
    {
        var permission = await _permissionService
            .GrantPermission(employeeCode, driveId, directoryId, PermissionLevels.Edit, correlationId);

        if (permission == null)
        {
            throw new Exception($"The permission for the employee has not been granted. EmployeeCode: {employeeCode}");
        }

        _logger.LogInformation(
            "Permission has been granted. EmployeeCode: {eCode} Permission: {permission}",
            employeeCode,
            permission);

        return new Permission(permission.PermissionId, employeeCode, (PermissionScope)permission.PermissionScope, permissionType, DateTime.UtcNow, true);
    }

    private async Task QueueSurveyOpenedEmail(CaseEntity caseEntity, string correlationId)
    {
        var eCodesToCheck = !string.IsNullOrEmpty(caseEntity.BillingPartnerEcode)
            ? new[] { caseEntity.ManagerEcode!, caseEntity.BillingPartnerEcode! }
            : new[] { caseEntity.ManagerEcode! };

        var employeeProfiles =
            await _profileMapper.GetEmployeeProfiles(eCodesToCheck, correlationId);

        var caseManagerProfile = employeeProfiles[caseEntity.ManagerEcode!];
        var caseManagerName = $"{caseManagerProfile.FirstName} {caseManagerProfile.LastName}";
        var (region, supportEmail) = await GetRegionAndSupportMailbox(caseEntity);
        var templateParameters = BuildSurveyTemplateParameters(caseEntity, caseManagerName, region);
        
        var ccRecipients = !string.IsNullOrEmpty(caseEntity.BillingPartnerEcode) &&
                           employeeProfiles.ContainsKey(caseEntity.BillingPartnerEcode) &&
                           !string.IsNullOrEmpty(employeeProfiles[caseEntity.BillingPartnerEcode].Email)
            ? new[] { employeeProfiles[caseEntity.BillingPartnerEcode].Email!, supportEmail }
            : new[] { supportEmail };

        await AddEmailToQueue(
            EmailTemplateType.SurveyOpened,
            templateParameters,
            new[] { caseManagerProfile.Email! },
            ccRecipients,
            correlationId);
    }

    private Dictionary<string, string?> BuildSurveyTemplateParameters(
        CaseEntity caseEntity,
        string caseManagerName,
        string? region) =>
        new()
        {
            { nameof(CaseEntity.ClientName), caseEntity.ClientName },
            { nameof(CaseEntity.CaseName), caseEntity.CaseName },
            { nameof(CaseEntity.CaseCode), caseEntity.CaseCode },
            { "Region", region },
            { "CaseManager", caseManagerName },
            { nameof(CaseEntity.Key), caseEntity.Key },
            { nameof(CaseEntity.Id), caseEntity.Id },
        };

    private async Task QueueSurveyClosedEmail(CaseEntity caseEntity, string correlationId)
    {
        var eCodesToCheck = !string.IsNullOrEmpty(caseEntity.LeadKnowledgeSpecialistEcode)
            ? new[] { caseEntity.ManagerEcode!, caseEntity.LeadKnowledgeSpecialistEcode }
            : new[] { caseEntity.ManagerEcode! };   

        var employeeProfiles =
            await _profileMapper.GetEmployeeProfiles(eCodesToCheck, correlationId);

        var caseManagerProfile = employeeProfiles[caseEntity.ManagerEcode!];
        var caseManagerName = $"{caseManagerProfile.FirstName} {caseManagerProfile.LastName}";
        var (region, supportEmail) = await GetRegionAndSupportMailbox(caseEntity);
        var templateParameters = BuildSurveyTemplateParameters(caseEntity, caseManagerName, region);

        var ccRecipients = !string.IsNullOrEmpty(caseEntity.LeadKnowledgeSpecialistEcode)
                           && employeeProfiles.TryGetValue(caseEntity.LeadKnowledgeSpecialistEcode, out var leadKnowledgeSpecialistProfile)
            ? new[] { leadKnowledgeSpecialistProfile.Email! }
            : Array.Empty<string>();

        await AddEmailToQueue(
            EmailTemplateType.SurveyClosed,
            templateParameters,
            new[] { supportEmail },
            ccRecipients,
            correlationId);
    }

    private Task AddEmailToQueue(
        EmailTemplateType templateType,
        IReadOnlyDictionary<string, string?> templateParameters,
        IReadOnlyList<string> recipients,
        IReadOnlyList<string> ccRecipients,
        string correlationId)
    {
        var emailDto = new EmailMessageDto(
            templateType,
            templateParameters,
            recipients,
            ccRecipients,
            correlationId);

        _logger
            .LogInformation("Adding an e-mail to sending queue. " +
                            "TemplateType: {templateType} " +
                            "TemplateParameters: [{templateParameters}] " +
                            "Recipients: [{recipients}] " +
                            "CCRecipients: [{ccRecipients}]",
                emailDto.TemplateType,
                string.Join(",", emailDto
                    .TemplateParameters.Select(k => $"[{k.Key}:{k.Value}]")),
                string.Join(",", emailDto.Recipients),
                string.Join(",", emailDto.CcRecipients));

        return _mailDistributionService.AddEmailToQueue(emailDto);
    }

    private async Task<(string? region, string supportEmail)> GetRegionAndSupportMailbox(CaseEntity caseEntity)
    {
        var taxonomy = await _taxonomyServiceFactory.Create();
        var region = taxonomy.MapOfficeTaxonomy(caseEntity.ManagingOffice)?.Region ?? string.Empty;
        var supportEmail = _mailDistributionService.GetSupportMailboxByRegion(region);

        return (region, supportEmail);
    }

    private async Task PatchCaseWithState(
        string caseId,
        string key,
        CaseState caseState,
        UserInfo serviceUserInfo,
        string correlationId,
        params KeyValuePair<string, object?>[] additionalProperties)
    {
        var propertiesToUpdate = new Dictionary<string, object?>(additionalProperties)
        {
            { nameof(CaseEntity.ItemStage).ToCamelCase(), caseState },
        };

        await PatchCase(
            caseId, 
            key, 
            serviceUserInfo, 
            correlationId,
            propertiesToUpdate);
    }

    private async Task PatchCase(
        string caseId,
        string key,
        UserInfo serviceUserInfo,
        string correlationId,
        IDictionary<string, object?> additionalProperties)
    {
        var propertiesToUpdate = new Dictionary<string, object?>(additionalProperties)
        {
            { nameof(CaseEntity.Modified).ToCamelCase(), DateTime.UtcNow },
            { nameof(CaseEntity.ModifiedBy).ToCamelCase(), serviceUserInfo },
            { nameof(CaseEntity.CorrelationId).ToCamelCase(), correlationId }
        };

        await _caseRepository.PatchCase(caseId, key, propertiesToUpdate);
    }

    private bool IsConfidentialCase(CaseEntity entity)
        => _options.ConfidentialCapabilities.Contains(entity.PrimaryCapability!.Id!.Value);
}