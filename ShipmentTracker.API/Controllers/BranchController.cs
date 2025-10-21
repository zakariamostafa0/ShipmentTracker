using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipmentTracker.API.DTOs.Batch;
using ShipmentTracker.API.DTOs.Branch;
using ShipmentTracker.API.DTOs.Common;
using ShipmentTracker.Core.Entities;
using ShipmentTracker.Core.Interfaces;

namespace ShipmentTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BranchController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<BranchController> _logger;

    public BranchController(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<BranchController> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<BranchResponse>>>> GetBranches()
    {
        try
        {
            var branches = await _unitOfWork.Branches.GetAllAsync();
            var branchResponses = _mapper.Map<List<BranchResponse>>(branches);
            return Ok(ApiResponse<List<BranchResponse>>.SuccessResult(branchResponses));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving branches");
            return StatusCode(500, ApiResponse<List<BranchResponse>>.ErrorResult("An error occurred while retrieving branches"));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<BranchResponse>>> GetBranch(long id)
    {
        try
        {
            var branch = await _unitOfWork.Branches.GetByIdAsync(id);
            if (branch == null)
            {
                return NotFound(ApiResponse<BranchResponse>.ErrorResult("Branch not found"));
            }

            var branchResponse = _mapper.Map<BranchResponse>(branch);
            return Ok(ApiResponse<BranchResponse>.SuccessResult(branchResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving branch {BranchId}", id);
            return StatusCode(500, ApiResponse<BranchResponse>.ErrorResult("An error occurred while retrieving branch"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<BranchResponse>>> CreateBranch([FromBody] CreateBranchRequest request)
    {
        try
        {
            // Check if branch name already exists
            var existingBranch = await _unitOfWork.Branches.FirstOrDefaultAsync(b => b.Name == request.Name);
            if (existingBranch != null)
            {
                return BadRequest(ApiResponse<BranchResponse>.ErrorResult("Branch name already exists"));
            }

            var branch = _mapper.Map<Branch>(request);
            await _unitOfWork.Branches.AddAsync(branch);
            await _unitOfWork.SaveChangesAsync();

            var branchResponse = _mapper.Map<BranchResponse>(branch);
            return CreatedAtAction(nameof(GetBranch), new { id = branch.Id }, 
                ApiResponse<BranchResponse>.SuccessResult(branchResponse, "Branch created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating branch");
            return StatusCode(500, ApiResponse<BranchResponse>.ErrorResult("An error occurred while creating branch"));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<BranchResponse>>> UpdateBranch(long id, [FromBody] UpdateBranchRequest request)
    {
        try
        {
            var branch = await _unitOfWork.Branches.GetByIdAsync(id);
            if (branch == null)
            {
                return NotFound(ApiResponse<BranchResponse>.ErrorResult("Branch not found"));
            }

            // Check if branch name already exists (excluding current branch)
            var existingBranch = await _unitOfWork.Branches.FirstOrDefaultAsync(b => b.Name == request.Name && b.Id != id);
            if (existingBranch != null)
            {
                return BadRequest(ApiResponse<BranchResponse>.ErrorResult("Branch name already exists"));
            }

            _mapper.Map(request, branch);
            await _unitOfWork.Branches.UpdateAsync(branch);
            await _unitOfWork.SaveChangesAsync();

            var branchResponse = _mapper.Map<BranchResponse>(branch);
            return Ok(ApiResponse<BranchResponse>.SuccessResult(branchResponse, "Branch updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating branch {BranchId}", id);
            return StatusCode(500, ApiResponse<BranchResponse>.ErrorResult("An error occurred while updating branch"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> DeleteBranch(long id)
    {
        try
        {
            var branch = await _unitOfWork.Branches.GetByIdAsync(id);
            if (branch == null)
            {
                return NotFound(ApiResponse.ErrorResult("Branch not found"));
            }

            // Check if branch has active batches
            var activeBatches = await _unitOfWork.Batches.GetBatchesByBranchAsync(id);
            var hasActiveBatches = activeBatches.Any(b => b.Status != Core.Enums.BatchStatus.Delivered && 
                                                         b.Status != Core.Enums.BatchStatus.Cancelled && 
                                                         b.Status != Core.Enums.BatchStatus.Archived);
            
            if (hasActiveBatches)
            {
                return BadRequest(ApiResponse.ErrorResult("Cannot delete branch with active batches"));
            }

            await _unitOfWork.Branches.DeleteAsync(branch);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Branch {BranchId} deleted", id);
            return Ok(ApiResponse.SuccessResult("Branch deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting branch {BranchId}", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while deleting branch"));
        }
    }

    [HttpGet("{id}/batches")]
    [Authorize(Roles = "BranchAdmin,Admin")]
    public async Task<ActionResult<ApiResponse<List<BatchResponse>>>> GetBranchBatches(long id)
    {
        try
        {
            var branch = await _unitOfWork.Branches.GetByIdAsync(id);
            if (branch == null)
            {
                return NotFound(ApiResponse<List<BatchResponse>>.ErrorResult("Branch not found"));
            }

            var batches = await _unitOfWork.Batches.GetBatchesByBranchAsync(id);
            var batchResponses = _mapper.Map<List<BatchResponse>>(batches);
            return Ok(ApiResponse<List<BatchResponse>>.SuccessResult(batchResponses));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving batches for branch {BranchId}", id);
            return StatusCode(500, ApiResponse<List<BatchResponse>>.ErrorResult("An error occurred while retrieving branch batches"));
        }
    }
}
