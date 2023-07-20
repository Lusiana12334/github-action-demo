using AutoMapper;
using Microsoft.Extensions.Logging;
using PEXC.Case.Domain;
using PEXC.Common.BaseApi.Profile;
using PEXC.Common.BaseApi.Profile.DataContracts.V1;

namespace PEXC.Case.Services.Mapping.EmployeeProfile;

public abstract class EmployeeProfileMapping<TSource, TDestination> : IMappingAction<TSource, TDestination> where TSource : IEntity
{
    public const string SystemDisplayName = "System";

    private readonly IProfileRepository _profileRepository;
    private readonly ILogger<EmployeeProfileMapping<TSource, TDestination>> _logger;

    protected EmployeeProfileMapping(IProfileRepository profileRepository, ILogger<EmployeeProfileMapping<TSource, TDestination>> logger)
    {
        _profileRepository = profileRepository;
        _logger = logger;
    }

    public void Process(TSource source, TDestination destination, ResolutionContext context)
    {
        var ecodes = CollectDistinctEcodes(source);
        var profiles =
            _profileRepository
                .GetProfiles(ecodes, source.CorrelationId.ToString()).GetAwaiter().GetResult()
                .ToDictionary(p => p.EmployeeCode.ToLower());

        LogMissingEcodes(ecodes, profiles.Keys);
        MapEmployeeNames(profiles, source, destination);
    }

    protected abstract IEnumerable<string?> CollectEcodes(TSource source);

    protected abstract void MapEmployeeNames(
        IReadOnlyDictionary<string, EmployeeDetailsDto> profiles,
        TSource source,
        TDestination destination);

    protected static string GetDisplayName(IReadOnlyDictionary<string, EmployeeDetailsDto> profiles, UserInfo userInfo)
        => userInfo.UserType == UserType.Service
            ? SystemDisplayName
            : GetFullName(profiles, userInfo.UserEcode) ?? userInfo.DisplayName;

    protected static string? GetFullName(IReadOnlyDictionary<string, EmployeeDetailsDto> profiles, params string?[]? ecodes)
    {
        if (ecodes == null)
            return null;

        var names = ecodes.Select(ecode => ecode == null ? null : profiles.GetValueOrDefault(ecode.ToLower())?.FullName);
        var result = string.Join("; ", names);
        return string.IsNullOrEmpty(result) ? null : result;
    }

    private IReadOnlyList<string> CollectDistinctEcodes(TSource source)
        => CollectEcodes(source)
            .Where(e => e != null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()!;

    private void LogMissingEcodes(IEnumerable<string> ecodes, IEnumerable<string> existingEcodes)
    {
        var missingEcodes = ecodes.Except(existingEcodes).ToList();

        if (missingEcodes.Count > 0)
            _logger.LogWarning("The following ecodes: {ecodes} does not have profiles in ProfileApi",
                string.Join(";", missingEcodes));
    }
}