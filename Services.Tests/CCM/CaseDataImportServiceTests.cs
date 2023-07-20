using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute.ExceptionExtensions;
using PEXC.Case.DataAccess;
using PEXC.Case.DataAccess.CosmosDB;
using PEXC.Case.Domain;
using PEXC.Case.Services.CCM;
using PEXC.Case.Services.CCM.Contracts;
using PEXC.Case.Services.Infrastructure;
using PEXC.Case.Services.IRIS;
using PEXC.Case.Services.IRIS.Contracts;
using PEXC.Case.Services.Mapping;
using PEXC.Case.Services.Mapping.Taxonomy;
using PEXC.Case.Services.Staffing;
using PEXC.Case.Tests.Common;
using PEXC.Common.Taxonomy;
using PEXC.Common.Taxonomy.DataContracts;

namespace PEXC.Case.Services.Tests.CCM;

public class CaseDataImportServiceTests : MappingAwareTestsBase<MainProfile>
{
    [Fact]
    public async Task ImportCases_WhenPreviousImportStateNotFound_CallsServiceWithInitialModifiedAfterTime()
    {
        // Arrange
        var initialModifiedAfterTime = new DateOnly(2000, 01, 15);

        var stateRepository = Substitute.For<IDataImportStateRepository<CaseDataImportState>>();
        stateRepository
            .GetState()
            .Returns((CaseDataImportState?)null);

        var ccmService = Substitute.For<IClientCaseApiService>();

        // Act
        var service = PrepareService(
            stateRepository: stateRepository,
            ccmService: ccmService,
            initialModifiedAfterTime: initialModifiedAfterTime);
        await service.ImportCases();

        // Assert
        await ccmService.Received().GetAllCasesModifiedAfter(initialModifiedAfterTime);
    }

    [Fact]
    public async Task ImportCases_WhenPreviousImportSuccessful_CallsServiceWithNewModifiedAfter()
    {
        // Arrange
        var lastImportState = new CaseDataImportState
        {
            LastExecutionTime = DateTime.UtcNow.AddDays(-5),
            LastModifiedAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-6))
        };

        var stateRepository = Substitute.For<IDataImportStateRepository<CaseDataImportState>>();
        stateRepository
            .GetState()
            .Returns(lastImportState);

        var ccmService = Substitute.For<IClientCaseApiService>();

        var service = PrepareService(
            stateRepository: stateRepository,
            ccmService: ccmService);

        // Act
        await service.ImportCases();

        // Assert
        await ccmService
            .Received()
            .GetAllCasesModifiedAfter(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-6));
    }

    [Fact]
    public async Task ImportCases_WhenPreviousImportFailed_CallsServiceWithLastModifiedAfter()
    {
        // Arrange
        var lastImportState = new CaseDataImportState
        {
            Failed = true,
            LastExecutionTime = DateTime.UtcNow.AddDays(-5),
            LastModifiedAfter = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-6)
        };

        var stateRepository = Substitute.For<IDataImportStateRepository<CaseDataImportState>>();
        stateRepository
            .GetState()
            .Returns(lastImportState);

        var ccmService = Substitute.For<IClientCaseApiService>();

        var service = PrepareService(
            stateRepository: stateRepository,
            ccmService: ccmService);

        // Act
        await service.ImportCases();

        // Assert
        await ccmService
            .Received()
            .GetAllCasesModifiedAfter(lastImportState.LastModifiedAfter.Value);
    }

    [Fact]
    public async Task ImportCases_WhenImportFailed_FailureInfoSavedInState()
    {
        // Arrange
        var lastImportState = new CaseDataImportState
        {
            LastSuccessfulExecutionTime = DateTime.UtcNow.AddDays(-2),
            LastSuccessfulModifiedAfter = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-3),
            LastExecutionTime = DateTime.UtcNow.AddDays(-1),
            LastModifiedAfter = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-3)
        };
        var stateRepository = Substitute.For<IDataImportStateRepository<CaseDataImportState>>();
        stateRepository
            .GetState()
            .Returns(lastImportState);

        var ccmService = Substitute.For<IClientCaseApiService>();
        ccmService
            .GetAllCasesModifiedAfter(Arg.Any<DateOnly>())
            .ThrowsAsync(new Exception("Error Message"));

        var service = PrepareService(
            stateRepository: stateRepository,
            ccmService: ccmService);

        // Act
        Func<Task> act = async () => { await service.ImportCases(); };

        // Assert
        await act.Should().ThrowAsync<Exception>();
        await stateRepository
            .Received()
            .UpdateState(
                Arg.Is<CaseDataImportState>(
                    s => s.Failed &&
                         s.FailedAttempts == 1 &&
                         s.ErrorMessage == "Error Message" &&
                         s.LastSuccessfulExecutionTime == lastImportState.LastSuccessfulExecutionTime &&
                         s.LastSuccessfulModifiedAfter == lastImportState.LastSuccessfulModifiedAfter &&
                         s.LastExecutionTime != lastImportState.LastExecutionTime &&
                         s.LastModifiedAfter != lastImportState.LastModifiedAfter));
    }

    [Fact]
    public async Task ImportCases_WhenPreviousAndCurrentImportFailed_FailureInfoUpdatedInState()
    {
        // Arrange
        var lastImportState = new CaseDataImportState
        {
            Failed = true,
            FailedAttempts = 3,
            LastSuccessfulExecutionTime = DateTime.UtcNow.AddDays(-2),
            LastSuccessfulModifiedAfter = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-3),
            LastExecutionTime = DateTime.UtcNow.AddDays(-1),
            LastModifiedAfter = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2)
        };
        var stateRepository = Substitute.For<IDataImportStateRepository<CaseDataImportState>>();
        stateRepository
            .GetState()
            .Returns(lastImportState);

        var ccmService = Substitute.For<IClientCaseApiService>();
        ccmService
            .GetAllCasesModifiedAfter(Arg.Any<DateOnly>())
            .ThrowsAsync(new Exception("Error Message"));

        var service = PrepareService(
            stateRepository: stateRepository,
            ccmService: ccmService);

        // Act
        Func<Task> act = async () => { await service.ImportCases(); };

        // Assert
        await act.Should().ThrowAsync<Exception>();
        await stateRepository
            .Received()
            .UpdateState(
                Arg.Is<CaseDataImportState>(
                    s => s.Failed &&
                         s.FailedAttempts == lastImportState.FailedAttempts + 1 &&
                         s.ErrorMessage == "Error Message" &&
                         s.LastSuccessfulExecutionTime == lastImportState.LastSuccessfulExecutionTime &&
                         s.LastSuccessfulModifiedAfter == lastImportState.LastSuccessfulModifiedAfter &&
                         s.LastExecutionTime != lastImportState.LastExecutionTime &&
                         s.LastModifiedAfter == lastImportState.LastModifiedAfter));
    }

    [Fact]
    public async Task ImportCases_WhenImportSuccessfulAfterFailed_StateReset()
    {
        // Arrange
        var lastImportState = new CaseDataImportState
        {
            Failed = true,
            FailedAttempts = 5,
            ErrorMessage = "Error Message",
            LastSuccessfulExecutionTime = DateTime.UtcNow.AddDays(-4),
            LastSuccessfulModifiedAfter = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-5),
            LastExecutionTime = DateTime.UtcNow.AddDays(-2),
            LastModifiedAfter = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-4)
        };
        var stateRepository = Substitute.For<IDataImportStateRepository<CaseDataImportState>>();
        stateRepository
            .GetState()
            .Returns(lastImportState);

        var ccmService = Substitute.For<IClientCaseApiService>();

        var service = PrepareService(
            stateRepository: stateRepository,
            ccmService: ccmService);

        // Act
        await service.ImportCases();

        // Assert
        await stateRepository
            .Received()
            .UpdateState(
                Arg.Is<CaseDataImportState>(
                    s => s.Failed == false &&
                         s.FailedAttempts == 0 &&
                         s.ErrorMessage == null &&
                         s.LastSuccessfulExecutionTime != lastImportState.LastSuccessfulExecutionTime &&
                         s.LastSuccessfulModifiedAfter != lastImportState.LastSuccessfulModifiedAfter &&
                         s.LastExecutionTime != lastImportState.LastExecutionTime &&
                         s.LastModifiedAfter == lastImportState.LastModifiedAfter));
    }

    [Fact]
    public async Task ImportCases_WhenPreviousImportWithSameModifiedAfter_ImportSkipped()
    {
        var etcNow = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));

        // Arrange
        var successfulExecution = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        var lastImportState = new CaseDataImportState
        {
            LastSuccessfulModifiedAfter = successfulExecution,
            LastSuccessfulExecutionTime = etcNow,
            LastExecutionTime = successfulExecution.ToDateTime(new TimeOnly()),
        };
        var caseRepository = Substitute.For<ISingleCaseRepository>();
        var stateRepository = Substitute.For<IDataImportStateRepository<CaseDataImportState>>();
        stateRepository
            .GetState()
            .Returns(lastImportState);

        var ccmService = Substitute.For<IClientCaseApiService>();

        var cases = new[]
        {
            new CaseDetailsDto("1")
            {
                LastUpdated = etcNow.AddHours(-1)
            }
        };

        ccmService
            .GetAllCasesModifiedAfter(lastImportState.LastSuccessfulModifiedAfter.Value)
            .Returns(cases);

        // Act
        var service = PrepareService(
            caseRepository: caseRepository,
            stateRepository: stateRepository,
            ccmService: ccmService);
        await service.ImportCases();

        // Assert
        await caseRepository
            .Received(0)
            .UpdateCase(Arg.Any<CaseEntity>());
        await caseRepository
            .Received(0)
            .AddCase(Arg.Any<CaseEntity>());
    }

    [Fact]
    public async Task ImportCases_WhenNoCasesFound_NoCasesSavedInRepository()
    {
        // Arrange
        var ccmService = Substitute.For<IClientCaseApiService>();
        ccmService
            .GetAllCasesModifiedAfter(Arg.Any<DateOnly>())
            .Returns(Array.Empty<CaseDetailsDto>());

        var caseRepository = Substitute.For<ISingleCaseRepository>();

        // Act
        var service = PrepareService(caseRepository: caseRepository, ccmService: ccmService);
        await service.ImportCases();

        // Assert
        await caseRepository
            .DidNotReceive()
            .AddCase(Arg.Any<CaseEntity>());
        await caseRepository
            .DidNotReceive()
            .UpdateCase(Arg.Any<CaseEntity>());
    }

    [Fact]
    public async Task ImportCasesByCaseCodes_WhenNoCasesFound_NoCasesSavedInRepository()
    {
        // Arrange
        var ccmService = Substitute.For<IClientCaseApiService>();
        ccmService
            .GetCasesByCaseCodes(Arg.Any<IReadOnlyCollection<string>>())
            .Returns(Array.Empty<CaseDetailsDto>());

        var caseRepository = Substitute.For<ISingleCaseRepository>();

        // Act
        var service = PrepareService(caseRepository: caseRepository, ccmService: ccmService);
        var (updatedCaseCodes, createdCaseCodes) = await service.ImportCasesByCaseCodes(new[] { "ABC" });

        // Assert
        updatedCaseCodes
            .Should()
            .BeEmpty();
        createdCaseCodes
            .Should()
            .BeEmpty();
        await caseRepository
            .DidNotReceive()
            .AddCase(Arg.Any<CaseEntity>());
        await caseRepository
            .DidNotReceive()
            .UpdateCase(Arg.Any<CaseEntity>());
    }

    [Fact]
    public async Task ImportCases_WhenNotPegTaxonomy_CaseSkipped()
    {
        // Arrange
        var taxonomies =
            new[] { new TermDto { TagId = Guid.NewGuid(), Name = nameof(TermDto.Name), Id = 1 } };
        var stateRepository = Substitute.For<IDataImportStateRepository<CaseDataImportState>>();
        stateRepository
            .GetState()
            .Returns((CaseDataImportState?)null);

        var ccmService = Substitute.For<IClientCaseApiService>();
        var cases = new[]
        {
            new CaseDetailsDto("1")
            {
                CaseName = $"{nameof(CaseDetailsDto)}_1",
                PrimaryCapabilityTagId = taxonomies[0].TagId.ToString(),
                PrimaryIndustryTagId = taxonomies[0].TagId.ToString()
            },
            new CaseDetailsDto("2")
            {
                CaseName = $"{nameof(CaseDetailsDto)}_2",
                PrimaryCapabilityTagId = taxonomies[0].TagId.ToString(),
                PrimaryIndustryTagId = taxonomies[0].TagId.ToString()
            }
        };
        ccmService
            .GetAllCasesModifiedAfter(Arg.Any<DateOnly>())
            .Returns(cases);

        var caseRepository = Substitute.For<ISingleCaseRepository>();
        foreach (var caseDetails in cases)
        {
            caseRepository
                .GetNonRetainerCaseByCaseCode(caseDetails.CaseCode)
                .Returns(new CaseEntity(caseDetails.CaseCode, caseDetails.CaseCode, RelationshipType.NonRetainer));
        }

        // Act
        var service = PrepareService(
            stateRepository,
            caseRepository,
            ccmService,
            industries: taxonomies,
            capabilities: taxonomies,
            pegIndustries: Array.Empty<int>(),
            pegCapabilities: Array.Empty<int>());
        await service.ImportCases();

        // Assert
        await caseRepository
            .Received(0)
            .UpdateCase(Arg.Any<CaseEntity>());
    }

    [Fact]
    public async Task ImportCasesByCaseCodes_WhenNotPegTaxonomy_CaseSkipped()
    {
        // Arrange
        var taxonomies =
            new[] { new TermDto { TagId = Guid.NewGuid(), Name = nameof(TermDto.Name), Id = 1 } };

        var ccmService = Substitute.For<IClientCaseApiService>();
        var cases = new[]
        {
            new CaseDetailsDto("1")
            {
                CaseName = $"{nameof(CaseDetailsDto)}_1",
                PrimaryCapabilityTagId = taxonomies[0].TagId.ToString(),
                PrimaryIndustryTagId = taxonomies[0].TagId.ToString()
            },
            new CaseDetailsDto("2")
            {
                CaseName = $"{nameof(CaseDetailsDto)}_2",
                PrimaryCapabilityTagId = taxonomies[0].TagId.ToString(),
                PrimaryIndustryTagId = taxonomies[0].TagId.ToString()
            }
        };
        var caseCodes = cases.Select(c => c.CaseCode).ToArray();
        ccmService
            .GetCasesByCaseCodes(Arg.Is<IReadOnlyCollection<string>>(c => c.SequenceEqual(caseCodes)))
            .Returns(cases);

        var caseRepository = Substitute.For<ISingleCaseRepository>();
        foreach (var caseDetails in cases)
        {
            caseRepository
                .GetNonRetainerCaseByCaseCode(caseDetails.CaseCode)
                .Returns(new CaseEntity(caseDetails.CaseCode, caseDetails.CaseCode, RelationshipType.NonRetainer));
        }

        // Act
        var service = PrepareService(
            caseRepository: caseRepository,
            ccmService: ccmService,
            industries: taxonomies,
            capabilities: taxonomies,
            pegIndustries: Array.Empty<int>(),
            pegCapabilities: Array.Empty<int>());
        var (updatedCaseCodes, createdCaseCodes) = await service.ImportCasesByCaseCodes(caseCodes);

        // Assert
        updatedCaseCodes
            .Should()
            .BeEmpty();
        createdCaseCodes
            .Should()
            .BeEmpty();
        await caseRepository
            .Received(0)
            .UpdateCase(Arg.Any<CaseEntity>());
    }

    [Fact]
    public async Task ImportCases_WhenExistingCasesFound_CasesUpdated()
    {
        // Arrange
        var taxonomies =
            new[] { new TermDto { TagId = Guid.NewGuid(), Name = nameof(TermDto.Name), Id = 1 } };
        var stateRepository = Substitute.For<IDataImportStateRepository<CaseDataImportState>>();
        stateRepository
            .GetState()
            .Returns((CaseDataImportState?)null);

        var ccmService = Substitute.For<IClientCaseApiService>();
        var ccmCases = new[]
        {
            new CaseDetailsDto(" 1 ")
            {
                CaseName = $"{nameof(CaseDetailsDto)}_1",
                PrimaryCapabilityTagId = taxonomies[0].TagId.ToString(),
                PrimaryIndustryTagId = taxonomies[0].TagId.ToString()
            },
            new CaseDetailsDto("2")
            {
                CaseName = $"{nameof(CaseDetailsDto)}_2",
                PrimaryCapabilityTagId = taxonomies[0].TagId.ToString(),
                PrimaryIndustryTagId = taxonomies[0].TagId.ToString()
            }
        };
        ccmService
            .GetAllCasesModifiedAfter(Arg.Any<DateOnly>())
            .Returns(ccmCases);

        var caseRepository = Substitute.For<ISingleCaseRepository>();
        foreach (var caseDetails in ccmCases)
        {
            caseRepository
                .GetNonRetainerCaseByCaseCode(caseDetails.CaseCode)
                .Returns(new CaseEntity(caseDetails.CaseCode, caseDetails.CaseCode, RelationshipType.NonRetainer));
        }

        // Act
        var service = PrepareService(stateRepository, caseRepository, ccmService, industries: taxonomies, capabilities: taxonomies);
        await service.ImportCases();

        // Assert
        await caseRepository
            .Received()
            .UpdateCase(Arg.Is<CaseEntity>(c => c.CaseCode == ccmCases[0].CaseCode.Trim()));
        await caseRepository
            .Received()
            .UpdateCase(Arg.Is<CaseEntity>(c => c.CaseCode == ccmCases[1].CaseCode.Trim()));
    }

    [Fact]
    public async Task ImportCasesByCaseCodes_WhenExistingCasesFound_CasesUpdated()
    {
        // Arrange
        var taxonomies =
            new[] { new TermDto { TagId = Guid.NewGuid(), Name = nameof(TermDto.Name), Id = 1 } };

        var ccmService = Substitute.For<IClientCaseApiService>();
        var ccmCases = new[]
        {
            new CaseDetailsDto("1")
            {
                CaseName = $"{nameof(CaseDetailsDto)}_1",
                PrimaryCapabilityTagId = taxonomies[0].TagId.ToString(),
                PrimaryIndustryTagId = taxonomies[0].TagId.ToString()
            },
            new CaseDetailsDto("2")
            {
                CaseName = $"{nameof(CaseDetailsDto)}_2",
                PrimaryCapabilityTagId = taxonomies[0].TagId.ToString(),
                PrimaryIndustryTagId = taxonomies[0].TagId.ToString()
            }
        };
        var caseCodes = ccmCases.Select(c => c.CaseCode).ToArray();
        ccmService
            .GetCasesByCaseCodes(Arg.Is<IReadOnlyCollection<string>>(c => c.SequenceEqual(caseCodes)))
            .Returns(ccmCases);

        var caseRepository = Substitute.For<ISingleCaseRepository>();
        foreach (var caseDetails in ccmCases)
        {
            caseRepository
                .GetNonRetainerCaseByCaseCode(caseDetails.CaseCode)
                .Returns(new CaseEntity(caseDetails.CaseCode, caseDetails.CaseCode, RelationshipType.NonRetainer));
        }

        // Act
        var service = PrepareService(caseRepository: caseRepository, ccmService: ccmService, industries: taxonomies, capabilities: taxonomies);
        var (updatedCaseCodes, createdCaseCodes) = await service.ImportCasesByCaseCodes(caseCodes);

        // Assert
        updatedCaseCodes
            .Should()
            .BeEquivalentTo(caseCodes);
        createdCaseCodes
            .Should()
            .BeEmpty();
        await caseRepository
            .Received()
            .UpdateCase(Arg.Is<CaseEntity>(c => c.CaseCode == ccmCases[0].CaseCode));
        await caseRepository
            .Received()
            .UpdateCase(Arg.Is<CaseEntity>(c => c.CaseCode == ccmCases[1].CaseCode));
    }

    [Fact]
    public async Task ImportCases_WhenNotExistingCasesFound_CasesCreated()
    {
        // Arrange
        var taxonomies =
            new[] { new TermDto { TagId = Guid.NewGuid(), Name = nameof(TermDto.Name), Id = 1 } };
        var stateRepository = Substitute.For<IDataImportStateRepository<CaseDataImportState>>();
        stateRepository
            .GetState()
            .Returns((CaseDataImportState?)null);

        var ccmService = Substitute.For<IClientCaseApiService>();
        var cases = new[]
        {
            new CaseDetailsDto("1")
            {
                CaseName = $"{nameof(CaseDetailsDto)}_1",
                PrimaryCapabilityTagId = taxonomies[0].TagId.ToString(),
                PrimaryIndustryTagId = taxonomies[0].TagId.ToString()
            },
            new CaseDetailsDto("2")
            {
                CaseName = $"{nameof(CaseDetailsDto)}_2",
                PrimaryCapabilityTagId = taxonomies[0].TagId.ToString(),
                PrimaryIndustryTagId = taxonomies[0].TagId.ToString()
            }
        };
        ccmService
            .GetAllCasesModifiedAfter(Arg.Any<DateOnly>())
            .Returns(cases);

        var caseRepository = Substitute.For<ISingleCaseRepository>();
        foreach (var caseDetails in cases)
        {
            caseRepository
                .GetNonRetainerCaseByCaseCode(caseDetails.CaseCode)
                .Returns((CaseEntity)null!);
        }

        // Act
        var service = PrepareService(stateRepository, caseRepository, ccmService, industries: taxonomies, capabilities: taxonomies);
        await service.ImportCases();

        // Assert
        await caseRepository
            .Received(cases.Length)
            .AddCase(Arg.Any<CaseEntity>());
    }

    [Fact]
    public async Task ImportCasesByCaseCodes_WhenNotExistingCasesFound_CasesCreated()
    {
        // Arrange
        var taxonomies =
            new[] { new TermDto { TagId = Guid.NewGuid(), Name = nameof(TermDto.Name), Id = 1 } };

        var ccmService = Substitute.For<IClientCaseApiService>();
        var cases = new[]
        {
            new CaseDetailsDto("1")
            {
                CaseName = $"{nameof(CaseDetailsDto)}_1",
                PrimaryCapabilityTagId = taxonomies[0].TagId.ToString(),
                PrimaryIndustryTagId = taxonomies[0].TagId.ToString()
            },
            new CaseDetailsDto("2")
            {
                CaseName = $"{nameof(CaseDetailsDto)}_2",
                PrimaryCapabilityTagId = taxonomies[0].TagId.ToString(),
                PrimaryIndustryTagId = taxonomies[0].TagId.ToString()
            }
        };
        var caseCodes = cases.Select(c => c.CaseCode).ToArray();
        ccmService
            .GetCasesByCaseCodes(Arg.Is<IReadOnlyCollection<string>>(c => c.SequenceEqual(caseCodes)))
            .Returns(cases);

        var caseRepository = Substitute.For<ISingleCaseRepository>();
        foreach (var caseDetails in cases)
        {
            caseRepository
                .GetNonRetainerCaseByCaseCode(caseDetails.CaseCode)
                .Returns((CaseEntity)null!);
        }

        // Act
        var service = PrepareService(caseRepository: caseRepository, ccmService: ccmService, industries: taxonomies, capabilities: taxonomies);
        var (updatedCaseCodes, createdCaseCodes) = await service.ImportCasesByCaseCodes(caseCodes);

        // Assert
        updatedCaseCodes
            .Should()
            .BeEmpty();
        createdCaseCodes
            .Should()
            .BeEquivalentTo(caseCodes);
        await caseRepository
            .Received(cases.Length)
            .AddCase(Arg.Any<CaseEntity>());
    }

    [Fact]
    public async Task ImportCases_WithKMLeadFromIRIS_WhenCaseExistsInIRIS_CasesCreated()
    {
        // Arrange
        var taxonomies =
            new[] { new TermDto { TagId = Guid.NewGuid(), Name = nameof(TermDto.Name), Id = 1 } };
        var stateRepository = Substitute.For<IDataImportStateRepository<CaseDataImportState>>();
        stateRepository
            .GetState()
            .Returns((CaseDataImportState?)null);

        var ccmService = Substitute.For<IClientCaseApiService>();
        var cases = new[]
        {
            new CaseDetailsDto("1")
            {
                CaseName = $"{nameof(CaseDetailsDto)}_1",
                PrimaryCapabilityTagId = taxonomies[0].TagId.ToString(),
                PrimaryIndustryTagId = taxonomies[0].TagId.ToString()
            }
        };
        ccmService
            .GetAllCasesModifiedAfter(Arg.Any<DateOnly>())
            .Returns(cases);

        var caseRepository = Substitute.For<ISingleCaseRepository>();
        foreach (var caseDetails in cases)
        {
            caseRepository
                .GetNonRetainerCaseByCaseCode(caseDetails.CaseCode)
                .Returns((CaseEntity)null!);
        }

        var irisService = Substitute.For<IIrisIntegrationService>();
        irisService
            .GetCasesByCaseCodes(Arg.Is<IReadOnlyList<string>>(l => l[0] == "1"))
            .Returns(new List<IrisCaseDto> { new("1") { LeadKnowledgeSpecialist = "eCode1" } });

        // Act
        var service = PrepareService(
            stateRepository: stateRepository,
            caseRepository: caseRepository,
            ccmService: ccmService,
            industries: taxonomies,
            capabilities: taxonomies,
            irisService: irisService);
        await service.ImportCases();

        // Assert
        await caseRepository
            .Received(cases.Length)
            .AddCase(Arg.Is<CaseEntity>(c => c.LeadKnowledgeSpecialistEcode == "eCode1"));
    }

    [Fact]
    public async Task ImportCases_WithKMLeadFromIRIS_WhenCaseNotExistsInIRIS_CasesCreated()
    {
        // Arrange
        var taxonomies =
            new[] { new TermDto { TagId = Guid.NewGuid(), Name = nameof(TermDto.Name), Id = 1 } };
        var stateRepository = Substitute.For<IDataImportStateRepository<CaseDataImportState>>();
        stateRepository
            .GetState()
            .Returns((CaseDataImportState?)null);

        var ccmService = Substitute.For<IClientCaseApiService>();
        var cases = new[]
        {
            new CaseDetailsDto("1")
            {
                CaseName = $"{nameof(CaseDetailsDto)}_1",
                PrimaryCapabilityTagId = taxonomies[0].TagId.ToString(),
                PrimaryIndustryTagId = taxonomies[0].TagId.ToString()
            }
        };
        ccmService
            .GetAllCasesModifiedAfter(Arg.Any<DateOnly>())
            .Returns(cases);

        var caseRepository = Substitute.For<ISingleCaseRepository>();
        foreach (var caseDetails in cases)
        {
            caseRepository
                .GetNonRetainerCaseByCaseCode(caseDetails.CaseCode)
                .Returns((CaseEntity)null!);
        }

        // Act
        var service = PrepareService(stateRepository, caseRepository, ccmService, industries: taxonomies, capabilities: taxonomies);
        await service.ImportCases();

        // Assert
        await caseRepository
            .Received(cases.Length)
            .AddCase(Arg.Is<CaseEntity>(c => c.LeadKnowledgeSpecialistEcode == null));
    }

    [Fact]
    public async Task ImportCases_WhenExistingCaseFound_CaseDetailsMappedProperly()
    {
        // Arrange
        var primaryIndustry = new TermDto { Id = 123, TagId = Guid.NewGuid(), Name = "primary-ind" };
        var secondaryIndustry1 = new TermDto { Id = 1231, TagId = Guid.NewGuid(), Name = "secondary-ind1" };
        var secondaryIndustry2 = new TermDto { Id = 1232, TagId = Guid.NewGuid(), Name = "secondary-ind2" };
        var primaryCapability = new TermDto { Id = 321, TagId = Guid.NewGuid(), Name = "primary-cap" };
        var secondaryCapability1 = new TermDto { Id = 3211, TagId = Guid.NewGuid(), Name = "secondary-cap1" };
        var secondaryCapability2 = new TermDto { Id = 3212, TagId = Guid.NewGuid(), Name = "secondary-cap2" };
        var office = new TermDto { OfficeCode = 123321, Name = "office", OfficeCluster = "cluster", Parent = new TermDto { Name = "region" } };
        var ccmCase = new CaseDetailsDto("1")
        {
            BillingPartner = "billing-partner",
            CaseManager = "case-manager",
            CaseName = "case-name",
            CaseOffice = office.OfficeCode,
            ClientId = 456,
            ClientName = "client-name",
            GlobalCoordinatingPartner = "global-coordinating-partner",
            PrimaryCapabilityTagId = primaryCapability.TagId.ToString(),
            PrimaryIndustryTagId = primaryIndustry.TagId.ToString(),
            SecondaryCapability = new[]
            {
                new CaseDetailsDto.TaxonomyTerm(secondaryCapability1.TagId.ToString()!),
                new CaseDetailsDto.TaxonomyTerm(secondaryCapability2.TagId.ToString()!)
            },
            SecondaryIndustry = new[]
            {
                new CaseDetailsDto.TaxonomyTerm(secondaryIndustry1.TagId.ToString()!),
                new CaseDetailsDto.TaxonomyTerm(secondaryIndustry2.TagId.ToString()!)
            },
            StartDate = new DateTime(2022, 12, 05),
            EndDate = new DateTime(2022, 12, 10)
        };
        var existingEntity = new CaseEntity(Guid.NewGuid().ToString(), ccmCase.CaseCode, RelationshipType.NonRetainer)
        {
            FinalMaterialAvailable = FinalMaterialAvailable.NoPendingSubmission,
            ItemStage = CaseState.SurveyOpened
        };

        var ccmService = Substitute.For<IClientCaseApiService>();
        ccmService
            .GetAllCasesModifiedAfter(Arg.Any<DateOnly>())
            .Returns(new[] { ccmCase });

        var caseRepository = Substitute.For<ISingleCaseRepository>();
        caseRepository
            .GetNonRetainerCaseByCaseCode(ccmCase.CaseCode)
            .Returns(existingEntity);

        // Act
        var service = PrepareService(
            caseRepository: caseRepository,
            ccmService: ccmService,
            industries: new[] { primaryIndustry, secondaryIndustry1, secondaryIndustry2 },
            capabilities: new[] { primaryCapability, secondaryCapability1, secondaryCapability2 },
            offices: new[] { office });
        await service.ImportCases();

        // Assert
        await caseRepository
            .Received()
            .UpdateCase(Arg.Is<CaseEntity>(entity => VerifyUpdatedProperties(entity, ccmCase, existingEntity)));
    }

    [Fact]
    public async Task ImportCases_WhenNoCcmPropertyUpdated_UpdateNotCalled()
    {
        // Arrange
        var primaryIndustry = new TermDto { Id = 123, TagId = Guid.NewGuid(), Name = "primary-ind" };
        var secondaryIndustry1 = new TermDto { Id = 1231, TagId = Guid.NewGuid(), Name = "secondary-ind1" };
        var secondaryIndustry2 = new TermDto { Id = 1232, TagId = Guid.NewGuid(), Name = "secondary-ind2" };
        var primaryCapability = new TermDto { Id = 321, TagId = Guid.NewGuid(), Name = "primary-cap" };
        var secondaryCapability1 = new TermDto { Id = 3211, TagId = Guid.NewGuid(), Name = "secondary-cap1" };
        var secondaryCapability2 = new TermDto { Id = 3212, TagId = Guid.NewGuid(), Name = "secondary-cap2" };
        var office = new TermDto { OfficeCode = 123321, Name = "office", OfficeCluster = "cluster", Parent = new TermDto { Name = "region" } };
        var ccmCase = new CaseDetailsDto("1")
        {
            BillingPartner = "billing-partner",
            CaseManager = "case-manager",
            CaseName = "case-name",
            CaseOffice = office.OfficeCode,
            ClientId = 456,
            ClientName = "client-name",
            GlobalCoordinatingPartner = "global-coordinating-partner",
            PrimaryCapabilityTagId = primaryCapability.TagId.ToString(),
            PrimaryIndustryTagId = primaryIndustry.TagId.ToString(),
            StartDate = new DateTime(2022, 12, 05),
            EndDate = new DateTime(2022, 12, 10)
        };
        var existingEntity = new CaseEntity(Guid.NewGuid().ToString(), ccmCase.CaseCode, RelationshipType.NonRetainer)
        {
            FinalMaterialAvailable = FinalMaterialAvailable.NoPendingSubmission,
            ItemStage = CaseState.SurveyOpened,
            BillingPartnerEcode = ccmCase.BillingPartner,
            ManagerEcode = ccmCase.CaseManager,
            CaseName = ccmCase.CaseName,
            ManagingOffice = new TaxonomyOffice(
                office.OfficeCode,
                office.Name,
                office.OfficeCluster,
                office.Parent.Name),
            ClientId = ccmCase.ClientId.ToString(),
            ClientName = ccmCase.ClientName,
            ClientHeadEcode = ccmCase.GlobalCoordinatingPartner,
            PrimaryIndustry = new TaxonomyItem(primaryIndustry.Id, primaryIndustry.Name),
            PrimaryCapability = new TaxonomyItem(primaryCapability.Id, primaryCapability.Name),
            StartDate = ccmCase.StartDate,
            EndDate = ccmCase.EndDate,
            LeadKnowledgeSpecialistEcode = "KSEcode",
            OperatingPartnerEcodes = new List<string> { "EMP001", "EMP002" },
            AdvisorsEcodes = new List<string> { "ADV01", "ADV02" },
        };

        var ccmService = Substitute.For<IClientCaseApiService>();
        ccmService
            .GetAllCasesModifiedAfter(Arg.Any<DateOnly>())
            .Returns(new[] { ccmCase });

        var caseRepository = Substitute.For<ISingleCaseRepository>();
        caseRepository
            .GetNonRetainerCaseByCaseCode(ccmCase.CaseCode)
            .Returns(existingEntity);
        var staffingService = Substitute.For<IStaffingApiService>();
        staffingService
            .GetCasesTeamMembers(Arg.Is<IReadOnlyList<string>>(r => r.SequenceEqual(new[] { ccmCase.CaseCode })))
            .Returns(
                new Dictionary<string, CaseTeamMembers>
                {
                    { ccmCase.CaseCode, new CaseTeamMembers(new List<string> { "EMP001", "EMP002" }, new List<string> {"ADV01", "ADV02"}) }
                });

        var irisService = Substitute.For<IIrisIntegrationService>();
        irisService
            .GetCasesByCaseCodes(Arg.Is<IReadOnlyList<string>>(l => l[0] == ccmCase.CaseCode))
            .Returns(new List<IrisCaseDto> { new(ccmCase.CaseCode) { LeadKnowledgeSpecialist = "KSEcode" } });

        // Act
        var service = PrepareService(
            caseRepository: caseRepository,
            ccmService: ccmService,
            staffingApiService: staffingService,
            irisService: irisService,
            industries: new[] { primaryIndustry, secondaryIndustry1, secondaryIndustry2 },
            capabilities: new[] { primaryCapability, secondaryCapability1, secondaryCapability2 },
            offices: new[] { office });
        await service.ImportCases();

        // Assert
        await caseRepository
            .DidNotReceive()
            .UpdateCase(Arg.Any<CaseEntity>());
    }

    [Fact]
    public async Task ImportCasesByCaseCodes_WhenExistingCaseFound_CaseDetailsMappedProperly()
    {
        // Arrange
        var primaryIndustry = new TermDto { Id = 123, TagId = Guid.NewGuid(), Name = "primary-ind" };
        var secondaryIndustry1 = new TermDto { Id = 1231, TagId = Guid.NewGuid(), Name = "secondary-ind1" };
        var secondaryIndustry2 = new TermDto { Id = 1232, TagId = Guid.NewGuid(), Name = "secondary-ind2" };
        var primaryCapability = new TermDto { Id = 321, TagId = Guid.NewGuid(), Name = "primary-cap" };
        var secondaryCapability1 = new TermDto { Id = 3211, TagId = Guid.NewGuid(), Name = "secondary-cap1" };
        var secondaryCapability2 = new TermDto { Id = 3212, TagId = Guid.NewGuid(), Name = "secondary-cap2" };
        var office = new TermDto { OfficeCode = 123321, Name = "office", OfficeCluster = "cluster", Parent = new TermDto { Name = "region" } };
        var ccmCase = new CaseDetailsDto("1")
        {
            BillingPartner = "billing-partner",
            CaseManager = "case-manager",
            CaseName = "case-name",
            CaseOffice = office.OfficeCode,
            ClientId = 456,
            ClientName = "client-name",
            GlobalCoordinatingPartner = "global-coordinating-partner",
            PrimaryCapabilityTagId = primaryCapability.TagId.ToString(),
            PrimaryIndustryTagId = primaryIndustry.TagId.ToString(),
            SecondaryCapability = new[]
            {
                new CaseDetailsDto.TaxonomyTerm(secondaryCapability1.TagId.ToString()!),
                new CaseDetailsDto.TaxonomyTerm(secondaryCapability2.TagId.ToString()!)
            },
            SecondaryIndustry = new[]
            {
                new CaseDetailsDto.TaxonomyTerm(secondaryIndustry1.TagId.ToString()!),
                new CaseDetailsDto.TaxonomyTerm(secondaryIndustry2.TagId.ToString()!)
            },
            StartDate = new DateTime(2022, 12, 05),
            EndDate = new DateTime(2022, 12, 10)
        };
        var existingEntity = new CaseEntity(Guid.NewGuid().ToString(), ccmCase.CaseCode, RelationshipType.NonRetainer)
        {
            FinalMaterialAvailable = FinalMaterialAvailable.NoPendingSubmission,
            ItemStage = CaseState.SurveyOpened
        };

        var caseCodes = new[] { ccmCase.CaseCode };
        var ccmService = Substitute.For<IClientCaseApiService>();
        ccmService
            .GetCasesByCaseCodes(Arg.Is<IReadOnlyCollection<string>>(c => c.SequenceEqual(caseCodes)))
            .Returns(new[] { ccmCase });

        var caseRepository = Substitute.For<ISingleCaseRepository>();
        caseRepository
            .GetNonRetainerCaseByCaseCode(ccmCase.CaseCode)
            .Returns(existingEntity);

        // Act
        var service = PrepareService(
            caseRepository: caseRepository,
            ccmService: ccmService,
            industries: new[] { primaryIndustry, secondaryIndustry1, secondaryIndustry2 },
            capabilities: new[] { primaryCapability, secondaryCapability1, secondaryCapability2 },
            offices: new[] { office });
        var (updatedCaseCodes, createdCaseCodes) = await service.ImportCasesByCaseCodes(caseCodes);

        // Assert
        updatedCaseCodes
            .Should()
            .BeEquivalentTo(caseCodes);
        createdCaseCodes
            .Should()
            .BeEmpty();
        await caseRepository
            .Received()
            .UpdateCase(Arg.Is<CaseEntity>(entity => VerifyUpdatedProperties(entity, ccmCase, existingEntity)));
    }

    [Fact]
    public async Task ImportCases_WhenCasesFoundWithExistingResourceAllocations_CaseCreatedOrUpdatedWithDistinctOperatingPartnerEcodes()
    {
        // Arrange
        var taxonomies =
            new[] { new TermDto { TagId = Guid.NewGuid(), Name = nameof(TermDto.Name), Id = 1 } };

        var ccmService = Substitute.For<IClientCaseApiService>();
        const string caseCode1 = "1";
        const string caseCode2 = "2";
        const string caseCode3 = "3";
        var cases = new[]
        {
            new CaseDetailsDto(caseCode1)
            {
                CaseName = $"{nameof(CaseDetailsDto)}_1",
                PrimaryCapabilityTagId = taxonomies[0].TagId.ToString(),
                PrimaryIndustryTagId = taxonomies[0].TagId.ToString()
            },
            new CaseDetailsDto(caseCode2)
            {
                CaseName = $"{nameof(CaseDetailsDto)}_2",
                PrimaryCapabilityTagId = taxonomies[0].TagId.ToString(),
                PrimaryIndustryTagId = taxonomies[0].TagId.ToString()
            },
            new CaseDetailsDto(caseCode3)
            {
                CaseName = $"{nameof(CaseDetailsDto)}_3",
                PrimaryCapabilityTagId = taxonomies[0].TagId.ToString(),
                PrimaryIndustryTagId = taxonomies[0].TagId.ToString()
            }
        };
        ccmService
            .GetAllCasesModifiedAfter(Arg.Any<DateOnly>())
            .Returns(cases);

        var caseRepository = Substitute.For<ISingleCaseRepository>();
        caseRepository
            .GetNonRetainerCaseByCaseCode(Arg.Is<string>(c => c == caseCode1 || c == caseCode3))
            .Returns((CaseEntity)null!);

        caseRepository
                .GetNonRetainerCaseByCaseCode(caseCode2)
                .Returns(new CaseEntity(caseCode2, caseCode2, RelationshipType.NonRetainer));

        var staffingService = Substitute.For<IStaffingApiService>();
        staffingService
            .GetCasesTeamMembers(
                Arg.Is<IReadOnlyCollection<string>>(r => r.SequenceEqual(new[] { caseCode1, caseCode2, caseCode3 }))
            )
            .Returns(
                new Dictionary<string, CaseTeamMembers>
                {
                    { caseCode1, new CaseTeamMembers(new List<string> { "EMP001", "EMP002" }, new List<string> {"ADV01", "ADV02"}) },
                    { caseCode2, new CaseTeamMembers(new List<string> {"EMP003" }, new List<string> {"ADV03"}) },
                });

        // Act
        var service = PrepareService(caseRepository: caseRepository, ccmService: ccmService, staffingApiService: staffingService, industries: taxonomies, capabilities: taxonomies);
        await service.ImportCases();

        // Assert
        await caseRepository
            .Received()
            .AddCase(
                Arg.Is<CaseEntity>(
                    c => c.CaseCode == caseCode1 &&
                         c.OperatingPartnerEcodes!.SequenceEqual(new[] { "EMP001", "EMP002" }) &&
                         c.AdvisorsEcodes!.SequenceEqual(new[] { "ADV01", "ADV02" })
                         ));
        await caseRepository
            .Received()
            .UpdateCase(
                Arg.Is<CaseEntity>(
                    c => c.CaseCode == caseCode2 &&
                         c.OperatingPartnerEcodes!.SequenceEqual(new[] { "EMP003" }) &&
                         c.AdvisorsEcodes!.SequenceEqual(new[] { "ADV03" })));
        await caseRepository
            .Received()
            .AddCase(Arg.Is<CaseEntity>(c => c.CaseCode == caseCode3
                                             && c.OperatingPartnerEcodes == null
                                             && c.AdvisorsEcodes == null));
    }

    private static bool VerifyUpdatedProperties(
        CaseEntity updatedEntity,
        CaseDetailsDto ccmEntity,
        CaseEntity existingEntity)
    {
        updatedEntity.ModifiedBy
            .Should()
            .BeEquivalentTo(new UserInfo(UserType.Service, nameof(CaseDataImportService)));
        updatedEntity.ClientId
            .Should()
            .Be(ccmEntity.ClientId.ToString());
        updatedEntity.PrimaryIndustry
            .Should()
            .BeEquivalentTo(new TaxonomyItem(123, "primary-ind"));
        updatedEntity.PrimaryCapability
            .Should()
            .BeEquivalentTo(new TaxonomyItem(321, "primary-cap"));
        updatedEntity.SecondaryIndustries
            .Should()
            .BeNull();
        updatedEntity.SecondaryCapabilities
            .Should()
            .BeNull();
        updatedEntity.ManagingOffice
            .Should()
            .BeEquivalentTo(new TaxonomyOffice(123321, "office", "cluster", "region"));
        updatedEntity.ClientHeadEcode
            .Should()
            .Be(ccmEntity.GlobalCoordinatingPartner);
        updatedEntity.ManagerEcode
            .Should()
            .Be(ccmEntity.CaseManager);
        updatedEntity.BillingPartnerEcode
            .Should()
            .Be(ccmEntity.BillingPartner);
        updatedEntity.CaseName
            .Should()
            .Be(ccmEntity.CaseName);
        updatedEntity.ClientName
            .Should()
            .Be(ccmEntity.ClientName);
        updatedEntity.StartDate
            .Should()
            .Be(ccmEntity.StartDate);
        updatedEntity.EndDate
            .Should()
            .Be(ccmEntity.EndDate);
        updatedEntity.LeadKnowledgeSpecialistEcode
            .Should()
            .Be(ccmEntity.LeadKnowledgeSpecialistEcode);

        // Not CCM properties should not be updated.
        updatedEntity.CaseCode
            .Should()
            .Be(existingEntity.CaseCode);
        updatedEntity.RelationshipType
            .Should()
            .Be(existingEntity.RelationshipType);

        return true;
    }

    private CaseDataImportService PrepareService(
        IDataImportStateRepository<CaseDataImportState>? stateRepository = null,
        ISingleCaseRepository? caseRepository = null,
        IClientCaseApiService? ccmService = null,
        IIrisIntegrationService? irisService = null,
        IStaffingApiService? staffingApiService = null,
        DateOnly? initialModifiedAfterTime = null,
        IReadOnlyCollection<TermDto>? industries = null,
        IReadOnlyCollection<TermDto>? capabilities = null,
        IReadOnlyCollection<TermDto>? offices = null,
        int[]? pegCapabilities = null,
        int[]? pegIndustries = null)
    {
        var options = Options.Create(new CaseDataImportOptions
        {
            InitialModifiedAfterTime = initialModifiedAfterTime ?? new DateOnly(2000, 1, 1),
            PegCapabilities = pegCapabilities ?? capabilities?.Select(c => c.Id).ToArray() ?? Array.Empty<int>(),
            PegIndustries = pegIndustries ?? industries?.Select(i => i.Id).ToArray() ?? Array.Empty<int>(),
        });

        ccmService ??= Substitute.For<IClientCaseApiService>();
        stateRepository ??= Substitute.For<IDataImportStateRepository<CaseDataImportState>>();
        caseRepository ??= Substitute.For<ISingleCaseRepository>();
        irisService ??= Substitute.For<IIrisIntegrationService>();
        var logger = Substitute.For<ILogger<CaseDataImportService>>();
        var taxonomyRepo = Substitute.For<ITaxonomyRepository>();
        taxonomyRepo
            .GetFlatTaxonomy(TaxonomyType.Industry)
            .Returns(industries?.ToDictionary(i => i.Id) ?? new Dictionary<int, TermDto>());
        taxonomyRepo
            .GetFlatTaxonomy(TaxonomyType.Capability)
            .Returns(capabilities?.ToDictionary(c => c.Id) ?? new Dictionary<int, TermDto>());
        taxonomyRepo
            .GetFlatTaxonomy(TaxonomyType.Office)
            .Returns(offices?.ToDictionary(o => o.Id) ?? new Dictionary<int, TermDto>());
        var factory = new TaxonomyServiceFactory(taxonomyRepo);
        staffingApiService ??= Substitute.For<IStaffingApiService>();
        var mapper = CreateMapper(
            new CcmTaxonomyMapping(
                factory,
                Substitute.For<ILogger<CcmTaxonomyMapping>>()));
        return new CaseDataImportService(
            options,
            ccmService,
            mapper,
            stateRepository,
            caseRepository,
            irisService,
            staffingApiService,
            logger);
    }
}