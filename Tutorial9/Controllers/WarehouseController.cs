namespace Tutorial9.Controllers;

using Microsoft.AspNetCore.Mvc;
using Tutorial9.Model;
using Tutorial9.Services;

[ApiController]
[Route("api/[controller]")]
public class WarehouseController: ControllerBase
{
    private readonly IDbService _service;
    
    public WarehouseController(IDbService service)
    {
        _service = service;
    }
    [HttpPost("proc")]
    public async Task<IActionResult> AddProductViaProcedure([FromBody] WarehouseRequestDto request)
    {
        try
        {
            int newId = await _service.AddProductToWarehouseViaProcedureAsync(request);
            return Ok(new { Id = newId });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}