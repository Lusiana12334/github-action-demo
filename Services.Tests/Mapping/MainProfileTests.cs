using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.DataContracts.V1.CaseForms;
using PEXC.Case.Domain;
using PEXC.Case.Services.CCM.Contracts;
using PEXC.Case.Services.Infrastructure;
using PEXC.Case.Services.Mapping;
using PEXC.Case.Services.Mapping.EmployeeProfile;
using PEXC.Case.Services.Mapping.FieldMasking;
using PEXC.Case.Services.Mapping.Taxonomy;
using PEXC.Case.Services.Workflow;
using PEXC.Case.Tests.Common;
using PEXC.Common.BaseApi.Profile;
using PEXC.Common.BaseApi.Profile.DataContracts.V1;

namespace PEXC.Case.Services.Tests.Mapping;

public class MainProfileTests : MappingAwareTestsBase<MainProfile>
{
    [Fact]
    public void CaseEntityToCaseSearchItemDto()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        var primaryIndustry = new TaxonomyItem(1234, "Ind1234");
        caseEntity.PrimaryIndustry = primaryIndustry;
        var primaryCapability = new TaxonomyItem(2345, "Cap2345");
        caseEntity.PrimaryCapability = primaryCapability;
        var managingOffice = new TaxonomyOffice(345, "Office345", "Cluster345", "Region345");
        caseEntity.ManagingOffice = managingOffice;
        var secIndustry1 = new TaxonomyItem(2221, "SecInd222.1");
        var secIndustry2 = new TaxonomyItem(2222, "SecInd222.2");
        var secIndustry3 = new TaxonomyItem(2223, "SecInd222.3");
        caseEntity.SecondaryIndustries = new List<TaxonomyItem> { secIndustry1, secIndustry2, secIndustry3 };
        caseEntity.AdvancedAnalyticsUsage = new List<string> { "Item1", "Item2" };
        caseEntity.HistoricFields!.OpsDdComponent = "OpsDdComponent";

        var taxonomyService = Substitute.For<ITaxonomyService>();
        taxonomyService
            .MapIndustryTaxonomy(caseEntity.PrimaryIndustry)
            .Returns(primaryIndustry);
        taxonomyService
            .MapCapabilityTaxonomy(caseEntity.PrimaryCapability)
            .Returns(primaryCapability);
        taxonomyService
            .MapOfficeTaxonomy(caseEntity.ManagingOffice)
            .Returns(managingOffice);
        taxonomyService
            .MapIndustryTaxonomyPath(secIndustry1)
            .Returns(new[] { "Top1", "Sec1", "Prim1" });
        taxonomyService
            .MapIndustryTaxonomyPath(secIndustry2)
            .Returns(new[] { "Top2", "Sec2", "Prim2.1", "Prim2.2" });
        taxonomyService
            .MapIndustryTaxonomyPath(secIndustry3)
            .Returns(new[] { null, null, "Prim3" });

        // Act
        var result = ConfigureMapper(taxonomyService).Map<CaseSearchItemDto>(caseEntity);

        // Assert
        result.ClientType
            .Should()
            .Be(primaryIndustry.Name);
        result.CaseType
            .Should()
            .Be(primaryCapability.Name);
        result.ManagingOffice
            .Should()
            .Be(managingOffice.Name);
        result.Region
            .Should()
            .Be(managingOffice.Region);
        result.TopLevelIndustry
            .Should()
            .Be("Top1;Top2");
        result.SecondLevelIndustry
            .Should()
            .Be("Sec1;Sec2");
        result.PrimaryIndustry
            .Should()
            .Be("Prim1;Prim2.1;Prim2.2;Prim3");
        result.AdvancedAnalyticsUsage
            .Should()
            .Be(string.Join(';', caseEntity.AdvancedAnalyticsUsage));

        result.ModifiedBy
            .Should()
            .Be(GenerateEmployeeFullName(caseEntity.ModifiedBy!.UserEcode!));
        result.OperatingPartnerName
            .Should()
            .Be(string.Join("; ", caseEntity.OperatingPartnerEcodes!.Select(GenerateEmployeeFullName))); 
        result.AdvisorsNames
            .Should()
            .Be(string.Join("; ", caseEntity.AdvisorsEcodes!.Select(GenerateEmployeeFullName)));
        result.BillingPartnerName
            .Should()
            .Be(GenerateEmployeeFullName(caseEntity.BillingPartnerEcode!));
        result.ManagerName
            .Should()
            .Be(GenerateEmployeeFullName(caseEntity.ManagerEcode!));

        result.ClientName
            .Should()
            .Be(caseEntity.ClientName);
        result.TargetName
            .Should()
            .Be(caseEntity.TargetName);
        result.TargetDescription
            .Should()
            .Be(caseEntity.TargetDescription);
        result.MainCompetitorsAnalyzed
            .Should()
            .Be(caseEntity.MainCompetitorsAnalyzed);
        result.FinalMaterialAvailable
            .Should()
            .Be("N - Active Case");
        result.Keyword
            .Should()
            .Be(caseEntity.Keyword);
        result.IndustrySectorsAnalyzed
            .Should()
            .Be(caseEntity.IndustrySectorsAnalyzed);
        result.GeographicRegion
            .Should()
            .Be(caseEntity.GeographicRegion);
        result.OpsDdComponent
            .Should()
            .Be(caseEntity.HistoricFields.OpsDdComponent);
        result.OpsDdDuration
            .Should()
            .Be(caseEntity.OpsDdDuration);
        result.OpsDdTeam
            .Should()
            .Be(caseEntity.OpsDdTeam);
    }

    [Fact]
    public void CaseEntityToCaseSearchItemDto_WhenCaseAfterConfidentialGracePeriod_DataNotMaskedAsConfidential()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        caseEntity.IsInConfidentialGracePeriod = false;

        // Act
        var result = ConfigureMapper().Map<CaseSearchItemDto>(caseEntity);

        // Assert
        result.ClientName
            .Should()
            .Be(caseEntity.ClientName);
        result.TargetName
            .Should()
            .Be(caseEntity.TargetName);
    }

    [Fact]
    public void CaseEntityToCaseManagementItemDto()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        var primaryIndustry = new TaxonomyItem(1234, "Ind1234");
        caseEntity.PrimaryIndustry = primaryIndustry;
        var primaryCapability = new TaxonomyItem(2345, "Cap2345");
        caseEntity.PrimaryCapability = primaryCapability;
        var managingOffice = new TaxonomyOffice(345, "Office345", "Cluster345", "Region345");
        caseEntity.ManagingOffice = managingOffice;
        var secIndustry1 = new TaxonomyItem(2221, "SecInd222.1");
        var secIndustry2 = new TaxonomyItem(2222, "SecInd222.2");
        var secIndustry3 = new TaxonomyItem(2223, "SecInd222.3");
        var secIndustry4 = new TaxonomyItem(2224, "SecInd222.4");
        var secIndustry5 = new TaxonomyItem(2225, "SecInd222.5");
        caseEntity.SecondaryIndustries = new List<TaxonomyItem> { secIndustry1, secIndustry2, secIndustry3, secIndustry4, secIndustry5 };
        caseEntity.AdvancedAnalyticsUsage = new List<string> { "Item1", "Item2" };
        caseEntity.HistoricFields!.OpsDdComponent = "OpsDdComponent";
        caseEntity.SharePointDirectory = new SharePointDirectoryEntity { Url = "https://some.url.com" };
        caseEntity.BainExpertGroupsEcosystemPartnerUsage = new List<string> { "Item3", "Item4" };
        caseEntity.PrimaryResearch = new List<string> { "Item5", "Item6" };
        caseEntity.OpsMarginImprovementDetails = new List<string> { "Item7", "Item8" };
        caseEntity.PrimaryResearchRecommendation1 = new ResearchRecommendation("Test1", 1);
        caseEntity.PrimaryResearchRecommendation2 = new ResearchRecommendation("Test2", 2);
        caseEntity.PrimaryResearchRecommendation3 = new ResearchRecommendation("Test3", 3);
        caseEntity.Sensitive = true;

        var taxonomyService = Substitute.For<ITaxonomyService>();
        taxonomyService
            .MapIndustryTaxonomy(caseEntity.PrimaryIndustry)
            .Returns(primaryIndustry);
        taxonomyService
            .MapCapabilityTaxonomy(caseEntity.PrimaryCapability)
            .Returns(primaryCapability);
        taxonomyService
            .MapOfficeTaxonomy(caseEntity.ManagingOffice)
            .Returns(managingOffice);
        taxonomyService
            .MapIndustryTaxonomyPath(secIndustry1)
            .Returns(new[] { "Top1", "Sec1", "Prim1" });
        taxonomyService
            .MapIndustryTaxonomyPath(secIndustry2)
            .Returns(new[] { "Top2", "Sec2", "Prim2.1", "Prim2.2" });
        taxonomyService
            .MapIndustryTaxonomyPath(secIndustry3)
            .Returns(new[] { null, null, "Prim3" });
        taxonomyService
            .MapIndustryTaxonomyPath(secIndustry4)
            .Returns(new[] { "Top4" });
        taxonomyService
            .MapIndustryTaxonomyPath(secIndustry5)
            .Returns(new[] { "Top5", "Sec5" });

        // Act
        var result = ConfigureMapper(taxonomyService).Map<CaseManagementItemDto>(caseEntity);

        // Assert
        result.ClientType
            .Should()
            .Be(primaryIndustry.Name);
        result.CaseType
            .Should()
            .Be(primaryCapability.Name);
        result.ManagingOffice
            .Should()
            .Be(managingOffice.Name);
        result.Region
            .Should()
            .Be(managingOffice.Region);
        result.TopLevelIndustry
            .Should()
            .Be("Top1;Top2;Top4;Top5");
        result.SecondLevelIndustry
            .Should()
            .Be("Sec1;Sec2;Sec5");
        result.PrimaryIndustry
            .Should()
            .Be("Prim1;Prim2.1;Prim2.2;Prim3");
        result.AdvancedAnalyticsUsage
            .Should()
            .Be(string.Join(';', caseEntity.AdvancedAnalyticsUsage));
        result.OpsDdComponent
            .Should()
            .Be(caseEntity.HistoricFields.OpsDdComponent);
        result.CaseFolderUrl
            .Should()
            .Be(caseEntity.SharePointDirectory.Url);
        result.BainExpertGroupsEcosystemPartnerUsage
            .Should()
            .Be(string.Join(';', caseEntity.BainExpertGroupsEcosystemPartnerUsage));
        result.PrimaryResearch
            .Should()
            .Be(string.Join(';', caseEntity.PrimaryResearch));
        result.OpsMarginImprovementDetails
            .Should()
            .Be(string.Join(';', caseEntity.OpsMarginImprovementDetails));
        result.PrimaryResearchVendor1
            .Should()
            .Be(caseEntity.PrimaryResearchRecommendation1.Name);
        result.PrimaryResearchVendor2
            .Should()
            .Be(caseEntity.PrimaryResearchRecommendation2.Name);
        result.PrimaryResearchVendor3
            .Should()
            .Be(caseEntity.PrimaryResearchRecommendation3.Name);
        result.VendorNps1
            .Should()
            .Be(caseEntity.PrimaryResearchRecommendation1.Rating.ToString());
        result.VendorNps2
            .Should()
            .Be(caseEntity.PrimaryResearchRecommendation2.Rating.ToString());
        result.VendorNps3
            .Should()
            .Be(caseEntity.PrimaryResearchRecommendation3.Rating.ToString());

        result.ModifiedBy
            .Should()
            .Be(GenerateEmployeeFullName(caseEntity.ModifiedBy!.UserEcode!));
        result.CreatedBy
            .Should()
            .Be(GenerateEmployeeFullName(caseEntity.CreatedBy!.UserEcode!));
        result.OperatingPartnerName
            .Should()
            .Be(string.Join("; ", caseEntity.OperatingPartnerEcodes!.Select(GenerateEmployeeFullName)));
        result.BillingPartnerName
            .Should()
            .Be(GenerateEmployeeFullName(caseEntity.BillingPartnerEcode!));
        result.ManagerName
            .Should()
            .Be(GenerateEmployeeFullName(caseEntity.ManagerEcode!));
        result.BainExperts
            .Should()
            .Be(string.Join("; ", caseEntity.BainExpertsEcodes!.Select(GenerateEmployeeFullName)));
        result.KmContactName
            .Should()
            .Be(GenerateEmployeeFullName(caseEntity.LeadKnowledgeSpecialistEcode!));
        result.ClientHeadName
            .Should()
            .Be(GenerateEmployeeFullName(caseEntity.ClientHeadEcode!));
        result.ItemStage
            .Should()
            .Be(caseEntity.ItemStage.ToString());
        result.Sensitive.Should().BeTrue();
    }

    [Fact]
    public void CaseEntityToCaseEditDto()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        var expectedCaseDto = Fake.CaseEditDtoFromEntity(caseEntity);

        // Act
        var result = ConfigureMapper().Map<CaseEditDto>(caseEntity);

        // Assert
        result.Should().BeEquivalentTo(expectedCaseDto);
    }

    [Fact]
    public void CaseEditDtoToCaseEntity()
    {
        // Arrange
        var expectedCaseEntity = Fake.CaseEntity();
        //CorrelationId should not be returned to frontend
        expectedCaseEntity.CorrelationId = Guid.Empty;

        var caseDto = Fake.CaseEditDtoFromEntity(expectedCaseEntity);

        // Act
        var result = Mapper.Map<CaseEntity>(caseDto);

        // Assert
        result.Should().BeEquivalentTo(expectedCaseEntity, o =>
        {
            o.Excluding(c => c.Created);
            o.Excluding(c => c.CreatedBy);
            o.Excluding(c => c.Modified);
            o.Excluding(c => c.ModifiedBy);
            o.Excluding(c => c.IndustrySectorsAnalyzed);
            o.Excluding(c => c.OpsDdDuration);
            o.Excluding(c => c.OpsDdTeam);
            o.Excluding(c => c.HistoricFields);
            o.Excluding(c => c.IsInConfidentialGracePeriod);
            o.Excluding(c => c.UniqueId);
            o.Excluding(c => c.Year); 
            o.Excluding(c => c.AdvisorsEcodes); 
            o.Excluding(c => c.ETag);
            o.Excluding(c => c.Timestamp);
            return o;
        });
    }

    [Fact]
    public void CaseCreateDtoToCaseEntity()
    {
        // Arrange
        var expectedCaseEntity = Fake.CaseEntity(relationshipType: RelationshipType.Retainer);
        //CorrelationId should not be returned to frontend
        expectedCaseEntity.CorrelationId = Guid.Empty;
        expectedCaseEntity.Key = expectedCaseEntity.CaseCode;

        var caseCreateDto = Fake.CaseCreateDtoFromEntity(expectedCaseEntity);

        // Act
        var result = Mapper.Map<CaseEntity>(caseCreateDto);

        // Assert
        result.Should().BeEquivalentTo(expectedCaseEntity, o =>
        {
            o.Excluding(c => c.Created);
            o.Excluding(c => c.CreatedBy);
            o.Excluding(c => c.Modified);
            o.Excluding(c => c.ModifiedBy);
            o.Excluding(c => c.Id);
            o.Excluding(c => c.IndustrySectorsAnalyzed);
            o.Excluding(c => c.OpsDdDuration);
            o.Excluding(c => c.OpsDdTeam);
            o.Excluding(c => c.HistoricFields);
            o.Excluding(c => c.IsInConfidentialGracePeriod);
            o.Excluding(c => c.Year);
            o.Excluding(c => c.AdvisorsEcodes);
            o.Excluding(c => c.ETag);
            o.Excluding(c => c.Timestamp);
            return o;
        });
        result.Id.Should().NotBeNull();
    }

    [Fact]
    public void CaseEntityToSurveyDto()
    {
        // Arrange
        var caseEntity = Fake.CaseEntity();
        var expectedSurveyDto = Fake.SurveyDtoFromEntity(caseEntity);

        // Act
        var result = Mapper.Map<SurveyDto>(caseEntity);

        // Assert
        result.Should().BeEquivalentTo(expectedSurveyDto);
    }

    [Fact]
    public void SurveyDtoToCaseEntity()
    {
        // Arrange
        var expectedCaseEntity = Fake.CaseEntity();
        var surveyDto = Fake.SurveyDtoFromEntity(expectedCaseEntity);

        // Act
        var result = Mapper.Map<CaseEntity>(surveyDto);

        // Assert
        result.Should().BeEquivalentTo(expectedCaseEntity, o =>
        {
            o.Excluding(c => c.Created);
            o.Excluding(c => c.CreatedBy);
            o.Excluding(c => c.Modified);
            o.Excluding(c => c.ModifiedBy);
            o.Excluding(c => c.CorrelationId);
            o.Excluding(c => c.PrimaryCapability);
            o.Excluding(c => c.PrimaryIndustry);
            o.Excluding(c => c.ManagingOffice);
            o.Excluding(c => c.ClientHeadEcode);
            o.Excluding(c => c.BillingPartnerEcode);
            o.Excluding(c => c.OperatingPartnerEcodes);
            o.Excluding(c => c.ManagerEcode);
            o.Excluding(c => c.LeadKnowledgeSpecialistEcode);
            o.Excluding(c => c.SecondaryIndustries);
            o.Excluding(c => c.ClosedDeal);
            o.Excluding(c => c.Vdd);
            o.Excluding(c => c.Key);
            o.Excluding(c => c.IndustrySectorsAnalyzed);
            o.Excluding(c => c.OpsDdDuration);
            o.Excluding(c => c.OpsDdTeam);
            o.Excluding(c => c.HistoricFields);
            o.Excluding(c => c.IsInConfidentialGracePeriod);
            o.Excluding(c => c.UniqueId);
            o.Excluding(c => c.Year);
            o.Excluding(c => c.AdvisorsEcodes);
            o.Excluding(c => c.Sensitive);
            o.Excluding(c => c.ETag);
            o.Excluding(c => c.Timestamp);
            return o;
        });
    }

    [Fact]
    public void PagedResultToPagedResult()
    {
        // Arrange
        var caseEntities = new PagedResult<CaseEntity>
        {
            Items = new[] { Fake.CaseEntity(), Fake.CaseEntity() },
            NextPageToken = "ABC"
        };
        var expectedCaseDtos = new PagedResult<CaseEditDto>
        {
            Items = caseEntities.Items.Select(e => Fake.CaseEditDtoFromEntity(e)).ToList(),
            NextPageToken = "ABC"
        };

        // Act
        var result = ConfigureMapper().Map<PagedResult<CaseEditDto>>(caseEntities);

        // Assert
        result.Items.Count
            .Should()
            .Be(expectedCaseDtos.Items.Count);
        result.NextPageToken
            .Should()
            .Be(expectedCaseDtos.NextPageToken);
    }

    [Fact]
    public void CaseDetailsDtoToCaseEntity()
    {
        // Arrange
        var secondaryIndustries = new[] { new CaseDetailsDto.TaxonomyTerm("3"), new CaseDetailsDto.TaxonomyTerm("4") };
        var secondaryCapabilities = new[] { new CaseDetailsDto.TaxonomyTerm("1"), new CaseDetailsDto.TaxonomyTerm("2") };
        var caseDetails = new CaseDetailsDto("case-code")
        {
            BillingPartner = "billing-partner",
            CaseManager = "case-manager",
            CaseName = "case-name",
            CaseOffice = 1,
            ClientId = 2,
            ClientName = "client-name",
            StartDate = DateTime.Today.AddDays(-2),
            EndDate = DateTime.Today,
            GlobalCoordinatingPartner = "coordinating-partner",
            PrimaryCapabilityTagId = "primary-capability",
            PrimaryIndustryTagId = "primary-industry",
            SecondaryIndustry = secondaryIndustries,
            SecondaryCapability = secondaryCapabilities
        };

        var primaryIndustry = new TaxonomyItem(123, "SomeName");
        var primaryCapability = new TaxonomyItem(123, "SomeName");
        var secIndustry1 = new TaxonomyItem(2341, "SomeName");
        var secIndustry2 = new TaxonomyItem(2342, "SomeName");
        var secCapability1 = new TaxonomyItem(3451, "SomeName");
        var secCapability2 = new TaxonomyItem(3452, "SomeName");
        var managingOffice = new TaxonomyOffice(123, "SomeName", "SomeCluster", "SomeRegion");

        var taxonomyService = Substitute.For<ITaxonomyService>();
        taxonomyService
            .MapIndustryTagId(caseDetails.PrimaryIndustryTagId)
            .Returns(primaryIndustry);
        taxonomyService
            .MapCapabilityTagId(caseDetails.PrimaryCapabilityTagId)
            .Returns(primaryCapability);
        taxonomyService
            .MapIndustryTagId(secondaryIndustries[0].TagId)
            .Returns(secIndustry1);
        taxonomyService
            .MapIndustryTagId(secondaryIndustries[1].TagId)
            .Returns(secIndustry2);
        taxonomyService
            .MapCapabilityTagId(secondaryCapabilities[0].TagId)
            .Returns(secCapability1);
        taxonomyService
            .MapCapabilityTagId(secondaryCapabilities[1].TagId)
            .Returns(secCapability2);
        taxonomyService
            .MapOfficeTaxonomy(caseDetails.CaseOffice)
            .Returns(managingOffice);

        var expectedCaseEntity = new CaseEntity(string.Empty, caseDetails.CaseCode, RelationshipType.NonRetainer)
        {
            BillingPartnerEcode = caseDetails.BillingPartner,
            ManagerEcode = caseDetails.CaseManager,
            CaseName = caseDetails.CaseName,
            ClientId = caseDetails.ClientId.ToString(),
            ClientName = caseDetails.ClientName,
            StartDate = caseDetails.StartDate,
            EndDate = caseDetails.EndDate,
            ClientHeadEcode = caseDetails.GlobalCoordinatingPartner,
            PrimaryIndustry = primaryIndustry,
            PrimaryCapability = primaryCapability,
            SecondaryIndustries = new List<TaxonomyItem> { secIndustry1, secIndustry2 },
            SecondaryCapabilities = new List<TaxonomyItem> { secCapability1, secCapability2 },
            ManagingOffice = managingOffice,
            UniqueId = CaseDocumentHelper.GenerateUniqueId(caseDetails.CaseCode, caseDetails.CaseName, RelationshipType.NonRetainer),
        };

        // Act
        var result = ConfigureMapper(taxonomyService).Map<CaseEntity>(caseDetails);

        // Assert
        result
            .Should()
            .BeEquivalentTo(
                expectedCaseEntity,
                o => o.Excluding(c => c.Id));
    }

    [Fact]
    public void CaseDetailsDtoToCaseEntity_WhenSourceDateIsDefaultDateTime_TargetDateIsSetToNull()
    {
        // Arrange
        var caseDetails = new CaseDetailsDto("case-code")
        {
            StartDate = default(DateTime),
            EndDate = default(DateTime)
        };
        var expectedCaseEntity = new CaseEntity(string.Empty, caseDetails.CaseCode, RelationshipType.NonRetainer)
        {
            ClientId = "0",
            StartDate = null,
            EndDate = null
        };

        // Act
        var result = ConfigureMapper().Map<CaseEntity>(caseDetails);

        // Assert
        result
            .Should()
            .BeEquivalentTo(
                expectedCaseEntity,
                o =>
                {
                    o.Excluding(c => c.Id);
                    o.Excluding(c => c.PrimaryIndustry);
                    o.Excluding(c => c.PrimaryCapability);
                    o.Excluding(c => c.SecondaryIndustries);
                    o.Excluding(c => c.SecondaryCapabilities);
                    o.Excluding(c => c.ManagingOffice);
                    o.Excluding(c => c.UniqueId);
                    return o;
                });
    }

    [Fact]
    public void UpdateCaseEntityUsingTargetDetailsSection()
    {
        // Arrange
        const string ChangedTargetCountry = "NEW COUNTRY";
        var originalCaseEntity = Fake.CaseEntity("id");
        var expectedCaseEntity = Fake.CaseEntity("id");
        expectedCaseEntity.TargetCountry = ChangedTargetCountry;
        expectedCaseEntity.Sensitive = true;
        var mapper = ConfigureMapper();
        var caseEdit = mapper.Map<CaseEditDto>(originalCaseEntity);
        caseEdit.TargetDetailsSection!.TargetDetailsAndFinalDocumentsSection!.TargetCountry = ChangedTargetCountry;
        caseEdit.TargetDetailsSection!.TargetDetailsAndFinalDocumentsSection.Sensitive = true;
        // Act
        var result = mapper.Map(caseEdit.TargetDetailsSection, originalCaseEntity);

        // Assert
        result.Should().BeEquivalentTo(expectedCaseEntity, o =>
        {
            o.Excluding(c => c.Created);
            o.Excluding(c => c.Modified);
            o.Excluding(c => c.StartDate);
            o.Excluding(c => c.EndDate);
            o.Excluding(c => c.CorrelationId);
            return o;
        });
    }

    private IMapper ConfigureMapper(ITaxonomyService? taxonomyService = null)
    {
        if (taxonomyService == null)
        {
            taxonomyService = Substitute.For<ITaxonomyService>();
            taxonomyService
                .MapIndustryTagId(Arg.Any<string>())
                .Returns(TaxonomyItem.Empty);
            taxonomyService
                .MapCapabilityTagId(Arg.Any<string>())
                .Returns(TaxonomyItem.Empty);
            taxonomyService
                .MapIndustryTaxonomy(Arg.Any<TaxonomyItem>())
                .Returns(call => call.Arg<TaxonomyItem>());
            taxonomyService
                .MapCapabilityTaxonomy(Arg.Any<TaxonomyItem>())
                .Returns(call => call.Arg<TaxonomyItem>());
            taxonomyService
                .MapOfficeTaxonomy(Arg.Any<TaxonomyOffice>())
                .Returns(call => call.Arg<TaxonomyOffice>());
            taxonomyService
                .MapIndustryTaxonomyPath(Arg.Any<TaxonomyItem>())
                .Returns(call => new[] { null, null, call.Arg<TaxonomyItem>().Name });
        }

        var taxonomyServiceFactory = Substitute.For<ITaxonomyServiceFactory>();
        taxonomyServiceFactory
            .Create()
            .Returns(taxonomyService);
        var profileRepository = Substitute.For<IProfileRepository>();
        profileRepository
            .GetProfiles(Arg.Any<IReadOnlyList<string>>(), Arg.Any<string>())
            .Returns(
                call => call.Arg<IReadOnlyList<string>>()
                    .Select(
                        e => new EmployeeDetailsDto(
                            e,
                            $"First {e}",
                            $"Last {e}",
                            GenerateEmployeeFullName(e),
                            null,
                            null,
                            null,
                            false, 
                            null))
                    .ToList());

        var mappingOptions = Options.Create(new CoveoMappingOptions());
        return CreateMapper(
            new CoveoItemTaxonomyMapping<CaseSearchItemDto>(taxonomyServiceFactory, mappingOptions),
            new CoveoItemEmployeeProfileMapping<CaseSearchItemDto>(
                profileRepository,
                Substitute.For<ILogger<CoveoItemEmployeeProfileMapping<CaseSearchItemDto>>>()),
            new CaseSearchItemConfidentialDataMapping(Substitute.For<IFieldMaskingPolicy>()),
            new CoveoItemTaxonomyMapping<CaseManagementItemDto>(taxonomyServiceFactory, mappingOptions),
            new CaseManagementItemEmployeeProfileMapping(
                profileRepository,
                Substitute.For<ILogger<CaseManagementItemEmployeeProfileMapping>>()),
            new CaseEditTaxonomyMapping(taxonomyServiceFactory),
            new CcmTaxonomyMapping(taxonomyServiceFactory, Substitute.For<ILogger<CcmTaxonomyMapping>>()));
    }

    private static string GenerateEmployeeFullName(string ecode) => $"Last, First {ecode.ToLower()}";
}