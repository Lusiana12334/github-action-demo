using PEXC.Case.Domain;
using static PEXC.Case.Services.Workflow.CaseDocumentHelper;

namespace PEXC.Case.Services.Tests.Workflow;

public class CaseDocumentHelperTests
{
    [Fact]
    public void GenerateUniqueId_ReturnsSameIdForCase()
    {
        // Arrange
        var caseCode = "caseCode";
        var caseName = "caseName";

        // Act
        var uniqueId = GenerateUniqueId(caseCode, caseName, RelationshipType.Retainer);
        var anotherUniqueId = GenerateUniqueId(caseCode, caseName, RelationshipType.Retainer);

        // Assert
        uniqueId.Should().Be(anotherUniqueId);
    }

    [Theory]
    [InlineData("caseCode1", "caseName", RelationshipType.Retainer)]
    [InlineData("caseCode", "Name", RelationshipType.Retainer)]
    [InlineData("caseCode", "caseName", RelationshipType.NonRetainer)]
    [InlineData("caseCode1", "caseName1", RelationshipType.NonRetainer)]
    public void GenerateUniqueId_ReturnsUniqueIdForOneCase(string caseCode, string caseName,
        RelationshipType relationship)
    {
        // Arrange
        var uniqueId = GenerateUniqueId("caseCode", "caseName", RelationshipType.Retainer);

        // Act
        var anotherUniqueId = GenerateUniqueId(caseCode, caseName, relationship);

        // Assert
        uniqueId.Should().NotBe(anotherUniqueId);
    }
}