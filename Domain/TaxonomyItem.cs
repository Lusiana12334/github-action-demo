namespace PEXC.Case.Domain;

public record TaxonomyItem(int? Id, string? Name)
{
    public static TaxonomyItem Empty = new TaxonomyItem(null, null);
}
