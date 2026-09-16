using Microsoft.Data.SqlClient;

namespace SynteraERP.Api.Helpers;

public static class SandboxConnectionStringHelper
{
    public static string ForDatabase(string baseConnectionString, string databaseName)
    {
        var builder = new SqlConnectionStringBuilder(baseConnectionString)
        {
            InitialCatalog = databaseName,
        };
        return builder.ConnectionString;
    }
}
