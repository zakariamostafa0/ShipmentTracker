using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipmentTracker.API.DTOs.Batch;
using ShipmentTracker.API.DTOs.Common;
using ShipmentTracker.Core.Entities;
using ShipmentTracker.Core.Enums;
using ShipmentTracker.Core.Interfaces;
using System.Security.Claims;

namespace ShipmentTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BatchController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<BatchController> _logger;

    public BatchController(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<BatchController> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "DataEntry,BranchAdmin,Admin")]
    public async Task<ActionResult<ApiResponse<List<BatchResponse>>>> GetBatches(
        [FromQuery] long? branchId = null,
        [FromQuery] BatchStatus? status = null)
    {
        try
        {
            var batches = await _unitOfWork.Batches.GetAllAsync();
            
            if (branchId.HasValue)
            {
                batches = batches.Where(b => b.BranchId == branchId.Value);
            }
            
            if (status.HasValue)
            {
                batches = batches.Where(b => b.Status == status.Value);
            }

            var batchResponses = _mapper.Map<List<BatchResponse>>(batches);
            return Ok(ApiResponse<List<BatchResponse>>.SuccessResult(batchResponses));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving batches");
            return StatusCode(500, ApiResponse<List<BatchResponse>>.ErrorResult("An error occurred while retrieving batches"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "DataEntry,BranchAdmin,Admin")]
    public async Task<ActionResult<ApiResponse<BatchResponse>>> CreateBatch([FromBody] CreateBatchRequest request)
    {
        try
        {
            // Validate branch exists
            var branch = await _unitOfWork.Branches.GetByIdAsync(request.BranchId);
            if (branch == null)
            {
                return BadRequest(ApiResponse<BatchResponse>.ErrorResult("Branch not found"));
            }

            var batch = _mapper.Map<Batch>(request);
            await _unitOfWork.Batches.AddAsync(batch);
            await _unitOfWork.SaveChangesAsync();

            var batchResponse = _mapper.Map<BatchResponse>(batch);
            return CreatedAtAction(nameof(GetBatch), new { id = batch.Id }, 
                ApiResponse<BatchResponse>.SuccessResult(batchResponse, "Batch created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating batch");
            return StatusCode(500, ApiResponse<BatchResponse>.ErrorResult("An error occurred while creating batch"));
        }
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "DataEntry,BranchAdmin,Admin")]
    public async Task<ActionResult<ApiResponse<BatchDetailResponse>>> GetBatch(long id)
    {
        try
        {
            var batch = await _unitOfWork.Batches.GetWithShipmentsAsync(id);
            if (batch == null)
            {
                return NotFound(ApiResponse<BatchDetailResponse>.ErrorResult("Batch not found"));
            }

            var batchResponse = _mapper.Map<BatchDetailResponse>(batch);
            return Ok(ApiResponse<BatchDetailResponse>.SuccessResult(batchResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving batch {BatchId}", id);
            return StatusCode(500, ApiResponse<BatchDetailResponse>.ErrorResult("An error occurred while retrieving batch"));
        }
    }

    [HttpPost("{id}/close")]
    [Authorize(Roles = "DataEntry,BranchAdmin,Admin")]
    public async Task<ActionResult<ApiResponse>> CloseBatch(long id, [FromBody] UpdateBatchStatusRequest request)
    {
        try
        {
            var batch = await _unitOfWork.Batches.GetByIdAsync(id);
            if (batch == null)
            {
                return NotFound(ApiResponse.ErrorResult("Batch not found"));
            }

            if (batch.Status != BatchStatus.Open)
            {
                return BadRequest(ApiResponse.ErrorResult("Only open batches can be closed"));
            }

            if (batch.ShipmentCount == 0)
            {
                return BadRequest(ApiResponse.ErrorResult("Cannot close an empty batch"));
            }

            batch.Status = BatchStatus.Closed;
            await _unitOfWork.Batches.UpdateAsync(batch);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Batch {BatchId} closed successfully", id);
            return Ok(ApiResponse.SuccessResult("Batch closed successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing batch {BatchId}", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while closing batch"));
        }
    }

    [HttpPost("{id}/shipments/{shipmentId}")]
    [Authorize(Roles = "DataEntry,BranchAdmin,Admin")]
    public async Task<ActionResult<ApiResponse>> AddShipmentToBatch(long id, long shipmentId)
    {
        try
        {
            var batch = await _unitOfWork.Batches.GetByIdAsync(id);
            if (batch == null)
            {
                return NotFound(ApiResponse.ErrorResult("Batch not found"));
            }

            var shipment = await _unitOfWork.Shipments.GetByIdAsync(shipmentId);
            if (shipment == null)
            {
                return NotFound(ApiResponse.ErrorResult("Shipment not found"));
            }

            if (batch.Status != BatchStatus.Open)
            {
                return BadRequest(ApiResponse.ErrorResult("Can only add shipments to open batches"));
            }

            if (shipment.BatchId.HasValue)
            {
                return BadRequest(ApiResponse.ErrorResult("Shipment is already assigned to a batch"));
            }

            shipment.BatchId = id;
            shipment.Status = ShipmentStatus.InBatch;
            batch.ShipmentCount++;
            batch.TotalWeight += shipment.Weight;

            await _unitOfWork.Shipments.UpdateAsync(shipment);
            await _unitOfWork.Batches.UpdateAsync(batch);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Shipment {ShipmentId} added to batch {BatchId}", shipmentId, id);
            return Ok(ApiResponse.SuccessResult("Shipment added to batch successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding shipment {ShipmentId} to batch {BatchId}", shipmentId, id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while adding shipment to batch"));
        }
    }

    [HttpPost("{id}/move-to-warehouse")]
    [Authorize(Roles = "WarehouseOperator,Admin")]
    public async Task<ActionResult<ApiResponse>> MoveToWarehouse(long id, [FromBody] MoveToWarehouseRequest request)
    {
        try
        {
            var batch = await _unitOfWork.Batches.GetByIdAsync(id);
            if (batch == null)
            {
                return NotFound(ApiResponse.ErrorResult("Batch not found"));
            }

            if (batch.Status != BatchStatus.Closed)
            {
                return BadRequest(ApiResponse.ErrorResult("Only closed batches can be moved to warehouse"));
            }

            // Validate warehouse exists
            var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(request.SourceWarehouseId);
            if (warehouse == null)
            {
                return BadRequest(ApiResponse.ErrorResult("Source warehouse not found"));
            }

            batch.Status = BatchStatus.InWarehouse;
            batch.SourceWarehouseId = request.SourceWarehouseId;
            await _unitOfWork.Batches.UpdateAsync(batch);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Batch {BatchId} moved to source warehouse {WarehouseId}", id, request.SourceWarehouseId);
            return Ok(ApiResponse.SuccessResult("Batch moved to warehouse successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving batch {BatchId} to warehouse", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while moving batch to warehouse"));
        }
    }

    [HttpPost("{id}/assign-destination-warehouse")]
    [Authorize(Roles = "WarehouseOperator,Admin")]
    public async Task<ActionResult<ApiResponse>> AssignDestinationWarehouse(long id, [FromBody] AssignDestinationWarehouseRequest request)
    {
        try
        {
            var batch = await _unitOfWork.Batches.GetByIdAsync(id);
            if (batch == null)
            {
                return NotFound(ApiResponse.ErrorResult("Batch not found"));
            }

            if (batch.Status != BatchStatus.InWarehouse && batch.Status != BatchStatus.AtSourcePort)
            {
                return BadRequest(ApiResponse.ErrorResult("Destination warehouse can only be assigned to batches in warehouse or at source port"));
            }

            // Validate warehouse exists
            var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(request.DestinationWarehouseId);
            if (warehouse == null)
            {
                return BadRequest(ApiResponse.ErrorResult("Destination warehouse not found"));
            }

            batch.DestinationWarehouseId = request.DestinationWarehouseId;
            await _unitOfWork.Batches.UpdateAsync(batch);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Batch {BatchId} assigned destination warehouse {WarehouseId}", id, request.DestinationWarehouseId);
            return Ok(ApiResponse.SuccessResult("Destination warehouse assigned successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning destination warehouse to batch {BatchId}", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while assigning destination warehouse"));
        }
    }

    [HttpPost("{id}/move-to-source-port")]
    [Authorize(Roles = "PortOperator,Admin")]
    public async Task<ActionResult<ApiResponse>> MoveToSourcePort(long id, [FromBody] MoveToPortRequest request)
    {
        try
        {
            var batch = await _unitOfWork.Batches.GetByIdAsync(id);
            if (batch == null)
            {
                return NotFound(ApiResponse.ErrorResult("Batch not found"));
            }

            if (batch.Status != BatchStatus.InWarehouse)
            {
                return BadRequest(ApiResponse.ErrorResult("Only batches in warehouse can be moved to source port"));
            }

            // Validate source port exists
            var sourcePort = await _unitOfWork.Ports.GetByIdAsync(request.SourcePortId);
            if (sourcePort == null)
            {
                return BadRequest(ApiResponse.ErrorResult("Source port not found"));
            }

            // Validate destination port if provided
            if (request.DestinationPortId.HasValue)
            {
                var destinationPort = await _unitOfWork.Ports.GetByIdAsync(request.DestinationPortId.Value);
                if (destinationPort == null)
                {
                    return BadRequest(ApiResponse.ErrorResult("Destination port not found"));
                }
                batch.DestinationPortId = request.DestinationPortId.Value;
            }

            batch.Status = BatchStatus.AtSourcePort;
            batch.SourcePortId = request.SourcePortId;
            await _unitOfWork.Batches.UpdateAsync(batch);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Batch {BatchId} moved to source port {SourcePortId}", id, request.SourcePortId);
            return Ok(ApiResponse.SuccessResult("Batch moved to source port successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving batch {BatchId} to source port", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while moving batch to source port"));
        }
    }

    [HttpPost("{id}/clear-source-port")]
    [Authorize(Roles = "PortOperator,Admin")]
    public async Task<ActionResult<ApiResponse>> ClearSourcePort(long id, [FromBody] UpdateBatchStatusRequest request)
    {
        try
        {
            var batch = await _unitOfWork.Batches.GetByIdAsync(id);
            if (batch == null)
            {
                return NotFound(ApiResponse.ErrorResult("Batch not found"));
            }

            if (batch.Status != BatchStatus.AtSourcePort)
            {
                return BadRequest(ApiResponse.ErrorResult("Only batches at source port can be cleared"));
            }

            batch.Status = BatchStatus.ClearedSourcePort;
            await _unitOfWork.Batches.UpdateAsync(batch);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Batch {BatchId} cleared from source port", id);
            return Ok(ApiResponse.SuccessResult("Batch cleared from source port successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing batch {BatchId} from source port", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while clearing batch from source port"));
        }
    }

    [HttpPost("{id}/start-transit")]
    [Authorize(Roles = "PortOperator,Admin")]
    public async Task<ActionResult<ApiResponse>> StartTransit(long id, [FromBody] UpdateBatchStatusRequest request)
    {
        try
        {
            var batch = await _unitOfWork.Batches.GetByIdAsync(id);
            if (batch == null)
            {
                return NotFound(ApiResponse.ErrorResult("Batch not found"));
            }

            if (batch.Status != BatchStatus.ClearedSourcePort)
            {
                return BadRequest(ApiResponse.ErrorResult("Only cleared batches can start transit"));
            }

            batch.Status = BatchStatus.InTransit;
            await _unitOfWork.Batches.UpdateAsync(batch);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Batch {BatchId} started transit", id);
            return Ok(ApiResponse.SuccessResult("Batch started transit successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting transit for batch {BatchId}", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while starting transit"));
        }
    }

    [HttpPost("{id}/arrival")]
    [Authorize(Roles = "PortOperator,Admin")]
    public async Task<ActionResult<ApiResponse>> MarkArrival(long id, [FromBody] UpdateBatchStatusRequest request)
    {
        try
        {
            var batch = await _unitOfWork.Batches.GetByIdAsync(id);
            if (batch == null)
            {
                return NotFound(ApiResponse.ErrorResult("Batch not found"));
            }

            if (batch.Status != BatchStatus.InTransit)
            {
                return BadRequest(ApiResponse.ErrorResult("Only batches in transit can be marked as arrived"));
            }

            batch.Status = BatchStatus.ArrivedDestinationPort;
            await _unitOfWork.Batches.UpdateAsync(batch);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Batch {BatchId} marked as arrived at destination", id);
            return Ok(ApiResponse.SuccessResult("Batch marked as arrived successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking arrival for batch {BatchId}", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while marking arrival"));
        }
    }

    [HttpPost("{id}/assign-carriers")]
    [Authorize(Roles = "BranchAdmin,Admin")]
    public async Task<ActionResult<ApiResponse>> AssignCarriers(long id, [FromBody] AssignCarriersRequest request)
    {
        try
        {
            var batch = await _unitOfWork.Batches.GetByIdAsync(id);
            if (batch == null)
            {
                return NotFound(ApiResponse.ErrorResult("Batch not found"));
            }

            if (batch.Status != BatchStatus.InDestinationWarehouse)
            {
                return BadRequest(ApiResponse.ErrorResult("Carriers can only be assigned to batches that are in destination warehouse"));
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                foreach (var assignment in request.Assignments)
                {
                    var shipment = await _unitOfWork.Shipments.GetByIdAsync(assignment.ShipmentId);
                    if (shipment == null || shipment.BatchId != id)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return BadRequest(ApiResponse.ErrorResult($"Shipment {assignment.ShipmentId} not found in this batch"));
                    }

                    var carrier = await _unitOfWork.Carriers.GetByIdAsync(assignment.CarrierId);
                    if (carrier == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return BadRequest(ApiResponse.ErrorResult($"Carrier {assignment.CarrierId} not found"));
                    }

                    shipment.CarrierId = assignment.CarrierId;
                    shipment.Status = ShipmentStatus.WithCarrier;
                    await _unitOfWork.Shipments.UpdateAsync(shipment);
                }

                batch.Status = BatchStatus.AssignedToCarriers;
                batch.CarrierAssignedAt = DateTime.UtcNow;
                await _unitOfWork.Batches.UpdateAsync(batch);
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Carriers assigned to batch {BatchId}", id);
                return Ok(ApiResponse.SuccessResult("Carriers assigned successfully"));
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning carriers to batch {BatchId}", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while assigning carriers"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "BranchAdmin,Admin")]
    public async Task<ActionResult<ApiResponse>> CancelBatch(long id)
    {
        try
        {
            var batch = await _unitOfWork.Batches.GetByIdAsync(id);
            if (batch == null)
            {
                return NotFound(ApiResponse.ErrorResult("Batch not found"));
            }

            if (batch.Status == BatchStatus.Delivered || batch.Status == BatchStatus.Cancelled)
            {
                return BadRequest(ApiResponse.ErrorResult("Cannot cancel delivered or already cancelled batches"));
            }

            batch.Status = BatchStatus.Cancelled;
            await _unitOfWork.Batches.UpdateAsync(batch);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Batch {BatchId} cancelled", id);
            return Ok(ApiResponse.SuccessResult("Batch cancelled successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling batch {BatchId}", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while cancelling batch"));
        }
    }

    [HttpPost("{id}/move-to-destination-warehouse")]
    [Authorize(Roles = "WarehouseOperator,Admin")]
    public async Task<ActionResult<ApiResponse>> MoveToDestinationWarehouse(long id, [FromBody] MoveToWarehouseRequest request)
    {
        try
        {
            var batch = await _unitOfWork.Batches.GetByIdAsync(id);
            if (batch == null)
            {
                return NotFound(ApiResponse.ErrorResult("Batch not found"));
            }

            if (batch.Status != BatchStatus.ArrivedDestinationPort)
            {
                return BadRequest(ApiResponse.ErrorResult("Only batches that have arrived at destination port can be moved to destination warehouse"));
            }

            // Validate destination warehouse exists
            var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(request.SourceWarehouseId);
            if (warehouse == null)
            {
                return BadRequest(ApiResponse.ErrorResult("Destination warehouse not found"));
            }

            batch.Status = BatchStatus.InDestinationWarehouse;
            batch.DestinationWarehouseId = request.SourceWarehouseId;
            await _unitOfWork.Batches.UpdateAsync(batch);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Batch {BatchId} moved to destination warehouse {WarehouseId}", id, request.SourceWarehouseId);
            return Ok(ApiResponse.SuccessResult("Batch moved to destination warehouse successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving batch {BatchId} to destination warehouse", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while moving batch to destination warehouse"));
        }
    }
}
