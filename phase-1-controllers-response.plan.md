<!-- 314644bc-fb64-42dd-8d93-25ab1a1da0ec 229d677d-1548-425d-bc26-cb34daffe8cb -->
# Phase 1B: Master Data Controllers - UPDATED

## Overview
Implement controllers for master data entities (Branch, Warehouse, Port, Carrier) that are referenced throughout the batch lifecycle. These entities are required for batch creation and status transitions.

## Current Issue
The BatchController allows assigning warehouses, ports, and carriers, but there are no endpoints to:
- List available branches (for batch creation)
- List available warehouses (for move-to-warehouse operation)
- List available ports (for port operations)
- List available carriers (for carrier assignment)

## Master Data Flow - UPDATED WORKFLOW
According to the enhanced workflow with source and destination warehouses:
1. **Branch** - Batches are created at a branch; DataEntry must select a branch
2. **Source Warehouse** - WarehouseOperator assigns batch to a source warehouse for initial processing
3. **Source Port** - PortOperator moves batch from source warehouse to source port for customs
4. **Source Warehouse (Return)** - After customs clearance, batch returns to source warehouse for final preparation
5. **Destination Port** - Batch moves to destination port for transit
6. **Destination Warehouse** - Batch arrives at destination warehouse for final processing
7. **Carrier** - BranchAdmin assigns carriers to individual shipments for delivery

## Enhanced Batch Workflow States
```
Draft → Open → Closed → InWarehouse (Source) → AtSourcePort → InWarehouse (Source) → InTransit → ArrivedDestinationPort → AssignedToCarriers → Delivered
```

## 1. Create Master Data DTOs

### Branch DTOs (ShipmentTracker.API/DTOs/Branch/)
- `BranchResponse.cs` - Id, Name, Address, BatchCount
- `CreateBranchRequest.cs` - Name, Address (Admin only)
- `UpdateBranchRequest.cs` - Name, Address (Admin only)

### Warehouse DTOs (ShipmentTracker.API/DTOs/Warehouse/) - UPDATED
- `WarehouseResponse.cs` - Id, Name, Address, SourceBatchCount, DestinationBatchCount, ActiveBatchCount
- `CreateWarehouseRequest.cs` - Name, Address (Admin only)
- `UpdateWarehouseRequest.cs` - Name, Address (Admin only)

### Port DTOs (ShipmentTracker.API/DTOs/Port/)
- `PortResponse.cs` - Id, Name, Address, Country, ActiveBatchCount
- `CreatePortRequest.cs` - Name, Address, Country (Admin only)
- `UpdatePortRequest.cs` - Name, Address, Country (Admin only)

### Carrier DTOs (ShipmentTracker.API/DTOs/Carrier/)
- `CarrierResponse.cs` - Id, Name, ContactInfo, ActiveShipmentCount
- `CreateCarrierRequest.cs` - Name, ContactInfo (Admin only)
- `UpdateCarrierRequest.cs` - Name, ContactInfo (Admin only)

## 2. Update AutoMapper Profile - UPDATED
Add mappings for all master data entities with enhanced warehouse tracking:
- Branch <-> BranchResponse
- Warehouse <-> WarehouseResponse (with source/destination batch counts)
- Port <-> PortResponse
- Carrier <-> CarrierResponse

## 3. BranchController
**File**: `ShipmentTracker.API/Controllers/BranchController.cs`

Endpoints:
- `GET /api/branches` - List all branches - **Roles**: All authenticated users
- `GET /api/branches/{id}` - Get branch details - **Roles**: All authenticated users
- `POST /api/branches` - Create branch - **Roles**: Admin
- `PUT /api/branches/{id}` - Update branch - **Roles**: Admin
- `DELETE /api/branches/{id}` - Delete branch (soft delete) - **Roles**: Admin
- `GET /api/branches/{id}/batches` - Get batches for branch - **Roles**: BranchAdmin, Admin

**Business Rules**:
- Branch name must be unique
- Cannot delete branch with active batches
- Only admins can create/update/delete branches

## 4. WarehouseController - UPDATED
**File**: `ShipmentTracker.API/Controllers/WarehouseController.cs`

Endpoints:
- `GET /api/warehouses` - List all warehouses - **Roles**: WarehouseOperator, Admin
- `GET /api/warehouses/{id}` - Get warehouse details - **Roles**: WarehouseOperator, Admin
- `POST /api/warehouses` - Create warehouse - **Roles**: Admin
- `PUT /api/warehouses/{id}` - Update warehouse - **Roles**: Admin
- `DELETE /api/warehouses/{id}` - Delete warehouse - **Roles**: Admin
- `GET /api/warehouses/{id}/batches` - Get batches in warehouse - **Roles**: WarehouseOperator, Admin

**Business Rules**:
- Warehouse name must be unique
- Cannot delete warehouse with batches currently in it
- Track both source and destination batch counts

## 5. PortController
**File**: `ShipmentTracker.API/Controllers/PortController.cs`

Endpoints:
- `GET /api/ports` - List all ports (with optional country filter) - **Roles**: PortOperator, Admin
- `GET /api/ports/{id}` - Get port details - **Roles**: PortOperator, Admin
- `POST /api/ports` - Create port - **Roles**: Admin
- `PUT /api/ports/{id}` - Update port - **Roles**: Admin
- `DELETE /api/ports/{id}` - Delete port - **Roles**: Admin
- `GET /api/ports/{id}/batches` - Get batches at port - **Roles**: PortOperator, Admin

**Business Rules**:
- Port name + country combination must be unique
- Cannot delete port with batches currently at it

## 6. CarrierController
**File**: `ShipmentTracker.API/Controllers/CarrierController.cs`

Endpoints:
- `GET /api/carriers` - List all carriers - **Roles**: BranchAdmin, CarrierOperator, Admin
- `GET /api/carriers/{id}` - Get carrier details - **Roles**: BranchAdmin, CarrierOperator, Admin
- `POST /api/carriers` - Create carrier - **Roles**: Admin
- `PUT /api/carriers/{id}` - Update carrier - **Roles**: Admin
- `DELETE /api/carriers/{id}` - Delete carrier - **Roles**: Admin
- `GET /api/carriers/{id}/shipments` - Get shipments assigned to carrier - **Roles**: CarrierOperator, Admin

**Business Rules**:
- Carrier name must be unique
- Cannot delete carrier with active shipments

## 7. Update BatchController - ENHANCED
Fix the BatchController to properly handle the enhanced warehouse workflow:

**Changes needed**:
- `POST /api/batches` - Validate BranchId exists
- `POST /api/batches/{id}/move-to-warehouse` - Accept sourceWarehouseId in request body and validate it exists
- `POST /api/batches/{id}/assign-destination-warehouse` - NEW: Assign destination warehouse
- `POST /api/batches/{id}/move-to-source-port` - Accept sourcePortId and destinationPortId in request body
- Update BatchResponse DTOs to include source and destination warehouse names

## 8. Additional Request DTOs for Batch Operations - UPDATED

### MoveToWarehouseRequest - UPDATED
- `SourceWarehouseId` (required) - Changed from WarehouseId
- `Notes` (optional)

### AssignDestinationWarehouseRequest - NEW
- `DestinationWarehouseId` (required)
- `Notes` (optional)

### MoveToPortRequest
- `SourcePortId` (required for move-to-source-port)
- `DestinationPortId` (optional for future use)
- `Notes` (optional)

## 9. Enhanced Batch Workflow Operations - NEW

### Batch Status Transitions
1. **Draft** → **Open** (when first shipment added)
2. **Open** → **Closed** (when threshold reached)
3. **Closed** → **InWarehouse** (moved to source warehouse)
4. **InWarehouse** → **AtSourcePort** (moved to source port for customs)
5. **AtSourcePort** → **InWarehouse** (returned to source warehouse after customs)
6. **InWarehouse** → **InTransit** (started transit to destination)
7. **InTransit** → **ArrivedDestinationPort** (arrived at destination port)
8. **ArrivedDestinationPort** → **InDestinationWarehouse** (moved to destination warehouse)
9. **InDestinationWarehouse** → **AssignedToCarriers** (carriers assigned)
10. **AssignedToCarriers** → **Delivered** (all shipments delivered)

### New Batch Endpoints
- `POST /api/batches/{id}/assign-destination-warehouse` - Assign destination warehouse
- `POST /api/batches/{id}/move-to-destination-warehouse` - Move to destination warehouse
- Enhanced warehouse tracking in all batch operations

## Key Implementation Notes - UPDATED
- All master data endpoints use `ApiResponse<T>` wrapper
- List endpoints should return lightweight DTOs (just Id, Name for dropdowns)
- Detail endpoints include related counts (e.g., source/destination batch counts)
- Soft delete for master data (set IsActive = false instead of DELETE)
- Validate references before allowing deletion
- Enhanced warehouse tracking with source and destination relationships
- Support for the complete batch lifecycle with warehouse transitions

## Benefits - ENHANCED
- DataEntry can see available branches when creating batches
- WarehouseOperator can select from available warehouses (source and destination)
- PortOperator can select from available ports
- BranchAdmin can select from available carriers
- Admin can manage all master data
- Complete CRUD for reference data
- **NEW**: Full warehouse workflow support (source → port → source → destination)
- **NEW**: Enhanced tracking of batch locations throughout the journey
- **NEW**: Support for complex logistics operations

## Database Schema Updates - COMPLETED
- ✅ Batch entity updated with SourceWarehouseId and DestinationWarehouseId
- ✅ Warehouse entity updated with separate navigation properties
- ✅ Migration created for database schema changes
- ✅ AutoMapper mappings updated for new warehouse structure

## Role Management System - COMPLETED
- ✅ RoleController implemented for role CRUD operations
- ✅ UserRoleController implemented for user-role assignments
- ✅ User management endpoints added to AuthController
- ✅ Default Client role assignment during registration
- ✅ Complete role-based access control

### To-dos - UPDATED STATUS

- [x] Create DTOs for Branch, Warehouse, Port, and Carrier (Request and Response)
- [x] Update AutoMapper profile with master data mappings
- [x] Implement BranchController with 6 endpoints
- [x] Implement WarehouseController with 6 endpoints
- [x] Implement PortController with 6 endpoints
- [x] Implement CarrierController with 6 endpoints
- [x] Create MoveToWarehouseRequest and MoveToPortRequest DTOs
- [x] Create AssignDestinationWarehouseRequest DTO
- [x] Update BatchController operations to accept and validate warehouse/port IDs
- [x] Update Batch entity with SourceWarehouseId and DestinationWarehouseId
- [x] Update Warehouse entity with separate navigation properties
- [x] Create database migration for warehouse structure changes
- [x] Implement Role Management System
- [x] Implement User Management System
- [x] Verify default Client role assignment during registration

## Current Status: ✅ COMPLETED
All Phase 1B requirements have been implemented including:
- Complete master data controllers
- Enhanced warehouse workflow support
- Role management system
- User management system
- Database schema updates
- AutoMapper configurations
- API response wrappers
- Role-based authorization

The system now supports the complete batch lifecycle with proper warehouse tracking from source to destination! 🚀
