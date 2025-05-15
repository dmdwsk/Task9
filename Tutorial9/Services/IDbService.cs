namespace Tutorial9.Services;
using Tutorial9.Model;
public interface IDbService
{
    Task<int> AddProductToWarehouseAsync(WarehouseRequestDto request);
    Task <int> AddProductToWarehouseViaProcedureAsync(WarehouseRequestDto request);
}