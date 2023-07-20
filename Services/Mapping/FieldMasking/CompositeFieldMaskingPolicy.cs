using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;

namespace PEXC.Case.Services.Mapping.FieldMasking;

public class CompositeFieldMaskingPolicy : IFieldMaskingPolicy
{
    private readonly List<IFieldMaskingPolicy> _innerPolicies;

    public CompositeFieldMaskingPolicy(IEnumerable<IFieldMaskingPolicy>? innerPolicies)
        => _innerPolicies = innerPolicies?.ToList() ?? throw new ArgumentNullException(nameof(innerPolicies));

    public void Apply(CaseEntity source, CaseSearchItemDto destination)
    {
        foreach (var policy in _innerPolicies)
            policy.Apply(source, destination);
    }
}