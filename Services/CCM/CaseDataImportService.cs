using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PEXC.Case.DataAccess;
using PEXC.Case.DataAccess.CosmosDB;
using PEXC.Case.Domain;
using PEXC.Case.Services.CCM.Contracts;
using PEXC.Case.Services.Infrastructure;
using PEXC.Common.Logging.Extensions;
using PEXC.Case.Services.IRIS;
using PEXC.Case.Services.Staffing;

namespace PEXC.Case.Services.CCM;

public class CaseDataImportService : ICaseDataImportService
{
    private static readonly UserInfo ServiceUserInfo = new(UserType.Service, nameof(CaseDataImportService));
    private static readonly IReadOnlyCollection<Expression<Func<CaseEntity, dynamic?>>> CCMProperties =
        new List<Expression<Func<CaseEntity, dynamic?>>>
        {
            e => e.PrimaryIndustry,
            e => e.PrimaryCapability,
            e => e.ManagingOffice,
            e => e.ClientHeadEcode,
            e => e.ManagerEcode,
            e => e.BillingPartnerEcode,
            e => e.CaseName,
            e => e.ClientId,
            e => e.ClientName,
            e => e.StartDate,
            e => e.EndDate,
            e => e.LeadKnowledgeSpecialistEcode,
            e => e.OperatingPartnerEcodes,
            e => e.AdvisorsEcodes,
        };

    private readonly IClientCaseApiService _ccmService;
    private readonly IMapper _mapper;
    private readonly IDataImportStateRepository<CaseDataImportState> _caseDataImportStateRepository;
    private readonly ISingleCaseRepository _caseRepository;
    private readonly IIrisIntegrationService _irisIntegrationService;
    private readonly IStaffingApiService _staffingApiService;
    private readonly ILogger<CaseDataImportService> _logger;
    private readonly CaseDataImportOptions _options;

    public CaseDataImportService(
        IOptions<CaseDataImportOptions> options,
        IClientCaseApiService ccmService,
        IMapper mapper,
        IDataImportStateRepository<CaseDataImportState> caseDataImportStateRepository,
        ISingleCaseRepository caseRepository,
        IIrisIntegrationService irisIntegrationService,
        IStaffingApiService staffingApiService,
        ILogger<CaseDataImportService> logger)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        _options = options.Value;
        _ccmService = ccmService ?? throw new ArgumentNullException(nameof(ccmService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _caseDataImportStateRepository = caseDataImportStateRepository ?? throw new ArgumentNullException(nameof(caseDataImportStateRepository));
        _caseRepository = caseRepository ?? throw new ArgumentNullException(nameof(caseRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _irisIntegrationService = irisIntegrationService ?? throw new ArgumentNullException(nameof(irisIntegrationService));
        _staffingApiService = staffingApiService ?? throw new ArgumentNullException(nameof(staffingApiService));
    }

    public async Task<(IReadOnlyCollection<string> UpdatedCaseCodes, IReadOnlyCollection<string> CreatedCaseCodes)> ImportCasesByCaseCodes(IReadOnlyCollection<string> caseCodes)
    {
        var caseCodesString = string.Join(';', caseCodes);
        var updatedCaseCodes = new List<string>();
        var createdCaseCodes = new List<string>();

        try
        {
            _logger.LogInformation("Querying CCM service for Cases with case codes: [{caseCodes}]", caseCodesString);

            var ccmCases = FilterNonPexcCases(await _ccmService.GetCasesByCaseCodes(caseCodes));
            var operatingPartners = await LoadAdditionalData(ccmCases);
            await SaveCases(ccmCases, operatingPartners, updatedCaseCodes, createdCaseCodes);

            _logger.LogInformation(
                "Import finished for Cases with case codes: {caseCodes}.\n" +
                "Updated {updatedCasesCount} Cases [{updatedCaseCodes}]. Created {createdCasesCount} Cases [{createdCaseCodes}].",
                caseCodesString,
                updatedCaseCodes.Count,
                string.Join(';', updatedCaseCodes),
                createdCaseCodes.Count,
                string.Join(';', createdCaseCodes));
            return (updatedCaseCodes, createdCaseCodes);
        }
        catch (Exception ex)
        {
            _logger.LogErrorWithTelemetry(
                ex,
                "Import for Cases with case codes: [{caseCodes}] failed with error: {error}.\n" +
                "Updated {updatedCasesCount} Cases ({updatedCaseCodes}). Created {createdCasesCount} Cases ({createdCaseCodes}).",
                caseCodesString,
                ex.Message,
                updatedCaseCodes.Count,
                string.Join(';', updatedCaseCodes),
                createdCaseCodes.Count,
                string.Join(';', createdCaseCodes));
            throw;
        }
    }

    public async Task ImportCases()
    {
        var lastImportState = await _caseDataImportStateRepository.GetState();

        var newImportState = new CaseDataImportState
        {
            OperationId = Activity.Current?.RootId,
            LastExecutionTime = DateTime.UtcNow,
            LastModifiedAfter = GenerateNewModifiedAfter(lastImportState)
        };

        try
        {
            _logger.LogInformation(
                "Querying CCM service for Cases modified after: {modifiedAfter:O}",
                newImportState.LastModifiedAfter);

            var casesFromCcm = await GetCasesFromCcm(newImportState.LastModifiedAfter!.Value);
            var ccmCases = FilterNonPexcCases(
                FilterAlreadyProcessed(casesFromCcm, lastImportState?.LastSuccessfulExecutionTime)
            );
            var operatingPartners = await LoadAdditionalData(ccmCases);

            await SaveCases(ccmCases, operatingPartners, newImportState.UpdatedCases, newImportState.CreatedCases);

            newImportState.LastSuccessfulExecutionTime = newImportState.LastExecutionTime;
            newImportState.LastSuccessfulModifiedAfter = newImportState.LastModifiedAfter;

            _logger.LogInformation(
                "Import finished for Cases modified after: {modifiedAfter:O}.\n" +
                "Updated {updatedCasesCount} Cases [{updatedCaseCodes}]. Created {createdCasesCount} Cases [{createdCaseCodes}].",
                newImportState.LastModifiedAfter,
                newImportState.UpdatedCases.Count,
                string.Join(';', newImportState.UpdatedCases),
                newImportState.CreatedCases.Count,
                string.Join(';', newImportState.CreatedCases));
        }
        catch (Exception ex)
        {
            _logger.LogErrorWithTelemetry(
                ex,
                "Import for Cases modified after: {modifiedAfter:O} failed with error: {error}.\n" +
                "Updated {updatedCasesCount} Cases ({updatedCaseCodes}). Created {createdCasesCount} Cases ({createdCaseCodes}).",
                newImportState.LastModifiedAfter,
                ex.Message,
                newImportState.UpdatedCases.Count,
                string.Join(';', newImportState.UpdatedCases),
                newImportState.CreatedCases.Count,
                string.Join(';', newImportState.CreatedCases));

            newImportState.Failed = true;
            newImportState.FailedAttempts = (lastImportState?.FailedAttempts ?? 0) + 1;
            newImportState.ErrorMessage = ex.Message;
            newImportState.LastSuccessfulExecutionTime = lastImportState?.LastSuccessfulExecutionTime;
            newImportState.LastSuccessfulModifiedAfter = lastImportState?.LastSuccessfulModifiedAfter;

            throw;
        }
        finally
        {
            await _caseDataImportStateRepository.UpdateState(newImportState);
        }
    }

    private async Task<IReadOnlyDictionary<string, CaseTeamMembers>> LoadAdditionalData(IReadOnlyCollection<CaseDetailsDto> ccmCases)
    {
        if (!ccmCases.Any())
        {
            _logger.LogInformation("No cases to process");
            return new Dictionary<string, CaseTeamMembers>();
        }

        _logger.LogInformation("Getting Knowledge Specialists from IRIS");
        await UpdateKnowledgeSpecialists(ccmCases);
        _logger.LogInformation("Knowledge Specialists retrieved from IRIS");

        _logger.LogInformation("Getting Operating Partner Allocations from Staffing API");
        var operatingPartners = await GetOperatingPartners(ccmCases);
        _logger.LogInformation("Operating Partner Allocations retrieved from Staffing API");

        return operatingPartners;
    }

    private Task<IReadOnlyDictionary<string, CaseTeamMembers>> GetOperatingPartners(IEnumerable<CaseDetailsDto> ccmCases) 
        => _staffingApiService.GetCasesTeamMembers(ccmCases.Select(c => c.CaseCode).ToList());

    private async Task<IReadOnlyCollection<CaseDetailsDto>> GetCasesFromCcm(DateOnly modifiedAfter)
    {
        var ccmCases = (await _ccmService.GetAllCasesModifiedAfter(modifiedAfter)).ToList();

        _logger.LogInformation(
            "Found {ccmResultsCount} CCM cases modified after: {modifiedAfter:O}. Cases : [{cases}]",
            ccmCases.Count,
            modifiedAfter,
            string.Join(';', ccmCases.Select(item => item.CaseCode)));

        return ccmCases;
    }

    private IReadOnlyCollection<CaseDetailsDto> FilterNonPexcCases(IReadOnlyCollection<CaseDetailsDto> ccmEntities)
    {
        var validator = new CcmImportedEntityValidator(_options);
        return ccmEntities.Where(ccmCase => IsValid(_mapper.Map<CaseEntity>(ccmCase), validator)).ToList();
    }

    /// <summary>
    /// CCM can only return cases based on date.
    /// If I modified case 12/21/2022 3:00 PM it will be returned only with CCM call for 12/20/2022
    /// If I want to sync cases more often than once a day, I need to check LastUpdated property
    /// CCM works in EST time zone
    /// </summary>
    private IReadOnlyCollection<CaseDetailsDto> FilterAlreadyProcessed(
        IReadOnlyCollection<CaseDetailsDto> ccmCases, 
        DateTime? lastSuccessfulExecutionTime) 
    {
        if (lastSuccessfulExecutionTime == null)
        {
            return ccmCases;
        }

        var ccmTimeZone = TimeZoneInfo.FindSystemTimeZoneById(_options.CCMTimeZone);
        var ccmCasesToUpdate =
            ccmCases
                .Where(c =>
                    c.LastUpdated == null ||
                    TimeZoneInfo.ConvertTimeToUtc(c.LastUpdated.Value, ccmTimeZone) > lastSuccessfulExecutionTime)
                .ToList();

        if (ccmCases.Count != ccmCasesToUpdate.Count)
        {
            _logger.LogInformation(
                "Some of the cases has been already handled at last import successful execution time {lastSuccessfulExecutionTime}. Cases to update: [{cases}]",
                lastSuccessfulExecutionTime,
                string.Join(';', ccmCasesToUpdate.Select(c => c.CaseCode)));
        }

        return ccmCasesToUpdate;
    }
    
    private DateOnly GenerateNewModifiedAfter(CaseDataImportState? lastImportState)
    {
        if (lastImportState == null)
            return _options.InitialModifiedAfterTime;

        return lastImportState.Failed
            ? lastImportState.LastModifiedAfter!.Value
            : DateOnly.FromDateTime(lastImportState.LastExecutionTime!.Value.AddDays(-1));
    }

    private async Task UpdateKnowledgeSpecialists(IEnumerable<CaseDetailsDto> cases)
    {
        var casesDictionary = cases.ToDictionary(c => c.CaseCode);
        var irisCases = await _irisIntegrationService.GetCasesByCaseCodes(casesDictionary.Keys.ToList());

        foreach (var irisCaseDto in irisCases)
        {
            if (casesDictionary.TryGetValue(irisCaseDto.CaseCode, out var ccmCase))
            {
                ccmCase.LeadKnowledgeSpecialistEcode = irisCaseDto.LeadKnowledgeSpecialist;
            }
        }
    }

    private async Task SaveCases(
        IEnumerable<CaseDetailsDto> ccmCases,
        IReadOnlyDictionary<string, CaseTeamMembers> operatingPartners,
        ICollection<string> updatedCaseCodes,
        ICollection<string> createdCaseCodes)
    {
        foreach (var ccmCase in ccmCases)
        {
            var ccmEntity = _mapper.Map<CaseEntity>(ccmCase);
            var caseRoles = operatingPartners.GetValueOrDefault(ccmCase.CaseCode);
            ccmEntity.OperatingPartnerEcodes = caseRoles?.OperatingPartners;
            ccmEntity.AdvisorsEcodes= caseRoles?.Advisors;
            ccmEntity.CreatedBy = ServiceUserInfo;
            ccmEntity.Created = DateTime.UtcNow;
            ccmEntity.ModifiedBy = ServiceUserInfo;
            ccmEntity.Modified = DateTime.UtcNow;

            var existingEntity = await _caseRepository.GetNonRetainerCaseByCaseCode(ccmCase.CaseCode);

            if (existingEntity != null)
            {
                _logger.LogInformation(
                    "Found existing Case with CaseCode {caseCode}. Updating with CCM data...",
                    existingEntity.CaseCode);

                // Doing manual partial mapping and calling Update instead of Patch due to Cosmos DB API Patch limitations.
                // https://learn.microsoft.com/en-us/azure/cosmos-db/partial-document-update-faq#is-there-a-limit-to-the-number-of-partial-document-update-operations-
                var anyPropertyChanged = MapOnlyCcmPropertiesIfChanged(ccmEntity, existingEntity);
                if (!anyPropertyChanged)
                {
                    _logger.LogInformation(
                        "No important properties updated for Case with CaseCode {caseCode}. Skipping the update.",
                        existingEntity.CaseCode);
                    continue;
                }

                await _caseRepository.UpdateCase(existingEntity);
                updatedCaseCodes.Add(existingEntity.CaseCode);
            }
            else
            {
                _logger.LogInformation(
                    "CCM case with CaseCode {caseCode} not found in Cosmos DB. Creating new entity.",
                    ccmEntity.CaseCode);
                await _caseRepository.AddCase(ccmEntity);
                createdCaseCodes.Add(ccmEntity.CaseCode);
            }
        }
    }

    private static bool MapOnlyCcmPropertiesIfChanged(CaseEntity ccmEntity, CaseEntity existingEntity)
    {
        existingEntity.ModifiedBy = ccmEntity.ModifiedBy;
        existingEntity.Modified = ccmEntity.Modified;

        return CCMProperties.Aggregate(
            false,
            (anyChanged, property)
                => anyChanged | CopyValueIfDifferent(ccmEntity, existingEntity, property));
    }

    private static bool CopyValueIfDifferent<TProperty>(
        CaseEntity sourceEntity,
        CaseEntity targetEntity,
        Expression<Func<CaseEntity, TProperty?>> propertyExpression)
    {
        var property = (PropertyInfo)(propertyExpression.Body.NodeType == ExpressionType.Convert
            ? (MemberExpression)((UnaryExpression)propertyExpression.Body).Operand
            : (MemberExpression)propertyExpression.Body).Member;

        var newValue = property.GetValue(sourceEntity);
        var oldValue = property.GetValue(targetEntity);
        if (Equals(newValue, oldValue))
        {
            return false;
        }

        if (property.PropertyType.IsAssignableTo(typeof(IEnumerable<string>)) &&
            newValue != null &&
            oldValue != null &&
            ((IEnumerable<string>)newValue).SequenceEqual((IEnumerable<string>)oldValue, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        property.SetValue(targetEntity, newValue, null);
        return true;
    }

    private bool IsValid(CaseEntity entity, CcmImportedEntityValidator validator)
    {
        var validationResult = validator.Validate(entity);

        if (validationResult.IsValid)
        {
            return true;
        }

        var errors = validationResult.Errors.Where(item => item.Severity == Severity.Error).ToList();
        if (errors.Any())
        {
            _logger.LogErrorWithTelemetry(
                new ValidationException(validationResult.Errors),
                "Case with {caseCode} skipped during import - for the reasons : [{reasons}]",
                entity.CaseCode,
                string.Join(Environment.NewLine, errors.Select(e => e.ErrorMessage)));
        }

        var information = validationResult.Errors.Where(item => item.Severity == Severity.Info).ToList();
        if (information.Any())
        {
            _logger.LogInformation(
                "Case with {caseCode} skipped during import - for the reasons : [{reasons}]",
                entity.CaseCode,
                string.Join(Environment.NewLine, information.Select(e => e.ErrorMessage)));
        }

        return false;
    }
}