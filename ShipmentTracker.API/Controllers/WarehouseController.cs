using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipmentTracker.API.DTOs.Batch;
using ShipmentTracker.API.DTOs.Common;
using ShipmentTracker.API.DTOs.Warehouse;
using ShipmentTracker.Core.Entities;
using ShipmentTracker.Core.Interfaces;

namespace ShipmentTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WarehouseController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<WarehouseController> _logger;

    public WarehouseController(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<WarehouseController> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "WarehouseOperator,Admin")]
    public async Task<ActionResult<ApiResponse<List<WarehouseResponse>>>> GetWarehouses()
    {
        try
        {
            var warehouses = await _unitOfWork.Warehouses.GetAllAsync();
            var warehouseResponses = _mapper.Map<List<WarehouseResponse>>(warehouses);
            return Ok(ApiResponse<List<WarehouseResponse>>.SuccessResult(warehouseResponses));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving warehouses");
            return StatusCode(500, ApiResponse<List<WarehouseResponse>>.ErrorResult("An error occurred while retrieving warehouses"));
        }
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "WarehouseOperator,Admin")]
    public async Task<ActionResult<ApiResponse<WarehouseResponse>>> GetWarehouse(long id)
    {
        try
        {
            var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(id);
            if (warehouse == null)
            {
                return NotFound(ApiResponse<WarehouseResponse>.ErrorResult("Warehouse not found"));
            }

            var warehouseResponse = _mapper.Map<WarehouseResponse>(warehouse);
            return Ok(ApiResponse<WarehouseResponse>.SuccessResult(warehouseResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving warehouse {WarehouseId}", id);
            return StatusCode(500, ApiResponse<WarehouseResponse>.ErrorResult("An error occurred while retrieving warehouse"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<WarehouseResponse>>> CreateWarehouse([FromBody] CreateWarehouseRequest request)
    {
        try
        {
            // Check if warehouse name already exists
            var existingWarehouse = await _unitOfWork.Warehouses.FirstOrDefaultAsync(w => w.Name == request.Name);
            if (existingWarehouse != null)
            {
                return BadRequest(ApiResponse<WarehouseResponse>.ErrorResult("Warehouse name already exists"));
            }

            var warehouse = _mapper.Map<Warehouse>(request);
            await _unitOfWork.Warehouses.AddAsync(warehouse);
            await _unitOfWork.SaveChangesAsync();

            var warehouseResponse = _mapper.Map<WarehouseResponse>(warehouse);
            return CreatedAtAction(nameof(GetWarehouse), new { id = warehouse.Id }, 
                ApiResponse<WarehouseResponse>.SuccessResult(warehouseResponse, "Warehouse created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating warehouse");
            return StatusCode(500, ApiResponse<WarehouseResponse>.ErrorResult("An error occurred while creating warehouse"));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<WarehouseResponse>>> UpdateWarehouse(long id, [FromBody] UpdateWarehouseRequest request)
    {
        try
        {
            var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(id);
            if (warehouse == null)
            {
                return NotFound(ApiResponse<WarehouseResponse>.ErrorResult("Warehouse not found"));
            }

            // Check if warehouse name already exists (excluding current warehouse)
            var existingWarehouse = await _unitOfWork.Warehouses.FirstOrDefaultAsync(w => w.Name == request.Name && w.Id != id);
            if (existingWarehouse != null)
            {
                return BadRequest(ApiResponse<WarehouseResponse>.ErrorResult("Warehouse name already exists"));
            }

            _mapper.Map(request, warehouse);
            await _unitOfWork.Warehouses.UpdateAsync(warehouse);
            await _unitOfWork.SaveChangesAsync();

            var warehouseResponse = _mapper.Map<WarehouseResponse>(warehouse);
            return Ok(ApiResponse<WarehouseResponse>.SuccessResult(warehouseResponse, "Warehouse updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating warehouse {WarehouseId}", id);
            return StatusCode(500, ApiResponse<WarehouseResponse>.ErrorResult("An error occurred while updating warehouse"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> DeleteWarehouse(long id)
    {
        try
        {
            var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(id);
            if (warehouse == null)
            {
                return NotFound(ApiResponse.ErrorResult("Warehouse not found"));
            }

            // Check if warehouse has active batches
            var activeBatches = warehouse.Batches.Where(b => b.Status != Core.Enums.BatchStatus.Delivered && 
                                                             b.Status != Core.Enums.BatchStatus.Cancelled && 
                                                             b.Status != Core.Enums.BatchStatus.Archived);
            
            if (activeBatches.Any())
            {
                return BadRequest(ApiResponse.ErrorResult("Cannot delete warehouse with active batches"));
            }

            await _unitOfWork.Warehouses.DeleteAsync(warehouse);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Warehouse {WarehouseId} deleted", id);
            return Ok(ApiResponse.SuccessResult("Warehouse deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting warehouse {WarehouseId}", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while deleting warehouse"));
        }
    }

    [HttpGet("{id}/batches")]
    [Authorize(Roles = "WarehouseOperator,Admin")]
    public async Task<ActionResult<ApiResponse<List<BatchResponse>>>> GetWarehouseBatches(long id)
    {
        try
        {
            var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(id);
            if (warehouse == null)
            {
                return NotFound(ApiResponse<List<BatchResponse>>.ErrorResult("Warehouse not found"));
            }

            var batches = warehouse.Batches.ToList();
            var batchResponses = _mapper.Map<List<BatchResponse>>(batches);
            return Ok(ApiResponse<List<BatchResponse>>.SuccessResult(batchResponses));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving batches for warehouse {WarehouseId}", id);
            return StatusCode(500, ApiResponse<List<BatchResponse>>.ErrorResult("An error occurred while retrieving warehouse batches"));
        }
    }
}
