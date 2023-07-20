using AutoMapper;
using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;
using PEXC.Case.Services.Mapping.FieldMasking;

namespace PEXC.Case.Services.Mapping;

public class CaseSearchItemConfidentialDataMapping : IMappingAction<CaseEntity, CaseSearchItemDto>
{
    private readonly IFieldMaskingPolicy _caseVisibilityPolicy;

    public CaseSearchItemConfidentialDataMapping(IFieldMaskingPolicy caseVisibilityPolicy)
    {
        _caseVisibilityPolicy = caseVisibilityPolicy;
    }

    public void Process(CaseEntity source, CaseSearchItemDto destination, ResolutionContext context) 
        => _caseVisibilityPolicy.Apply(source, destination);
}