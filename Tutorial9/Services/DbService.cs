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
            
            await transaction.CommitAsync();
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
        throw new NotImplementedException();
    }
}