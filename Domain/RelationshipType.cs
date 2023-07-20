using System.Runtime.Serialization;

namespace PEXC.Case.Domain;

public enum RelationshipType
{
    [EnumMember(Value = "Non-Retainer")]
    NonRetainer = 0,
    [EnumMember(Value = "Retainer")]
    Retainer
}