using PEXC.Case.DataContracts.V1;
using PEXC.Case.Domain;

namespace PEXC.Case.Services.Mapping.FieldMasking;

public interface IFieldMaskingPolicy
{
    void Apply(CaseEntity source, CaseSearchItemDto destination);
}