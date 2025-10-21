using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipmentTracker.API.DTOs.Carrier;
using ShipmentTracker.API.DTOs.Common;
using ShipmentTracker.API.DTOs.Shipment;
using ShipmentTracker.Core.Entities;
using ShipmentTracker.Core.Interfaces;

namespace ShipmentTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CarrierController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CarrierController> _logger;

    public CarrierController(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CarrierController> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "BranchAdmin,CarrierOperator,Admin")]
    public async Task<ActionResult<ApiResponse<List<CarrierResponse>>>> GetCarriers()
    {
        try
        {
            var carriers = await _unitOfWork.Carriers.GetAllAsync();
            var carrierResponses = _mapper.Map<List<CarrierResponse>>(carriers);
            return Ok(ApiResponse<List<CarrierResponse>>.SuccessResult(carrierResponses));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving carriers");
            return StatusCode(500, ApiResponse<List<CarrierResponse>>.ErrorResult("An error occurred while retrieving carriers"));
        }
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "BranchAdmin,CarrierOperator,Admin")]
    public async Task<ActionResult<ApiResponse<CarrierResponse>>> GetCarrier(long id)
    {
        try
        {
            var carrier = await _unitOfWork.Carriers.GetByIdAsync(id);
            if (carrier == null)
            {
                return NotFound(ApiResponse<CarrierResponse>.ErrorResult("Carrier not found"));
            }

            var carrierResponse = _mapper.Map<CarrierResponse>(carrier);
            return Ok(ApiResponse<CarrierResponse>.SuccessResult(carrierResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving carrier {CarrierId}", id);
            return StatusCode(500, ApiResponse<CarrierResponse>.ErrorResult("An error occurred while retrieving carrier"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<CarrierResponse>>> CreateCarrier([FromBody] CreateCarrierRequest request)
    {
        try
        {
            // Check if carrier name already exists
            var existingCarrier = await _unitOfWork.Carriers.FirstOrDefaultAsync(c => c.Name == request.Name);
            if (existingCarrier != null)
            {
                return BadRequest(ApiResponse<CarrierResponse>.ErrorResult("Carrier name already exists"));
            }

            var carrier = _mapper.Map<Carrier>(request);
            await _unitOfWork.Carriers.AddAsync(carrier);
            await _unitOfWork.SaveChangesAsync();

            var carrierResponse = _mapper.Map<CarrierResponse>(carrier);
            return CreatedAtAction(nameof(GetCarrier), new { id = carrier.Id }, 
                ApiResponse<CarrierResponse>.SuccessResult(carrierResponse, "Carrier created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating carrier");
            return StatusCode(500, ApiResponse<CarrierResponse>.ErrorResult("An error occurred while creating carrier"));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<CarrierResponse>>> UpdateCarrier(long id, [FromBody] UpdateCarrierRequest request)
    {
        try
        {
            var carrier = await _unitOfWork.Carriers.GetByIdAsync(id);
            if (carrier == null)
            {
                return NotFound(ApiResponse<CarrierResponse>.ErrorResult("Carrier not found"));
            }

            // Check if carrier name already exists (excluding current carrier)
            var existingCarrier = await _unitOfWork.Carriers.FirstOrDefaultAsync(c => c.Name == request.Name && c.Id != id);
            if (existingCarrier != null)
            {
                return BadRequest(ApiResponse<CarrierResponse>.ErrorResult("Carrier name already exists"));
            }

            _mapper.Map(request, carrier);
            await _unitOfWork.Carriers.UpdateAsync(carrier);
            await _unitOfWork.SaveChangesAsync();

            var carrierResponse = _mapper.Map<CarrierResponse>(carrier);
            return Ok(ApiResponse<CarrierResponse>.SuccessResult(carrierResponse, "Carrier updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating carrier {CarrierId}", id);
            return StatusCode(500, ApiResponse<CarrierResponse>.ErrorResult("An error occurred while updating carrier"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> DeleteCarrier(long id)
    {
        try
        {
            var carrier = await _unitOfWork.Carriers.GetByIdAsync(id);
            if (carrier == null)
            {
                return NotFound(ApiResponse.ErrorResult("Carrier not found"));
            }

            // Check if carrier has active shipments
            var activeShipments = carrier.Shipments.Where(s => s.Status != Core.Enums.ShipmentStatus.Delivered && 
                                                               s.Status != Core.Enums.ShipmentStatus.Cancelled);
            
            if (activeShipments.Any())
            {
                return BadRequest(ApiResponse.ErrorResult("Cannot delete carrier with active shipments"));
            }

            await _unitOfWork.Carriers.DeleteAsync(carrier);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Carrier {CarrierId} deleted", id);
            return Ok(ApiResponse.SuccessResult("Carrier deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting carrier {CarrierId}", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while deleting carrier"));
        }
    }

    [HttpGet("{id}/shipments")]
    [Authorize(Roles = "CarrierOperator,Admin")]
    public async Task<ActionResult<ApiResponse<List<ShipmentResponse>>>> GetCarrierShipments(long id)
    {
        try
        {
            var carrier = await _unitOfWork.Carriers.GetByIdAsync(id);
            if (carrier == null)
            {
                return NotFound(ApiResponse<List<ShipmentResponse>>.ErrorResult("Carrier not found"));
            }

            var shipments = await _unitOfWork.Shipments.GetShipmentsByCarrierAsync(id);
            var shipmentResponses = _mapper.Map<List<ShipmentResponse>>(shipments);
            return Ok(ApiResponse<List<ShipmentResponse>>.SuccessResult(shipmentResponses));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shipments for carrier {CarrierId}", id);
            return StatusCode(500, ApiResponse<List<ShipmentResponse>>.ErrorResult("An error occurred while retrieving carrier shipments"));
        }
    }
}
