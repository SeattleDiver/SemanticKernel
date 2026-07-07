using Microsoft.Data.Sqlite;

public static class DatabaseSetup
{
    public static SqliteConnection InitializeMockDatabase()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE Products (Id INTEGER PRIMARY KEY, Name TEXT, Price REAL);
            CREATE TABLE Customers (Id INTEGER PRIMARY KEY, Name TEXT);
            CREATE TABLE Orders (Id INTEGER PRIMARY KEY, ProductId INTEGER, Quantity INTEGER, OrderDate TEXT, CustomerId INTEGER);

            INSERT INTO Customers (Name) VALUES ('Smith, Joseph'), ('Johnson, Timothy'), ('Triumph, Jesse');            
            INSERT INTO Products (Name, Price) VALUES ('Laptop', 1200.00), ('Mouse', 25.00), ('Keyboard', 75.00), ('Desktop', 575.00);
            INSERT INTO Orders (ProductId, Quantity, OrderDate, CustomerId) VALUES (1, 5, '2026-05-01', 1), (2, 50, '2026-05-02', 2), (3, 20, '2026-05-03', 3);
        ";
        command.ExecuteNonQuery();

        return connection;
    }
}