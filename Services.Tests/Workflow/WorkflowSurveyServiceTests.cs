using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;
using PEXC.Case.DataAccess;
using PEXC.Case.DataContracts;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;
using PEXC.Case.Services.Infrastructure;
using PEXC.Case.Services.Workflow;
using PEXC.Case.Tests.Common;
using PEXC.Common.BaseApi.Profile;
using PEXC.Common.Logging.Extensions;
using PEXC.Common.ServiceBus.Contracts;
using PEXC.Common.Taxonomy.DataContracts;
using PEXC.Common.Taxonomy;
using PEXC.Document.Client;
using PEXC.Document.DataContracts.V1;
using PEXC.MailDistribution;
using PEXC.MailDistribution.Contracts;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;
using PermissionScope = PEXC.Document.DataContracts.V1.PermissionScope;
using PEXC.Document.Client.Constants;

namespace PEXC.Case.Services.Tests.Workflow;

public class WorkflowSurveyServiceTests
{
    private static readonly UserInfo ServiceUserInfo = new(UserType.Service, "Start Survey Handler");

    [Fact]
    public async Task TriggerSurvey_StatusChangedToOpened()
    {
        var caseEntity = Fake.CaseEntity();
        var caseRepository = GetCaseRepository_WithPatchConfiguration(caseEntity);
        var surveyService = GetService(
            null,
            caseRepository,
            null);
        
        var result = surveyService.TriggerSurvey(caseEntity, ServiceUserInfo, "correlationId");
        await result;

        result.IsCompletedSuccessfully.Should().BeTrue();
        await caseRepository.PatchCase(
            Arg.Is(caseEntity.Id),
            Arg.Is(caseEntity.Key),
            Arg.Is<IReadOnlyDictionary<string, object?>>(
                d => 
                     d.Contains(
                         new KeyValuePair<string, object?>(
                             nameof(CaseEntity.ItemStage).ToCamelCase(),
                             CaseState.SurveyOpened))
                     && d.Contains(
                         new KeyValuePair<string, object?>(
                             nameof(CaseEntity.CorrelationId).ToCamelCase(),
                             "correlationId"))));
    }

    [Fact]
    public async Task ScheduleSurvey_MessageIsSent()
    {
        var eventDistributor = Substitute.For<IEventDistributionService>();
        var surveyService = GetService(
            null,
            null, 
            null,
            null!,
            null,
            new WorkflowSurveyOptions() { TriggerSurveyQueue = nameof(WorkflowSurveyOptions.TriggerSurveyQueue) },
            eventDistributor);

        var caseEntity = Fake.CaseEntity();
        var result = surveyService.ScheduleSurvey(caseEntity, ServiceUserInfo, "correlationId");
        await result;
        
        result.IsCompletedSuccessfully.Should().BeTrue();
        await eventDistributor.Received().ScheduleEvent(
            Arg.Is<AsbMessageDto>(
                (t) => t.Entity.Id == caseEntity.Id),
            nameof(WorkflowSurveyOptions.TriggerSurveyQueue),
            Arg.Is<DateTimeOffset>(d => d.DateTime == caseEntity.EndDate),
            Arg.Is<IReadOnlyDictionary<string, object>>(p => p[LoggerConsts.CorrelationIdProperty].ToString() == "correlationId"),
            Utils.TypeAwareSerialize);
    }

    [Fact]
    public async Task StartSurvey_CaseIsConfidential_SurveyNotStartedAndMaterialsSetToConfidential()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        caseEntity.PrimaryCapability = new TaxonomyItem(55, "confidential capability");
        var expectedResult = new DirectoryInfoDto("driveId", "directoryId", "url");
        var permission = new DirectoryPermissionDto("permId", "a@bain.com", PermissionScope.User);
        var documentService = GetDocumentService_ReturningDirectoryInfo(expectedResult);
        var caseRepository = GetCaseRepository_WithPatchConfiguration(caseEntity);
        var permissionService = GetPermissionService(permission);

        var surveyService = GetService(
            documentService,
            caseRepository,
            permissionService,
            optionsValue: new WorkflowSurveyOptions
                { ConfidentialCapabilities = new[] { caseEntity.PrimaryCapability.Id!.Value } });

        // Act
        var result = surveyService.StartSurvey(caseEntity, ServiceUserInfo, "correlationId");
        await result;

        // Assert
        result.IsCompletedSuccessfully.Should().BeTrue();
        await caseRepository
            .Received(1)
            .PatchCase(
                Arg.Is(caseEntity.Id),
                Arg.Is(caseEntity.Key),
                Arg.Is<IReadOnlyDictionary<string, object?>>(
                    d => d.Contains(
                             new KeyValuePair<string, object?>(
                                 nameof(CaseEntity.FinalMaterialAvailable).ToCamelCase(),
                                 FinalMaterialAvailable.NAConfidentialCase)) &&
                         d.Contains(
                             new KeyValuePair<string, object?>(
                                 nameof(CaseEntity.ItemStage).ToCamelCase(),
                                 CaseState.Published))
                         && d.Contains(
                             new KeyValuePair<string, object?>(
                                 nameof(CaseEntity.CorrelationId).ToCamelCase(),
                                 "correlationId"))));

        await documentService
            .Received()
            .CreateDirectory(Arg.Any<CreateDirectoryDto>(), Arg.Any<string>());
    }

    [Theory]
    [InlineData(-1, CaseState.SurveyOpened)]
    [InlineData(1, CaseState.SurveyScheduled)]
    public async Task StartSurvey_SurveyStartedAndMaterialsSetToPendingSubmission(int daysToModifyEndDate, CaseState desiredCaseState)
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        caseEntity.EndDate = DateTime.UtcNow.AddDays(daysToModifyEndDate);

        var expectedResult = new DirectoryInfoDto("driveId", "directoryId", "url");
        var permission = new DirectoryPermissionDto("permId", "a@bain.com", PermissionScope.User);
        var documentService = GetDocumentService_ReturningDirectoryInfo(expectedResult);
        var caseRepository = GetCaseRepository_WithPatchConfiguration(caseEntity);
        var permissionService = GetPermissionService(permission);
        var profileMapper = GetProfileMapper_ReturningProfiles(
            new List<string> { caseEntity.ManagerEcode!, caseEntity.BillingPartnerEcode! });

        var surveyService = GetService(
            documentService,
            caseRepository,
            permissionService,
            profileMapper);

        // Act
        var result = surveyService.StartSurvey(caseEntity, ServiceUserInfo, "correlationId");
        await result;

        // Assert
        result.IsCompletedSuccessfully.Should().BeTrue();
        await caseRepository
            .Received()
            .PatchCase(
                Arg.Is(caseEntity.Id),
                Arg.Is(caseEntity.Key),
                Arg.Is<IReadOnlyDictionary<string, object?>>(
                    d => d.Contains(
                             new KeyValuePair<string, object?>(
                                 nameof(CaseEntity.ItemStage).ToCamelCase(),
                                 CaseState.SurveyOpening))
                         && d.Contains(
                             new KeyValuePair<string, object?>(
                                 nameof(CaseEntity.CorrelationId).ToCamelCase(),
                                 "correlationId"))));
        await caseRepository
            .Received()
            .PatchCase(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Is<IReadOnlyDictionary<string, object?>>(
                    d => d.Contains(
                             new KeyValuePair<string, object?>(
                                 nameof(CaseEntity.FinalMaterialAvailable).ToCamelCase(),
                                 FinalMaterialAvailable.NoPendingSubmission)) &&
                         d.Contains(
                             new KeyValuePair<string, object?>(
                                 nameof(CaseEntity.ItemStage).ToCamelCase(),
                                 desiredCaseState)) &&
                         d[nameof(CaseEntity.CorrelationId).ToCamelCase()]!.ToString() == "correlationId"));

        await documentService
            .Received()
            .CreateDirectory(Arg.Any<CreateDirectoryDto>(), Arg.Any<string>());
    }

    [Fact]
    public async Task StartSurvey_DocumentServiceThrowException()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        var permission = new DirectoryPermissionDto("permId", "a@bain.com", PermissionScope.User);
        var caseRepository = GetCaseRepository_WithPatchConfiguration(caseEntity);
        var permissionService = GetPermissionService(permission);
        var documentService = Substitute.For<IDocumentServiceClient>();
        documentService
            .CreateDirectory(Arg.Any<CreateDirectoryDto>(), Arg.Any<string>())
            .ThrowsAsync(new Exception("message"));

        var surveyService = GetService(
            documentService,
            caseRepository,
            permissionService);

        // Act
        Func<Task> act = async () => { await surveyService.StartSurvey(caseEntity, ServiceUserInfo, "correlationId"); };

        //Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task CloseSurvey_SurveyClosedAndMaterialsSetToYes()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        caseEntity.DataConfirmation = true;
        caseEntity.FinalMaterialAvailable = FinalMaterialAvailable.NoPendingSubmission;

        var documentService = Substitute.For<IDocumentServiceClient>();
        var caseRepository = GetCaseRepository_WithPatchConfiguration(caseEntity);
        var permissionService = Substitute.For<IPermissionService>();
        var profileMapper = GetProfileMapper_ReturningProfiles(
            new List<string> { caseEntity.ManagerEcode!, caseEntity.LeadKnowledgeSpecialistEcode! });

        var surveyService = GetService(
            documentService,
            caseRepository,
            permissionService,
            profileMapper);

        // Act
        var result = surveyService.CloseSurvey(caseEntity, ServiceUserInfo, "correlationId");
        await result;

        // Assert
        result.IsCompletedSuccessfully.Should().BeTrue();
        await caseRepository
            .Received()
            .PatchCase(
                Arg.Is(caseEntity.Id),
                Arg.Is(caseEntity.Key),
                Arg.Is<IReadOnlyDictionary<string, object?>>(
                    d => d.Contains(
                             new KeyValuePair<string, object?>(
                                 nameof(CaseEntity.FinalMaterialAvailable).ToCamelCase(),
                                 FinalMaterialAvailable.Yes)) &&
                         d.Contains(
                             new KeyValuePair<string, object?>(
                                 nameof(CaseEntity.ItemStage).ToCamelCase(),
                                 CaseState.SurveyClosed))
                         && d.Contains(
                             new KeyValuePair<string, object?>(
                                 nameof(CaseEntity.CorrelationId).ToCamelCase(),
                                 "correlationId"))));
    }

    [Theory]
    [InlineData(FinalMaterialAvailable.NACdd, true)]
    [InlineData(FinalMaterialAvailable.NAConfidentialCase, true)]
    [InlineData(FinalMaterialAvailable.NADuplicateCase, true)]
    [InlineData(FinalMaterialAvailable.NAIncompleteCase, true)]
    [InlineData(FinalMaterialAvailable.NoActiveCase, true)]
    [InlineData(FinalMaterialAvailable.Yes, true)]
    [InlineData(FinalMaterialAvailable.NoPendingSubmission, false)]
    public async Task CloseSurvey_SurveyClosedAndMaterialsNotChanged(FinalMaterialAvailable finalMaterial,
        bool dataConfirmation)
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        caseEntity.DataConfirmation = dataConfirmation;
        caseEntity.FinalMaterialAvailable = finalMaterial;

        var documentService = Substitute.For<IDocumentServiceClient>();
        var caseRepository = GetCaseRepository_WithPatchConfiguration(caseEntity);
        var permissionService = Substitute.For<IPermissionService>();
        var profileMapper = GetProfileMapper_ReturningProfiles(
            new List<string> { caseEntity.ManagerEcode!, caseEntity.LeadKnowledgeSpecialistEcode! });

        var surveyService = GetService(
            documentService,
            caseRepository,
            permissionService,
            profileMapper);

        // Act
        var result = surveyService.CloseSurvey(caseEntity, ServiceUserInfo, "correlationId");
        await result;

        // Assert
        result.IsCompletedSuccessfully.Should().BeTrue();
        await caseRepository
            .Received()
            .PatchCase(
                Arg.Is(caseEntity.Id),
                Arg.Is(caseEntity.Key),
                Arg.Is<IReadOnlyDictionary<string, object?>>(
                    d => d.Contains(
                             new KeyValuePair<string, object?>(
                                 nameof(CaseEntity.FinalMaterialAvailable).ToCamelCase(),
                                 finalMaterial))
                         && d.Contains(
                             new KeyValuePair<string, object?>(
                                 nameof(CaseEntity.CorrelationId).ToCamelCase(),
                                 "correlationId"))));
    }

    [Fact]
    public async Task CloseSurvey_SurveyClosedAndMailAddedToTheQueue()
    {
        // Arrange
        var caseEntity = GetCaseEntity(true, true);
        var documentService = Substitute.For<IDocumentServiceClient>();
        var caseRepository = GetCaseRepository_WithPatchConfiguration(caseEntity);
        var permissionService = GetPermissionService(spDirectory: caseEntity.SharePointDirectory);
        var profileMapper = GetProfileMapper_ReturningProfiles(
            new List<string> { caseEntity.ManagerEcode!, caseEntity.LeadKnowledgeSpecialistEcode! });
        var mailDistributionService = GetMailDistributionService();

        var surveyService = GetService(
            documentService,
            caseRepository,
            permissionService,
            profileMapper,
            mailDistributionService);

        // Act
        var result = surveyService.CloseSurvey(caseEntity, ServiceUserInfo, "correlationId");
        await result;

        // Assert
        result.IsCompletedSuccessfully.Should().BeTrue();
        await caseRepository
            .Received()
            .PatchCase(
                Arg.Is(caseEntity.Id),
                Arg.Is(caseEntity.Key),
                Arg.Is<IReadOnlyDictionary<string, object?>>(
                    d => d.Contains(
                             new KeyValuePair<string, object?>(
                                 nameof(CaseEntity.ItemStage).ToCamelCase(),
                                 CaseState.SurveyClosed))
                         && d.Contains(
                             new KeyValuePair<string, object?>(
                                 nameof(CaseEntity.CorrelationId).ToCamelCase(),
                                 "correlationId"))));

        await permissionService
            .Received()
            .RemovePermission(
                Arg.Is(caseEntity.SharePointDirectory!.DriveId),
                Arg.Is(caseEntity.SharePointDirectory.DirectoryId),
                Arg.Any<string>(), 
                Arg.Any<string>());

        mailDistributionService
            .Received()
            .GetSupportMailboxByRegion(Arg.Any<string>());

        await mailDistributionService
            .Received()
            .AddEmailToQueue(Arg.Any<EmailMessageDto>());
    }

    [Fact]
    public async Task CloseSurvey_SurveyClosedWhenPermissionIsNotActive()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        caseEntity.Permissions = new List<Permission>
            { new("permId", caseEntity.ManagerEcode!, Domain.PermissionScope.User, PermissionType.SurveyAccess, new DateTime(2023, 1, 1), false) };

        var documentService = Substitute.For<IDocumentServiceClient>();
        var caseRepository = GetCaseRepository_WithPatchConfiguration(caseEntity);
        var permissionService = Substitute.For<IPermissionService>();
        var profileMapper = GetProfileMapper_ReturningProfiles(
            new List<string> { caseEntity.ManagerEcode!, caseEntity.LeadKnowledgeSpecialistEcode! });

        var surveyService = GetService(
            documentService,
            caseRepository,
            permissionService,
            profileMapper);

        // Act
        var result = surveyService.CloseSurvey(caseEntity, ServiceUserInfo, "correlationId");
        await result;

        // Assert
        result.IsCompletedSuccessfully.Should().BeTrue();
        await caseRepository
            .Received()
            .PatchCase(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Is<IReadOnlyDictionary<string, object?>>(
                    d => d.Contains(
                             new KeyValuePair<string, object?>(
                                 nameof(CaseEntity.ItemStage).ToCamelCase(),
                                 CaseState.SurveyClosed))
                         && d.Contains(
                             new KeyValuePair<string, object?>(
                                 nameof(CaseEntity.CorrelationId).ToCamelCase(),
                                 "correlationId"))));
    }

    [Fact]
    public async Task CloseSurvey_PermissionServiceThrowsException()
    {
        // Arrange
        var caseEntity = GetCaseEntity(true, true);
        var documentService = Substitute.For<IDocumentServiceClient>();
        var caseRepository = GetCaseRepository_WithPatchConfiguration(caseEntity);
        var permissionService = Substitute.For<IPermissionService>();
        permissionService
            .RemovePermission(
                Arg.Is(caseEntity.SharePointDirectory!.DriveId),
                Arg.Is(caseEntity.SharePointDirectory.DirectoryId),
                Arg.Any<string>(), 
                Arg.Any<string>())
            .ThrowsAsync(new Exception("message"));

        var surveyService = GetService(
            documentService,
            caseRepository,
            permissionService);

        // Act
        Func<Task> act = async () => { await surveyService.CloseSurvey(caseEntity, ServiceUserInfo, "correlationId"); };

        // Assert
        await act.Should().ThrowAsync<Exception>();

        await caseRepository
            .DidNotReceive()
            .PatchCase(
                Arg.Is(caseEntity.Id),
                Arg.Is(caseEntity.Key),
                Arg.Is<IReadOnlyDictionary<string, object?>>(
                    d => d.Contains(
                             new KeyValuePair<string, object?>(
                                 nameof(CaseEntity.ItemStage).ToCamelCase(),
                                 CaseState.SurveyClosed))
                         && d.Contains(
                             new KeyValuePair<string, object?>(
                                 nameof(CaseEntity.CorrelationId).ToCamelCase(),
                                 "correlationId"))));

        await permissionService
            .Received()
            .RemovePermission(
                Arg.Is(caseEntity.SharePointDirectory.DriveId),
                Arg.Is(caseEntity.SharePointDirectory.DirectoryId),
                Arg.Any<string>(),
                Arg.Any<string>());
    }

    [Fact]
    public async Task UpdateSurvey_WhenCaseManagerChanged_PermissionUpdatedAndEmailSent()
    {
        // Arrange
        var caseEntity = GetCaseEntity(true, true);
        caseEntity.ItemStage = CaseState.SurveyOpened;
        caseEntity.ManagerEcode = "newManagerEcode";

        var documentService = Substitute.For<IDocumentServiceClient>();
        var caseRepository = GetCaseRepository_WithPatchConfiguration(caseEntity);
        var mailDistributionService = GetMailDistributionService();
        var permission = new DirectoryPermissionDto("permId", "a@bain.com", PermissionScope.User);
        var permissionService = GetPermissionService(permission, caseEntity.SharePointDirectory);
        var profileMapper = GetProfileMapper_ReturningProfiles(
            new List<string> { caseEntity.ManagerEcode!, caseEntity.BillingPartnerEcode! });

        var surveyService = GetService(
            documentService,
            caseRepository,
            permissionService,
            profileMapper,
            mailDistributionService);

        // Act
        var result = surveyService.UpdateSurvey(caseEntity, ServiceUserInfo, "correlationId");
        await result;

        // Assert
        result.IsCompletedSuccessfully.Should().BeTrue();

        await permissionService
            .Received()
            .RemovePermission(
                Arg.Is(caseEntity.SharePointDirectory!.DriveId),
                Arg.Is(caseEntity.SharePointDirectory.DirectoryId),
                Arg.Any<string>(),
                Arg.Any<string>());

        await permissionService
            .Received()
            .GrantPermission(Arg.Is(caseEntity.ManagerEcode!),
                Arg.Is(caseEntity.SharePointDirectory.DriveId),
                Arg.Is(caseEntity.SharePointDirectory.DirectoryId),
                Arg.Is(PermissionLevels.Edit),
                Arg.Any<string>());

        mailDistributionService
            .Received()
            .GetSupportMailboxByRegion(Arg.Any<string>());
        await mailDistributionService
            .Received()
            .AddEmailToQueue(Arg.Any<EmailMessageDto>());

        await caseRepository
            .Received()
            .PatchCase(
                Arg.Is(caseEntity.Id),
                Arg.Is(caseEntity.Key),
                Arg.Is<IReadOnlyDictionary<string, object?>>(
                    d => d.Contains(
                             new KeyValuePair<string, object?>(
                                 nameof(CaseEntity.CorrelationId).ToCamelCase(),
                                 "correlationId"))));
    }

    [Fact]
    public async Task UpdateSurvey_WhenPermissionHasNotBeenGranted_PermissionGrantedAndEmailSent()
    {
        // Arrange
        var caseEntity = GetCaseEntity(true, false);
        caseEntity.ItemStage = CaseState.SurveyOpened;

        var documentService = Substitute.For<IDocumentServiceClient>();
        var caseRepository = GetCaseRepository_WithPatchConfiguration(caseEntity);
        var mailDistributionService = GetMailDistributionService();
        var permission = new DirectoryPermissionDto("permId", "a@bain.com", PermissionScope.User);
        var permissionService = GetPermissionService(permission, caseEntity.SharePointDirectory);
        var profileMapper = GetProfileMapper_ReturningProfiles(
            new List<string> { caseEntity.ManagerEcode!, caseEntity.BillingPartnerEcode! });

        var surveyService = GetService(
            documentService,
            caseRepository,
            permissionService,
            profileMapper,
            mailDistributionService);

        // Act
        var result = surveyService.UpdateSurvey(caseEntity, ServiceUserInfo, "correlationId");
        await result;

        // Assert
        result.IsCompletedSuccessfully.Should().BeTrue();

        await permissionService
            .DidNotReceive()
            .RemovePermission(
                Arg.Is(caseEntity.SharePointDirectory!.DriveId),
                Arg.Is(caseEntity.SharePointDirectory.DirectoryId),
                Arg.Any<string>(),
                Arg.Any<string>());

        await permissionService
            .Received()
            .GrantPermission(Arg.Is(caseEntity.ManagerEcode!),
                Arg.Is(caseEntity.SharePointDirectory.DriveId),
                Arg.Is(caseEntity.SharePointDirectory.DirectoryId),
                Arg.Is(PermissionLevels.Edit),
                Arg.Any<string>());

        mailDistributionService
            .Received()
            .GetSupportMailboxByRegion(Arg.Any<string>());
        await mailDistributionService
            .Received()
            .AddEmailToQueue(Arg.Any<EmailMessageDto>());

        await caseRepository
            .Received()
            .PatchCase(
                Arg.Is(caseEntity.Id),
                Arg.Is(caseEntity.Key),
                Arg.Is<IReadOnlyDictionary<string, object?>>(
                    d => d.Contains(
                             new KeyValuePair<string, object?>(
                                 nameof(CaseEntity.CorrelationId).ToCamelCase(),
                                 "correlationId"))));
    }

    [Fact]
    public async Task UpdateSurvey_WhenCaseManagerNotChanged_PermissionNotUpdated()
    {
        // Arrange
        var caseEntity = GetCaseEntity(true, true);
        caseEntity.ItemStage = CaseState.SurveyOpened;

        var documentService = Substitute.For<IDocumentServiceClient>();
        var caseRepository = GetCaseRepository_WithPatchConfiguration(caseEntity);
        var mailDistributionService = GetMailDistributionService();
        var permissionService = Substitute.For<IPermissionService>();

        var surveyService = GetService(
            documentService,
            caseRepository,
            permissionService);

        // Act
        var result = surveyService.UpdateSurvey(caseEntity, ServiceUserInfo, "correlationId");
        await result;

        // Assert
        result.IsCompletedSuccessfully.Should().BeTrue();

        await permissionService
            .DidNotReceive()
            .RemovePermission(
                Arg.Is(caseEntity.SharePointDirectory!.DriveId),
                Arg.Is(caseEntity.SharePointDirectory.DirectoryId),
                Arg.Is("permId"), Arg.Any<string>());

        await permissionService
            .DidNotReceive()
            .GrantPermission(Arg.Is(caseEntity.ManagerEcode!),
                Arg.Is(caseEntity.SharePointDirectory.DriveId),
                Arg.Is(caseEntity.SharePointDirectory.DirectoryId),
                Arg.Is(PermissionLevels.Edit),
                Arg.Any<string>());

        mailDistributionService
            .DidNotReceive()
            .GetSupportMailboxByRegion(Arg.Any<string>());

        await mailDistributionService
            .DidNotReceive()
            .AddEmailToQueue(Arg.Any<EmailMessageDto>());
    }

    [Fact]
    public async Task UpdateSurvey_WhenCaseIsDeleted_OnlyRemovePermissions()
    {
        // Arrange
        var caseEntity = GetCaseEntity(true, true);
        caseEntity.ItemStage = CaseState.Deleted;
        var documentService = Substitute.For<IDocumentServiceClient>();
        var caseRepository = GetCaseRepository_WithPatchConfiguration(caseEntity);
        var mailDistributionService = GetMailDistributionService();

        var permissionService = GetPermissionService(null, caseEntity.SharePointDirectory);
        var profileMapper = GetProfileMapper_ReturningProfiles(
            new List<string> { caseEntity.ManagerEcode!, caseEntity.BillingPartnerEcode! });

        var surveyService = GetService(
            documentService,
            caseRepository,
            permissionService,
            profileMapper,
            mailDistributionService);

        // Act
        var result = surveyService.UpdateSurvey(caseEntity, ServiceUserInfo, "correlationId");
        await result;

        // Assert
        result.IsCompletedSuccessfully.Should().BeTrue();

        await permissionService
            .Received()
            .RemovePermission(
                Arg.Is(caseEntity.SharePointDirectory!.DriveId),
                Arg.Is(caseEntity.SharePointDirectory.DirectoryId),
                Arg.Any<string>(),
                Arg.Any<string>());

        await permissionService
            .DidNotReceive()
            .GrantPermission(Arg.Is(caseEntity.ManagerEcode!),
                Arg.Is(caseEntity.SharePointDirectory.DriveId),
                Arg.Is(caseEntity.SharePointDirectory.DirectoryId),
                Arg.Is(PermissionLevels.Edit),
                Arg.Any<string>());

        mailDistributionService
            .DidNotReceive()
            .GetSupportMailboxByRegion(Arg.Any<string>());
        await mailDistributionService
            .DidNotReceive()
            .AddEmailToQueue(Arg.Any<EmailMessageDto>());

        await caseRepository
            .Received()
            .PatchCase(
                Arg.Is(caseEntity.Id),
                Arg.Is(caseEntity.Key),
                Arg.Is<IReadOnlyDictionary<string, object?>>(
                    d => d.Contains(
                             new KeyValuePair<string, object?>(
                                 nameof(CaseEntity.CorrelationId).ToCamelCase(),
                                 "correlationId"))));
    }

    [Fact]
    public async Task CloseSurvey_SurveyClosedAndPermissionRemoved()
    {
        // Arrange
        var caseEntity = GetCaseEntity(true, true);

        var documentService = Substitute.For<IDocumentServiceClient>();
        var caseRepository = GetCaseRepository_WithPatchConfiguration(caseEntity);
        var permissionService = Substitute.For<IPermissionService>();
        permissionService
            .RemovePermission(
                Arg.Is(caseEntity.SharePointDirectory!.DriveId),
                Arg.Is(caseEntity.SharePointDirectory.DirectoryId),
                Arg.Any<string>(), 
                Arg.Any<string>())
            .Returns(Task.CompletedTask);

        var profileMapper = GetProfileMapper_ReturningProfiles(
            new List<string> { caseEntity.ManagerEcode!, caseEntity.LeadKnowledgeSpecialistEcode! });

        var surveyService = GetService(
            documentService,
            caseRepository,
            permissionService,
            profileMapper);

        // Act
        var result = surveyService.CloseSurvey(caseEntity, ServiceUserInfo, "correlationId");
        await result;

        // Assert
        result.IsCompletedSuccessfully.Should().BeTrue();
        await caseRepository
            .Received()
            .PatchCase(
                Arg.Is(caseEntity.Id),
                Arg.Is(caseEntity.Key),
                Arg.Is<IReadOnlyDictionary<string, object?>>(
                    d => d.Contains(
                             new KeyValuePair<string, object?>(
                                 nameof(CaseEntity.ItemStage).ToCamelCase(),
                                 CaseState.SurveyClosed))
                         && d.Contains(
                             new KeyValuePair<string, object?>(
                                 nameof(CaseEntity.CorrelationId).ToCamelCase(),
                                 "correlationId"))));

        await permissionService
            .Received()
            .RemovePermission(
                Arg.Is(caseEntity.SharePointDirectory.DriveId),
                Arg.Is(caseEntity.SharePointDirectory.DirectoryId),
                Arg.Any<string>(),
                Arg.Any<string>());
    }

    [Fact]
    public async Task CloseSurvey_SurveyClosedWhenPermissionScopeIsGroupPermissionNotRemoved()
    {
        // Arrange
        var caseEntity = GetCaseEntity(true, false);
        caseEntity.Permissions = new List<Permission>
        {
            new("permId", caseEntity.ManagerEcode!, Domain.PermissionScope.Group, PermissionType.SurveyAccess, new DateTime(2023, 1, 5), true)
        };

        var documentService = Substitute.For<IDocumentServiceClient>();
        var caseRepository = GetCaseRepository_WithPatchConfiguration(caseEntity);
        var permissionService = Substitute.For<IPermissionService>();
        permissionService
            .RemovePermission(
                Arg.Is(caseEntity.SharePointDirectory!.DriveId),
                Arg.Is(caseEntity.SharePointDirectory.DirectoryId),
                Arg.Is("permId"),
                Arg.Any<string>())
            .Returns(Task.CompletedTask);

        var profileMapper = GetProfileMapper_ReturningProfiles(
            new List<string> { caseEntity.ManagerEcode!, caseEntity.LeadKnowledgeSpecialistEcode! });

        var surveyService = GetService(
            documentService,
            caseRepository,
            permissionService,
            profileMapper);

        // Act
        var result = surveyService.CloseSurvey(caseEntity, ServiceUserInfo, "correlationId");
        await result;

        // Assert
        result.IsCompletedSuccessfully.Should().BeTrue();
        await caseRepository
            .Received()
            .PatchCase(
                Arg.Is(caseEntity.Id),
                Arg.Is(caseEntity.Key),
                Arg.Is<IReadOnlyDictionary<string, object?>>(
                    d => d.Contains(
                             new KeyValuePair<string, object?>(
                                 nameof(CaseEntity.ItemStage).ToCamelCase(),
                                 CaseState.SurveyClosed))
                         && d.Contains(
                             new KeyValuePair<string, object?>(
                                 nameof(CaseEntity.CorrelationId).ToCamelCase(),
                                 "correlationId"))));

        await permissionService
            .DidNotReceive()
            .RemovePermission(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>());
    }

    private static CaseEntity GetCaseEntity(bool withSharePointDirectory, bool withPermission)
    {
        var caseEntity = Fake.CaseEntity();
        if (withSharePointDirectory)
        {
            caseEntity.SharePointDirectory = new SharePointDirectoryEntity()
            {
                DirectoryId = "directoryId",
                DriveId = "driveId"
            };
        }

        if (withPermission)
        {
            caseEntity.Permissions = new List<Permission>
            {
                new("permId", caseEntity.ManagerEcode!, Domain.PermissionScope.User, PermissionType.SurveyAccess, new DateTime(2023, 1, 1), false),
                new("permId", caseEntity.ManagerEcode!, Domain.PermissionScope.User, PermissionType.SurveyAccess, new DateTime(2023, 1, 5), true)
            };
        }
        return caseEntity;
    }

    private static IPermissionService GetPermissionService(DirectoryPermissionDto? permission = null, SharePointDirectoryEntity? spDirectory = null)
    {
        var permissionService = Substitute.For<IPermissionService>();
        if (permission != null)
        {
            permissionService
                .GrantPermission(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>())
                .Returns(permission);
        }

        if (spDirectory != null)
        {
            permissionService
                .RemovePermission(
                    Arg.Is(spDirectory.DriveId),
                    Arg.Is(spDirectory.DirectoryId),
                    Arg.Any<string>(),
                    Arg.Any<string>())
                .Returns(Task.CompletedTask);
        }
        return permissionService;
    }

    private static ISingleCaseRepository GetCaseRepository_WithPatchConfiguration(CaseEntity caseEntity)
    {
        var caseRepository = Substitute.For<ISingleCaseRepository>();
        caseRepository
            .PatchCase(
                Arg.Is(caseEntity.Id),
                Arg.Is(caseEntity.Key),
                Arg.Any<IReadOnlyDictionary<string, object?>>())
            .Returns(true);
        return caseRepository;
    }

    private static IDocumentServiceClient GetDocumentService_ReturningDirectoryInfo(DirectoryInfoDto expectedResult)
    {
        var documentService = Substitute.For<IDocumentServiceClient>();
        documentService
            .CreateDirectory(Arg.Any<CreateDirectoryDto>(), Arg.Any<string>())
            .Returns(expectedResult);
        return documentService;
    }

    private ProfileMapper GetProfileMapper_ReturningProfiles(List<string> ecodes,
        string correlationId = "correlationId")
    {
        var repository = Substitute.For<IProfileRepository>();

        repository.GetProfiles(
                Arg.Is<IReadOnlyList<string>>(list => list.OrderBy(x => x).SequenceEqual(ecodes.OrderBy(x => x))),
                correlationId)
            .Returns(ecodes.ConvertAll(Fake.EmployeeDetails));

        return new ProfileMapper(repository);
    }

    private IMailDistributionService GetMailDistributionService()
    {
        var mailDistributionService = Substitute.For<IMailDistributionService>();
        mailDistributionService
            .GetSupportMailboxByRegion(Arg.Any<string>())
            .Returns("test@test.test");

        mailDistributionService
            .AddEmailToQueue(Arg.Any<EmailMessageDto>())
            .Returns(Task.CompletedTask);

        return mailDistributionService;
    }

    private WorkflowSurveyService GetService(
        IDocumentServiceClient? documentService,
        ISingleCaseRepository? caseRepository,
        IPermissionService? permissionService,
        IProfileMapper? profileMapper = null,
        IMailDistributionService? mailDistributionService = null,
        WorkflowSurveyOptions? optionsValue = null,
        IEventDistributionService? eventDistributor = null
    )
    {
        var options = MicrosoftOptions.Create(optionsValue ?? new WorkflowSurveyOptions());
        var logger = Substitute.For<ILogger<WorkflowSurveyService>>();

        var taxonomy = Substitute.For<ITaxonomyRepository>();
        taxonomy.GetFlatTaxonomy(Arg.Any<TaxonomyType>()).Returns(new Dictionary<int, TermDto>());
        var taxonomyFactory = new TaxonomyServiceFactory(taxonomy);

        var mailService = mailDistributionService ?? Substitute.For<IMailDistributionService>();
        var pMapper = profileMapper ?? Substitute.For<IProfileMapper>();

        return new WorkflowSurveyService(
            documentService ?? Substitute.For<IDocumentServiceClient>(),
            caseRepository ?? Substitute.For<ISingleCaseRepository>(),
            options,
            permissionService ?? Substitute.For<IPermissionService>(),
            mailService, pMapper, taxonomyFactory, 
            eventDistributor ?? Substitute.For<IEventDistributionService>(),
            logger);
    }
}