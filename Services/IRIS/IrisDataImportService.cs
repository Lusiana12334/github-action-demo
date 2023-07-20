using Microsoft.Extensions.Logging;
using PEXC.Case.DataAccess.CosmosDB;
using PEXC.Case.DataAccess;
using PEXC.Case.Domain;
using PEXC.Case.Services.Infrastructure;
using PEXC.Common.Logging.Extensions;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using PEXC.Case.Services.IRIS.Contracts;

namespace PEXC.Case.Services.IRIS;

public class IrisDataImportService : IIrisDataImportService
{
    private static readonly UserInfo ServiceUserInfo = new(UserType.Service, nameof(IrisDataImportService));

    private readonly IDataImportStateRepository<IrisDataImportState> _irisDataImportStateRepository;
    private readonly ISingleCaseRepository _caseRepository;
    private readonly IIrisIntegrationService _irisIntegrationService;
    private readonly ILogger<IrisDataImportService> _logger;
    private readonly CaseDataImportOptions _options;

    public IrisDataImportService(
        IDataImportStateRepository<IrisDataImportState> irisDataImportStateRepository,
        ISingleCaseRepository caseRepository,
        IIrisIntegrationService irisIntegrationService,
        ILogger<IrisDataImportService> logger,
        IOptions<CaseDataImportOptions> options)
    {
        _irisDataImportStateRepository = irisDataImportStateRepository;
        _caseRepository = caseRepository;
        _irisIntegrationService = irisIntegrationService;
        _logger = logger;
        _options = options.Value;
    }

    public async Task UpdateCases()
    {
        var lastImportState = await _irisDataImportStateRepository.GetState();

        var newImportState = new IrisDataImportState
        {
            OperationId = Activity.Current?.RootId,
            LastExecutionTime = DateTime.UtcNow,
            LastModifiedAfter = GenerateNewModifiedAfter(lastImportState)
        };
        
        try
        {
            _logger.LogInformation(
                "Querying IRIS service for Cases modified after: {modifiedAfter:O}",
                newImportState.LastModifiedAfter);

            var irisCases = await GetCasesFromIris(newImportState);

            if (irisCases.Any())
            {
                await SaveCases(irisCases, newImportState.UpdatedCases);
            }
            else
            {
                _logger.LogInformation("No cases to process");
            }

            _logger.LogInformation(
                "Import finished for Cases modified after: {modifiedAfter:O}.\nUpdated {updatedCasesCount} Cases ({updatedCaseCodes})",
                newImportState.LastModifiedAfter,
                newImportState.UpdatedCases.Count,
                string.Join(';', newImportState.UpdatedCases));

            newImportState.LastSuccessfulExecutionTime = newImportState.LastExecutionTime;
            newImportState.LastSuccessfulModifiedAfter = newImportState.LastModifiedAfter;
        }
        catch (Exception ex)
        {
            _logger.LogErrorWithTelemetry(
                ex,
                "Import for Cases modified after: {modifiedAfter:O} failed with error: {error}.\nUpdated {updatedCasesCount} Cases ({updatedCaseCodes})",
                newImportState.LastModifiedAfter,
                ex.Message,
                newImportState.UpdatedCases.Count,
                string.Join(';', newImportState.UpdatedCases));

            newImportState.Failed = true;
            newImportState.FailedAttempts = (lastImportState?.FailedAttempts ?? 0) + 1;
            newImportState.ErrorMessage = ex.Message;
            newImportState.LastSuccessfulExecutionTime = lastImportState?.LastSuccessfulExecutionTime;
            newImportState.LastSuccessfulModifiedAfter = lastImportState?.LastSuccessfulModifiedAfter;

            throw;
        }
        finally
        {
            await _irisDataImportStateRepository.UpdateState(newImportState);
        }
    }

    private Task<IList<IrisCaseDto>> GetCasesFromIris(IrisDataImportState importState) =>
        _irisIntegrationService
            .GetCasesModifiedAfter(
                importState.LastModifiedAfter!.Value,
                _options.PegIndustries,
                _options.PegCapabilities);

    private async Task SaveCases(IEnumerable<IrisCaseDto> irisCases, ICollection<string> updatedCaseCodes)
    {
        foreach (var irisCase in irisCases)
        {
            var existingEntity = await _caseRepository.GetNonRetainerCaseByCaseCode(irisCase.CaseCode);

            if (existingEntity == null)
            {
                _logger.LogInformation("IRIS case with CaseCode {caseCode} not found in Cosmos DB.  Will be skipped.", irisCase.CaseCode);
                continue;
            }

            if (StringComparer.OrdinalIgnoreCase.Equals(existingEntity.LeadKnowledgeSpecialistEcode, irisCase.LeadKnowledgeSpecialist))
            {
                _logger.LogInformation("Lead Knowledge Specialist for a case with CaseCode {caseCode} has not be changed in IRIS. Will be skipped.", irisCase.CaseCode);
                continue;
            }

            _logger.LogInformation(
                "Found existing Case with CaseCode {caseCode}. Updating Lead Knowledge Specialist Ecode." +
                "Current Lead Knowledge Specialist Ecode: [{kSLeadEcode}]. " +
                "Lead Knowledge Specialist Ecode from IRIS: [{newKSLeadEcode}]",
                existingEntity.CaseCode,
                existingEntity.LeadKnowledgeSpecialistEcode,
                irisCase.LeadKnowledgeSpecialist);

            // Doing manual partial mapping and calling Update instead of Patch due to Cosmos DB API Patch limitations.
            // https://learn.microsoft.com/en-us/azure/cosmos-db/partial-document-update-faq#is-there-a-limit-to-the-number-of-partial-document-update-operations-
            existingEntity.LeadKnowledgeSpecialistEcode = irisCase.LeadKnowledgeSpecialist;
            existingEntity.ModifiedBy = ServiceUserInfo;
            existingEntity.Modified = DateTime.UtcNow;

            await _caseRepository.UpdateCase(existingEntity);
            updatedCaseCodes.Add(existingEntity.CaseCode);
        }
    }

    private DateOnly GenerateNewModifiedAfter(IrisDataImportState? lastImportState)
    {
        if (lastImportState == null)
            return _options.InitialModifiedAfterTime;

        return lastImportState.Failed
            ? lastImportState.LastModifiedAfter!.Value
            : DateOnly.FromDateTime(lastImportState.LastExecutionTime!.Value.AddDays(-1));
    }
}