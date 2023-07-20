using Microsoft.Data.SqlClient;

namespace PEXC.Case.Tools.Migration.Transformations;

public class ECodeLoader
{
    private readonly string _connectionString;

    private Dictionary<string, (string, bool IsTerminated)>? _eCodes;

    private HashSet<string>? _activeECodes;

    public ECodeLoader(string connectionString) => _connectionString = connectionString;

    public async Task<Dictionary<string, (string, bool IsTerminated)>> Load()
    {
        if (_eCodes != null)
            return _eCodes;

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = CreateCommand(conn, "SELECT [EmployeeCode], [FullName], [IsTerminated], [Email] FROM [dbo].[Employees]");
        await using var rdr = await cmd.ExecuteReaderAsync();

        _eCodes = new (StringComparer.OrdinalIgnoreCase);
        while (rdr.Read())
            _eCodes.Add(rdr.GetString(0), (rdr.GetString(1), rdr.GetBoolean(2) || rdr.IsDBNull(3)));

        return _eCodes;
    }
    public async Task<HashSet<string>> LoadActive()
    {
        if (_activeECodes != null)
            return _activeECodes;

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = CreateCommand(conn, "SELECT [EmployeeCode] FROM [dbo].[Employees] " +
            "WHERE IsTerminated = 0 and [Email] IS NOT NULL");
        await using var rdr = await cmd.ExecuteReaderAsync();

        _activeECodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (rdr.Read())
            _activeECodes.Add(rdr.GetString(0));

        return _activeECodes;
    }

    private static SqlCommand CreateCommand(SqlConnection conn, string text)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = text;
        cmd.Connection = conn;
        return cmd;
    }
}
