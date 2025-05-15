using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Tutorial9.Model;

namespace Tutorial9.Services;

public class DbService : IDbService
{
    private readonly IConfiguration _configuration;
    public DbService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public async Task <int> AddProductToWarehouseAsync(WarehouseRequestDto request)
    {
        if (request.Amount <= 0)
            throw new Exception("Amount must be greater than 0");
        
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        await connection.OpenAsync();

        DbTransaction transaction = await connection.BeginTransactionAsync();
        command.Transaction = transaction as SqlTransaction;

        // BEGIN TRANSACTION
        try
        {
            command.CommandText = "SELECT 1 FROM Product WHERE IdProduct = @IdProduct";
            command.Parameters.AddWithValue("@IdProduct", request.IdProduct);

            var exists = await command.ExecuteScalarAsync();
            if (exists == null)
                throw new Exception("Product not found");

            command.Parameters.Clear();
            
            command.CommandText = "SELECT 1 FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
            command.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);

            exists = await command.ExecuteScalarAsync();
            if (exists == null)
                throw new Exception("Warehouse not found");

            command.Parameters.Clear();
            
            command.CommandText = "INSERT INTO Animal VALUES (@IdAnimal, @Name);";
            command.Parameters.AddWithValue("@IdAnimal", 1);
            command.Parameters.AddWithValue("@Name", "Animal1");
        
            await command.ExecuteNonQueryAsync();
        
            command.Parameters.Clear();
            command.CommandText = "INSERT INTO Animal VALUES (@IdAnimal, @Name);";
            command.Parameters.AddWithValue("@IdAnimal", 2);
            command.Parameters.AddWithValue("@Name", "Animal2");
        
            await command.ExecuteNonQueryAsync();
            
            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw;
        }
        // END TRANSACTION
    }

    public async Task ProcedureAsync()
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        await connection.OpenAsync();
        
        command.CommandText = "NazwaProcedury";
        command.CommandType = CommandType.StoredProcedure;
        
        command.Parameters.AddWithValue("@Id", 2);
        
        await command.ExecuteNonQueryAsync();
        
    }
}