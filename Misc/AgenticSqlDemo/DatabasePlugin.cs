using Microsoft.Data.Sqlite;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text;

internal class DatabasePlugin
{
    private readonly SqliteConnection _dbConnection;

    public DatabasePlugin(SqliteConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    [KernelFunction("get_database_schema")]
    [Description("Retrieves the database schema so you know what tables and columns exist.  Always call this FIRST before writing any SQL queries.")]
    public string GetSchema()
    {
        return @"
            Table: Products
            Columns: Id (INTEGER PRIMARY KEY), Name (TEXT), Price (REAL)

            Table: Orders
            Columns: Id (INTEGER PRIMARY KEY), ProductId (INTEGER), Quantity (INTEGER), OrderDate (TEXT), CustomerId (INTEGER)

            Table: Customers
            Columns: Id (INTEGER PRIMARY KEY), Name (TEXT)
        ";
    }

    [KernelFunction("execute_sql_query")]
    [Description("Executes a SQL SELECT query against the SQLite database and returns the results. ONLY execute SELECT queries.")]
    public string ExecuteSqlQuery(string query)
    {
        // Implementation for executing SQL query
        try
        {
            using var command = _dbConnection.CreateCommand();
            command.CommandText = query;
            using var reader = command.ExecuteReader();

            var sb = new StringBuilder();

            // Get Column Names
            for (int i = 0; i < reader.FieldCount; i++)
            {
                sb.AppendLine(reader.GetName(i) + "\t");
            }

            // Get Rows
            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    sb.AppendLine(reader.GetValue(i).ToString() + "\t");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Error executing query: {ex.Message}";
        }
    }
}
