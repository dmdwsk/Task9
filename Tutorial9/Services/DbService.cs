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
            
            command.CommandText = @"
                SELECT TOP 1 IdOrder FROM [Order]
                WHERE IdProduct = @IdProduct AND Amount = @Amount AND CreatedAt < @CreatedAt";

            command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
            command.Parameters.AddWithValue("@Amount", request.Amount);
            command.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);

            var idOrderObj = await command.ExecuteScalarAsync();
            if (idOrderObj == null)
                throw new Exception("Matching order not found");

            int idOrder = Convert.ToInt32(idOrderObj);
            command.Parameters.Clear();
            command.CommandText = "SELECT 1 FROM Product_Warehouse WHERE IdOrder = @IdOrder";
            command.Parameters.AddWithValue("@IdOrder", idOrder);

            exists = await command.ExecuteScalarAsync();
            if (exists != null)
                throw new Exception("Order already fulfilled");

            command.Parameters.Clear();
            
            command.CommandText = "UPDATE [Order] SET FullfilledAt = GETDATE() WHERE IdOrder = @IdOrder";
            command.Parameters.AddWithValue("@IdOrder", idOrder);

            await command.ExecuteNonQueryAsync();
            command.Parameters.Clear();
            
            command.CommandText = "SELECT Price FROM Product WHERE IdProduct = @IdProduct";
            command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
            decimal unitPrice = Convert.ToDecimal(await command.ExecuteScalarAsync());
            decimal totalPrice = unitPrice * request.Amount;
            command.Parameters.Clear();
            command.CommandText = @"
                INSERT INTO Product_Warehouse
                (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
                VALUES
                (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, GETDATE());

                SELECT SCOPE_IDENTITY();";

            command.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
            command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
            command.Parameters.AddWithValue("@IdOrder", idOrder);
            command.Parameters.AddWithValue("@Amount", request.Amount);
            command.Parameters.AddWithValue("@Price", totalPrice);

            object result = await command.ExecuteScalarAsync();
            int newId = Convert.ToInt32(result);
            
            await transaction.CommitAsync();
            return newId;
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw;
        }
        // END TRANSACTION
        
    }

    public async Task<int> AddProductToWarehouseViaProcedureAsync(WarehouseRequestDto request)
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await connection.OpenAsync();

        await using SqlCommand command = new SqlCommand("AddProductToWarehouse", connection);
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
        command.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
        command.Parameters.AddWithValue("@Amount", request.Amount);
        command.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);

        var result = await command.ExecuteScalarAsync();

        if (result == null)
            throw new Exception("Procedure did not return any value");

        return Convert.ToInt32(result);
    }
}