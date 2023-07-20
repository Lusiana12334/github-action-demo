using PEXC.Case.DataAccess.CosmosDB;
using PEXC.Case.Domain;
using PEXC.Case.Services.Infrastructure;

namespace PEXC.Case.Tools.AdvisorsDbProcessor;

public class DbFacade
{
    private readonly ICosmosDbRepository _dbRepository;

    public DbFacade(ICosmosDbRepository dbRepository)
    {
        _dbRepository = dbRepository;
    }

    public async Task<IReadOnlyList<CaseInfo>> LoadCaseCodes()
    {
        var result = await _dbRepository.Query<CaseEntity, CaseInfo>(
            c => new CaseInfo { Id = c.Id, Key = c.Key, CaseCode = c.CaseCode },
            c => c.Type == nameof(CaseEntity) && c.RelationshipType == RelationshipType.NonRetainer);

        return result.Items;
    }

    public async Task PersistRecords(CaseInfo[] records)
    {
        var advisorUpdaterUserInfo = new UserInfo(UserType.Service, "Batch Advisor Update Task");

        foreach (var caseInfo in records)
        {
            if (caseInfo.Advisors == null)
                continue;

            await _dbRepository.PatchDocument<CaseEntity>(caseInfo.Id, caseInfo.Key, new Dictionary<string, object?>
            {
                { nameof(CaseEntity.AdvisorsEcodes).ToCamelCase(), caseInfo.Advisors },
                { nameof(CaseEntity.Modified).ToCamelCase(), DateTime.UtcNow },
                { nameof(CaseEntity.ModifiedBy).ToCamelCase(),  advisorUpdaterUserInfo },
            });
        }
    }
}

public record CaseInfo
{
    public string Id { get; set; } = null!;
    public string Key { get; set; } = null!;
    public string CaseCode { get; set; } = null!;
    public List<string>? Advisors { get; set; }
}