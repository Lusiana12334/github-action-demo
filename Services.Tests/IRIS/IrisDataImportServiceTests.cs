using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PEXC.Case.DataAccess.CosmosDB;
using PEXC.Case.DataAccess;
using PEXC.Case.Domain;
using PEXC.Case.Services.Infrastructure;
using PEXC.Case.Services.IRIS;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using PEXC.Case.Services.IRIS.Contracts;
using PEXC.Case.Tests.Common;

namespace PEXC.Case.Services.Tests.IRIS;

public class IrisDataImportServiceTests
{
    [Fact]
    public async Task UpdateCases_WhenPreviousImportStateNotFound_CallsServiceWithInitialModifiedAfterTime()
    {
        // Arrange
        var initialModifiedAfterTime = new DateOnly(2000, 01, 15);
        var pegCapabilities = new[] { 1, 2, 3 };
        var pegIndustries = new[] { 1, 2, 3 };
        var stateRepository = Substitute.For<IDataImportStateRepository<IrisDataImportState>>();
        stateRepository
            .GetState()
            .Returns((IrisDataImportState?)null);

        var irisService = Substitute.For<IIrisIntegrationService>();

        // Act
        var service = PrepareService(
            stateRepository: stateRepository,
            irisService: irisService,
            initialModifiedAfterTime: initialModifiedAfterTime,
            pegCapabilities: pegCapabilities,
            pegIndustries: pegIndustries);
        await service.UpdateCases();

        // Assert
        await irisService.Received().GetCasesModifiedAfter(initialModifiedAfterTime, pegIndustries, pegCapabilities);
    }

    [Fact]
    public async Task UpdateCases_WhenPreviousImportSuccessful_CallsServiceWithNewModifiedAfter()
    {
        // Arrange
        var lastImportState = new IrisDataImportState
        {
            LastExecutionTime = DateTime.UtcNow.AddDays(-5),
            LastModifiedAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-6))
        };
        var pegCapabilities = new[] { 1, 2, 3 };
        var pegIndustries = new[] { 1, 2, 3 };

        var stateRepository = Substitute.For<IDataImportStateRepository<IrisDataImportState>>();
        stateRepository
            .GetState()
            .Returns(lastImportState);

        var irisService = Substitute.For<IIrisIntegrationService>();

        var service = PrepareService(
            stateRepository: stateRepository,
            irisService: irisService,
            pegCapabilities: pegCapabilities,
            pegIndustries: pegIndustries);

        // Act
        await service.UpdateCases();

        // Assert
        await irisService
            .Received()
            .GetCasesModifiedAfter(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-6), pegIndustries, pegCapabilities);
    }

    [Fact]
    public async Task UpdateCases_WhenPreviousImportFailed_CallsServiceWithLastModifiedAfter()
    {
        // Arrange
        var lastImportState = new IrisDataImportState
        {
            Failed = true,
            LastExecutionTime = DateTime.UtcNow.AddDays(-5),
            LastModifiedAfter = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-6)
        };
        var pegCapabilities = new[] { 1, 2, 3 };
        var pegIndustries = new[] { 1, 2, 3 };

        var stateRepository = Substitute.For<IDataImportStateRepository<IrisDataImportState>>();
        stateRepository
            .GetState()
            .Returns(lastImportState);

        var irisService = Substitute.For<IIrisIntegrationService>();

        var service = PrepareService(
            stateRepository: stateRepository,
            irisService: irisService,
            pegCapabilities: pegCapabilities,
            pegIndustries: pegIndustries);

        // Act
        await service.UpdateCases();

        // Assert
        await irisService
            .Received()
            .GetCasesModifiedAfter(lastImportState.LastModifiedAfter.Value, pegIndustries, pegCapabilities);
    }

    [Fact]
    public async Task UpdateCases_WhenImportFailed_FailureInfoSavedInState()
    {
        // Arrange
        var lastImportState = new IrisDataImportState
        {
            LastSuccessfulExecutionTime = DateTime.UtcNow.AddDays(-2),
            LastSuccessfulModifiedAfter = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-3),
            LastExecutionTime = DateTime.UtcNow.AddDays(-2),
            LastModifiedAfter = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-4)
        };

        var stateRepository = Substitute.For<IDataImportStateRepository<IrisDataImportState>>();
        stateRepository
            .GetState()
            .Returns(lastImportState);

        var irisService = Substitute.For<IIrisIntegrationService>();
        irisService
            .GetCasesModifiedAfter(Arg.Any<DateOnly>(), Arg.Any<int[]>(), Arg.Any<int[]>())
            .ThrowsAsync(new Exception("Error Message"));

        var service = PrepareService(
            stateRepository: stateRepository,
            irisService: irisService);

        // Act
        var call = () => service.UpdateCases();

        // Assert
        await call.Should().ThrowAsync<Exception>();
        await stateRepository
            .Received()
            .UpdateState(
                Arg.Is<IrisDataImportState>(
                    s => s.Failed &&
                         s.FailedAttempts == 1 &&
                         s.ErrorMessage == "Error Message" &&
                         s.LastSuccessfulExecutionTime == lastImportState.LastSuccessfulExecutionTime &&
                         s.LastSuccessfulModifiedAfter == lastImportState.LastSuccessfulModifiedAfter &&
                         s.LastExecutionTime != lastImportState.LastExecutionTime &&
                         s.LastModifiedAfter != lastImportState.LastModifiedAfter));
    }

    [Fact]
    public async Task UpdateCases_WhenPreviousAndCurrentImportFailed_FailureInfoUpdatedInState()
    {
        // Arrange
        var lastImportState = new IrisDataImportState
        {
            Failed = true,
            FailedAttempts = 3,
            LastSuccessfulExecutionTime = DateTime.UtcNow.AddDays(-2),
            LastSuccessfulModifiedAfter = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-3),
            LastExecutionTime = DateTime.UtcNow.AddDays(-1),
            LastModifiedAfter = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2)
        };
        var stateRepository = Substitute.For<IDataImportStateRepository<IrisDataImportState>>();
        stateRepository
            .GetState()
            .Returns(lastImportState);

        var irisService = Substitute.For<IIrisIntegrationService>();
        irisService
            .GetCasesModifiedAfter(Arg.Any<DateOnly>(), Arg.Any<int[]>(), Arg.Any<int[]>())
            .ThrowsAsync(new Exception("Error Message"));

        var service = PrepareService(
            stateRepository: stateRepository,
            irisService: irisService);

        // Act
        var call = () =>  service.UpdateCases();

        // Assert
        await call.Should().ThrowAsync<Exception>();
        await stateRepository
            .Received()
            .UpdateState(
                Arg.Is<IrisDataImportState>(
                    s => s.Failed &&
                         s.FailedAttempts == lastImportState.FailedAttempts + 1 &&
                         s.ErrorMessage == "Error Message" &&
                         s.LastSuccessfulExecutionTime == lastImportState.LastSuccessfulExecutionTime &&
                         s.LastSuccessfulModifiedAfter == lastImportState.LastSuccessfulModifiedAfter &&
                         s.LastExecutionTime != lastImportState.LastExecutionTime &&
                         s.LastModifiedAfter == lastImportState.LastModifiedAfter));
    }

    [Fact]
    public async Task UpdateCases_WhenImportSuccessfulAfterFailed_StateReset()
    {
        // Arrange
        var lastImportState = new IrisDataImportState
        {
            Failed = true,
            FailedAttempts = 5,
            ErrorMessage = "Error Message",
            LastSuccessfulExecutionTime = DateTime.UtcNow.AddDays(-4),
            LastSuccessfulModifiedAfter = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-5),
            LastExecutionTime = DateTime.UtcNow.AddDays(-2),
            LastModifiedAfter = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-4)
        };

        var stateRepository = Substitute.For<IDataImportStateRepository<IrisDataImportState>>();
        stateRepository
            .GetState()
            .Returns(lastImportState);

        var irisService = Substitute.For<IIrisIntegrationService>();

        var service = PrepareService(
            stateRepository: stateRepository,
            irisService: irisService);

        // Act
        await service.UpdateCases();

        // Assert
        await stateRepository
            .Received()
            .UpdateState(
                Arg.Is<IrisDataImportState>(
                    s => s.Failed == false &&
                         s.FailedAttempts == 0 &&
                         s.ErrorMessage == null &&
                         s.LastSuccessfulExecutionTime != lastImportState.LastSuccessfulExecutionTime &&
                         s.LastSuccessfulModifiedAfter != lastImportState.LastSuccessfulModifiedAfter &&
                         s.LastExecutionTime != lastImportState.LastExecutionTime &&
                         s.LastModifiedAfter == lastImportState.LastModifiedAfter));
    }

    [Fact]
    public async Task UpdateCases_WhenNoCasesFound_NoCasesSavedInRepository()
    {
        // Arrange
        var stateRepository = Substitute.For<IDataImportStateRepository<IrisDataImportState>>();
        stateRepository
            .GetState()
            .Returns((IrisDataImportState?)null);

        var irisService = Substitute.For<IIrisIntegrationService>();
        irisService
            .GetCasesModifiedAfter(Arg.Any<DateOnly>(), Arg.Any<int[]>(), Arg.Any<int[]>())
            .Returns(Array.Empty<IrisCaseDto>());

        var caseRepository = Substitute.For<ISingleCaseRepository>();

        // Act
        var service = PrepareService(stateRepository, caseRepository, irisService);
        await service.UpdateCases();

        // Assert
        await caseRepository
            .DidNotReceive()
            .UpdateCase(Arg.Any<CaseEntity>());
    }

    [Fact]
    public async Task UpdateCases_WhenCaseNotExists_NoCasesSavedInRepository()
    {
        // Arrange
        var stateRepository = Substitute.For<IDataImportStateRepository<IrisDataImportState>>();
        stateRepository
            .GetState()
            .Returns((IrisDataImportState?)null);

        var irisService = Substitute.For<IIrisIntegrationService>();
        irisService
            .GetCasesModifiedAfter(Arg.Any<DateOnly>(), Arg.Any<int[]>(), Arg.Any<int[]>())
            .Returns(new List<IrisCaseDto> { new IrisCaseDto("123") });

        var caseRepository = Substitute.For<ISingleCaseRepository>();
        caseRepository
            .GetNonRetainerCaseByCaseCode("123")
            .ReturnsNull();

        // Act
        var service = PrepareService(stateRepository, caseRepository, irisService);
        await service.UpdateCases();

        // Assert
        await caseRepository
            .DidNotReceive()
            .UpdateCase(Arg.Any<CaseEntity>());
    }

    [Fact]
    public async Task UpdateCases_WhenCaseExists_CasesSavedInRepository()
    {
        // Arrange
        var stateRepository = Substitute.For<IDataImportStateRepository<IrisDataImportState>>();
        stateRepository
            .GetState()
            .Returns((IrisDataImportState?)null);

        var caseEntity = Fake.CaseEntity();
        var leadKnowledgeSpecialist = "leadKnowledgeEcode1";

        var irisService = Substitute.For<IIrisIntegrationService>();
        irisService
            .GetCasesModifiedAfter(Arg.Any<DateOnly>(), Arg.Any<int[]>(), Arg.Any<int[]>())
            .Returns(new List<IrisCaseDto>
                { new IrisCaseDto(caseEntity.CaseCode) { LeadKnowledgeSpecialist = leadKnowledgeSpecialist } });

        var caseRepository = Substitute.For<ISingleCaseRepository>();
        caseRepository
            .GetNonRetainerCaseByCaseCode(caseEntity.CaseCode)
            .Returns(caseEntity);

        // Act
        var service = PrepareService(stateRepository, caseRepository, irisService);
        await service.UpdateCases();

        // Assert
        await caseRepository
            .Received()
            .UpdateCase(Arg.Is<CaseEntity>(e =>
                e.CaseCode == caseEntity.CaseCode
                && e.LeadKnowledgeSpecialistEcode == leadKnowledgeSpecialist));
    }

    [Fact]
    public async Task UpdateCases_WhenCaseExistsAndKSLeadNotChanged_CasesNotSavedInRepository()
    {
        // Arrange
        var stateRepository = Substitute.For<IDataImportStateRepository<IrisDataImportState>>();
        stateRepository
            .GetState()
            .Returns((IrisDataImportState?)null);

        var caseEntity = Fake.CaseEntity();

        var irisService = Substitute.For<IIrisIntegrationService>();
        irisService
            .GetCasesModifiedAfter(Arg.Any<DateOnly>(), Arg.Any<int[]>(), Arg.Any<int[]>())
            .Returns(new List<IrisCaseDto>
                { new IrisCaseDto(caseEntity.CaseCode) { LeadKnowledgeSpecialist = caseEntity.LeadKnowledgeSpecialistEcode } });

        var caseRepository = Substitute.For<ISingleCaseRepository>();
        caseRepository
            .GetNonRetainerCaseByCaseCode(caseEntity.CaseCode)
            .Returns(caseEntity);

        // Act
        var service = PrepareService(stateRepository, caseRepository, irisService);
        await service.UpdateCases();

        // Assert
        await caseRepository
            .DidNotReceive()
            .UpdateCase(Arg.Is<CaseEntity>(e =>
                e.CaseCode == caseEntity.CaseCode
                && e.LeadKnowledgeSpecialistEcode == caseEntity.LeadKnowledgeSpecialistEcode));
    }

    private IrisDataImportService PrepareService(
    IDataImportStateRepository<IrisDataImportState>? stateRepository = null,
    ISingleCaseRepository? caseRepository = null,
    IIrisIntegrationService? irisService = null,
    DateOnly? initialModifiedAfterTime = null,
    int[]? pegCapabilities = null,
    int[]? pegIndustries = null)
    {
        var options = Options.Create(new CaseDataImportOptions
        {
            InitialModifiedAfterTime = initialModifiedAfterTime ?? new DateOnly(2000, 1, 1),
            PegCapabilities = pegCapabilities ?? new[] { 1, 2, 3 },
            PegIndustries = pegIndustries ?? new[] { 1, 2, 3 }
        });

        stateRepository ??= Substitute.For<IDataImportStateRepository<IrisDataImportState>>();
        caseRepository ??= Substitute.For<ISingleCaseRepository>();
        irisService ??= Substitute.For<IIrisIntegrationService>();
        var logger = Substitute.For<ILogger<IrisDataImportService>>();
        return new IrisDataImportService(stateRepository, caseRepository, irisService, logger, options);
    }
}