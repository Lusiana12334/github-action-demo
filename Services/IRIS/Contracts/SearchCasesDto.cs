namespace PEXC.Case.Services.IRIS.Contracts;

public record SearchCasesDto(
    DateOnly? ModifiedSince,
    IReadOnlyList<string>? CaseCodes,
    IReadOnlyList<int>? PrimaryIndustries,
    IReadOnlyList<int>? PrimaryCapabilities);