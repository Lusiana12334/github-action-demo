using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using PEXC.Case.Domain;

namespace PEXC.Case.Services.Workflow;

public static class CaseDocumentHelper
{
    public static string CreateDirectoryName(string caseCode, string caseName,string uniqueId)
    {
        var toUpper = caseName.ToUpper();
        var directoryName = $"{caseCode}_{toUpper[..Math.Min(toUpper.Length, 10)]}_{uniqueId}";
        return Regex.Replace(directoryName, "\\W", "_");
    }

    public static string GenerateUniqueId(string caseCode, string caseName, RelationshipType relationshipType)
    {
        var textToHash = $"{caseCode}{caseName}{relationshipType}";
        var bytes = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(textToHash.ToUpper())).Take(8).ToArray();
        var hash = BitConverter.ToString(bytes).Replace("-", string.Empty);
        return hash;
    }
}