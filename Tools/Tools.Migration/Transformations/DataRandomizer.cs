using Faker;
using PEXC.Case.Domain;
using PEXC.Case.Services.Mapping;

#pragma warning disable CS0618
namespace PEXC.Case.Tools.Migration.Transformations;

public interface IRandomizer
{
    Task<IEnumerable<MigrationData>> RandomizeData(MigrationData data);
}

public class EmptyRandomier : IRandomizer
{
    public Task<IEnumerable<MigrationData>> RandomizeData(MigrationData data)
        => Task.FromResult(EnumerableEx.Return(data));
}

public class DataRandomizer : IRandomizer
{
    private static readonly string[] OpsDdTeam =
    {
        "Dedicated ops DD resources integrated with main team",
        "Full ops DD team, separate from main team",
        "No dedicated ops DD resources",
        "Separate ops DD team sublet",
    };
    
    private static readonly string[] OpsDdDuration =
    {
        "2-3 weeks",
        "A few days only",
        "About 1 week",
    };

    private readonly Random _random = new Random();

    private readonly ECodeLoader _eCodeLoader;

    private readonly ExistingDataLoader _dataLoader;

    private List<string>? _eCodes;

    public DataRandomizer(ECodeLoader eCodeLoader, ExistingDataLoader dataLoader)
    {
        _eCodeLoader = eCodeLoader;
        _dataLoader = dataLoader;
    }

    public async Task<IEnumerable<MigrationData>> RandomizeData(MigrationData data)
    {
        _eCodes ??= (await _eCodeLoader.LoadActive()).ToList();

        var entity = data.Entity;
        var leapRecord = data.LeapRecord;

        /*
         * We want to preserve case name as it is used to generate SP directory. 
         * If we generate case name during every run we would need to create new
         * SP directory every time we run randomizer. This would kill SP performance after
         * few runs of the tool. 
         */
        entity.CaseName = _dataLoader.TryGetRecord(leapRecord.ID!, out var dbData) ? dbData.CaseName : Company.CatchPhrase();

        entity.ClientName = Company.Name();
        entity.KmContactName = Name.FullName();
        entity.CreatedBy = new UserInfo(UserType.Service, MainProfile.MigrationUserDisplayName);
        entity.ModifiedBy = new UserInfo(UserType.Service, MainProfile.MigrationUserDisplayName);

        entity.ManagerEcode = RandomEcode(_eCodes);
        entity.BillingPartnerEcode = RandomEcode(_eCodes);
        entity.ClientHeadEcode = RandomEcode(_eCodes);
        entity.LeadKnowledgeSpecialistEcode = RandomEcode(_eCodes);
        entity.BainExpertsEcodes = RandomEcodes(_eCodes);
        entity.OperatingPartnerEcodes = RandomEcodes(_eCodes);

        entity.TargetName = Company.Name();
        entity.TargetDescription = Lorem.Sentence();
        entity.MainCompetitorsAnalyzed = string.IsNullOrEmpty(leapRecord.MainCompetitorsAnalyzedAsPartOfDd)
            ? ""
            : string.Join(", ", Enumerable.Repeat(0, _random.Next(1, 4)).Select(_ => Company.Name()));
        entity.Keyword = string.IsNullOrEmpty(leapRecord.Keyword) ? "" : Company.BS();
        entity.IndustrySectorsAnalyzed = string.IsNullOrEmpty(leapRecord.IndustrySectorsAnalyzedAsPartOfDd)
            ? ""
            : Company.CatchPhrase();
        entity.OpsDdDuration = OpsDdDuration.ElementAtOrDefault(_random.Next(0, 14));
        entity.OpsDdTeam = OpsDdTeam.ElementAtOrDefault(_random.Next(0, 14));

        entity.AdditionalComments = string.IsNullOrEmpty(leapRecord.AdditionalComments)
            ? ""
            : string.Join(". ", Lorem.Sentences(2));

        entity.OpsDdComments = string.IsNullOrEmpty(leapRecord.OpsDdComments)
            ? ""
            : Lorem.Sentence();

        return EnumerableEx.Return(data);
    }

    private List<string> RandomEcodes(List<string> eCodes) 
        => Enumerable.Repeat(0, _random.Next(1, 6)).Select(_ => RandomEcode(eCodes)).ToList();

    private string RandomEcode(List<string> ecodes) 
        => ecodes[_random.Next(ecodes.Count)];
}