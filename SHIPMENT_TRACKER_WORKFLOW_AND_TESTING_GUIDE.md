# 🚢 Shipment Tracker - Complete Workflow & Testing Guide

## 📋 **Project Overview**

The Shipment Tracker is a comprehensive logistics management system that tracks shipments from creation to delivery through multiple stages involving different user roles. The system follows a **batch-based workflow** where multiple shipments are grouped together and move through various facilities.

## 🎭 **User Roles & Responsibilities**

1. **Admin** - Full system access, manages master data
2. **DataEntry** - Creates shipments and batches at branch level
3. **BranchAdmin** - Manages branch operations, assigns carriers
4. **WarehouseOperator** - Processes batches in warehouses
5. **PortOperator** - Handles port operations and customs clearance
6. **CarrierOperator** - Manages delivery operations
7. **Client** - Views their own shipments and announcements

## 🔄 **Complete Workflow (End-to-End)**

### **Phase 1: Setup & Master Data (Admin)**
```
Admin creates → Branches → Warehouses → Ports → Carriers
```

### **Phase 2: Shipment Creation (DataEntry)**
```
DataEntry creates → Shipments → Groups into Batches → Closes Batch
```

### **Phase 3: Logistics Processing**
```
WarehouseOperator → PortOperator → CarrierOperator → Delivery
```

### **Phase 4: Client Tracking**
```
Client views → Shipment Status → Tracking History → Announcements
```

## 📊 **Batch State Machine**

| Status | Allowed Transitions (Next) | Actor Roles / Validations |
| --- | --- | --- |
| **Draft** | Open (when first shipment added) | DataEntry; batch must contain at least one shipment. |
| **Open** | Closed | DataEntry; threshold of shipments/weight reached; no pending shipment validations. |
| **Closed** | InWarehouse | WarehouseOperator; warehouse location assigned. |
| **InWarehouse** | AtSourcePort (batch leaves warehouse), Open (if reopened for corrections) | WarehouseOperator; quality control complete. |
| **AtSourcePort** | ClearedSourcePort (customs cleared), InWarehouse (if failed inspection) | PortOperator; customs clearance documents validated. |
| **ClearedSourcePort** | InWarehouse (return to source warehouse), InTransit | PortOperator; batch can return to warehouse or proceed to transit. |
| **InTransit** | ArrivedDestinationPort | System/PortOperator via integration; arrival confirmed. |
| **ArrivedDestinationPort** | InDestinationWarehouse | WarehouseOperator; batch moved to destination warehouse. |
| **InDestinationWarehouse** | AssignedToCarriers | BranchAdmin; carriers assigned to shipments. |
| **AssignedToCarriers** | Delivered or PartiallyDelivered (once all shipments delivered) | CarrierOperator updates shipments individually. |
| **Delivered / PartiallyDelivered** | Archived | System or Admin; after retention period. |
| **Cancelled** | - (terminal) | Admin; only possible before InWarehouse. |

## 📦 **Shipment State Machine**

| Status | Allowed Transitions (Next) | Actor Roles / Validations |
| --- | --- | --- |
| **Created** | InBatch | DataEntry; assign to an open batch. |
| **InBatch** | InWarehouse (when batch moves) | System; inherits batch status. |
| **InWarehouse** | AtSourcePort, Cancelled | WarehouseOperator. |
| **AtSourcePort** | InTransit, Returned (if rejected), Cancelled | PortOperator. |
| **InTransit** | AtDestinationPort, Returned | System via transport integration. |
| **AtDestinationPort** | WithCarrier | DestinationPortOperator; carrier assignment required. |
| **WithCarrier** | OutForDelivery, Returned | CarrierOperator; indicates driver picked up. |
| **OutForDelivery** | Delivered, Returned | CarrierOperator; proof of delivery captured. |
| **Delivered** | -   | Terminal status; update batch progress. |
| **Returned** | InWarehouse (re‑process) or Cancelled | Operator; depending on policy. |
| **Cancelled** | -   | Terminal; may occur if shipment voided before being shipped. |

---

## 🧪 **Complete Testing Guide (As Admin User)**

### **Step 1: Authentication Setup**
First, you need to authenticate as an Admin user:

```http
POST /api/auth/login
Content-Type: application/json

{
  "userName": "admin",
  "password": "your-admin-password"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "abc123...",
    "expiresAt": "2024-01-15T10:30:00Z",
    "roles": ["Admin"],
    "userId": 1,
    "userName": "admin",
    "displayName": "System Administrator",
    "email": "admin@shipmenttracker.com"
  }
}
```

**Save the `accessToken` for all subsequent requests!**

---

### **Step 2: Master Data Setup (Admin Only)**

#### **2.1 Create Branches**
```http
POST /api/branches
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "name": "New York Branch",
  "address": "123 Main St, New York, NY 10001"
}
```

```http
POST /api/branches
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "name": "Los Angeles Branch", 
  "address": "456 Oak Ave, Los Angeles, CA 90210"
}
```

#### **2.2 Create Warehouses**
```http
POST /api/warehouses
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "name": "NYC Warehouse",
  "address": "789 Industrial Blvd, Brooklyn, NY 11201"
}
```

```http
POST /api/warehouses
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "name": "LAX Warehouse",
  "address": "321 Logistics Way, Los Angeles, CA 90058"
}
```

#### **2.3 Create Ports**
```http
POST /api/ports
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "name": "Port of New York",
  "address": "Port Authority, New York, NY",
  "country": "USA"
}
```

```http
POST /api/ports
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "name": "Port of Los Angeles",
  "address": "Port of LA, San Pedro, CA",
  "country": "USA"
}
```

#### **2.4 Create Carriers**
```http
POST /api/carriers
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "name": "FastTrack Delivery",
  "contactInfo": "Phone: (555) 123-4567, Email: dispatch@fasttrack.com"
}
```

```http
POST /api/carriers
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "name": "Reliable Logistics",
  "contactInfo": "Phone: (555) 987-6543, Email: info@reliable.com"
}
```

#### **2.5 Verify Master Data**
```http
GET /api/branches
Authorization: Bearer {accessToken}
```

```http
GET /api/warehouses
Authorization: Bearer {accessToken}
```

```http
GET /api/ports
Authorization: Bearer {accessToken}
```

```http
GET /api/carriers
Authorization: Bearer {accessToken}
```

---

### **Step 3: Create Test Client (Admin)**

#### **3.1 Register a Test Client**
```http
POST /api/auth/register
Content-Type: application/json

{
  "userName": "testclient",
  "email": "client@test.com",
  "displayName": "Test Client",
  "password": "TestPass123!",
  "phoneNumber": "+1-555-0123"
}
```

#### **3.2 Verify Email (Simulate)**
```http
POST /api/auth/verify-email
Content-Type: application/json

{
  "token": "verification-token-from-email"
}
```

---

### **Step 4: Shipment & Batch Creation (DataEntry Role)**

#### **4.1 Create Shipments**
```http
POST /api/shipments
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "clientId": 2,
  "weight": 5.5,
  "volume": 2.0,
  "pickupAddress": "123 Client St, New York, NY",
  "deliveryAddress": "456 Delivery Ave, Los Angeles, CA"
}
```

```http
POST /api/shipments
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "clientId": 2,
  "weight": 3.2,
  "volume": 1.5,
  "pickupAddress": "789 Pickup Rd, New York, NY",
  "deliveryAddress": "321 Destination St, Los Angeles, CA"
}
```

#### **4.2 Create Batch**
```http
POST /api/batches
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "name": "NYC to LAX Batch 001",
  "branchId": 1,
  "thresholdCount": 2,
  "thresholdWeight": 5.0
}
```

#### **4.3 Add Shipments to Batch**
```http
POST /api/batches/1/shipments/1
Authorization: Bearer {accessToken}
```

```http
POST /api/batches/1/shipments/2
Authorization: Bearer {accessToken}
```

#### **4.4 Close Batch (Threshold Reached)**
```http
POST /api/batches/1/close
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "notes": "Batch ready for warehouse processing"
}
```

---

### **Step 5: Warehouse Processing (WarehouseOperator Role)**

#### **5.1 Move Batch to Source Warehouse**
```http
POST /api/batches/1/move-to-warehouse
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "sourceWarehouseId": 1,
  "notes": "Batch received at NYC warehouse"
}
```

#### **5.2 Assign Destination Warehouse**
```http
POST /api/batches/1/assign-destination-warehouse
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "destinationWarehouseId": 2,
  "notes": "Destination warehouse assigned for final delivery"
}
```

#### **5.3 Verify Warehouse Status**
```http
GET /api/warehouses/1/batches
Authorization: Bearer {accessToken}
```

```http
GET /api/warehouses/2/batches
Authorization: Bearer {accessToken}
```

---

### **Step 6: Port Operations (PortOperator Role)**

#### **6.1 Move to Source Port**
```http
POST /api/batches/1/move-to-source-port
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "sourcePortId": 1,
  "destinationPortId": 2,
  "notes": "Batch moved to source port for customs"
}
```

#### **6.2 Clear Source Port (Customs)**
```http
POST /api/batches/1/clear-source-port
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "notes": "Customs clearance completed"
}
```

#### **6.3 Return to Source Warehouse (After Customs)**
```http
POST /api/batches/1/move-to-warehouse
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "sourceWarehouseId": 1,
  "notes": "Batch returned to source warehouse after customs clearance"
}
```

#### **6.4 Start Transit**
```http
POST /api/batches/1/start-transit
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "notes": "Vessel departed for destination"
}
```

#### **6.5 Mark Arrival at Destination**
```http
POST /api/batches/1/arrival
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "notes": "Vessel arrived at destination port"
}
```

#### **6.6 Move to Destination Warehouse**
```http
POST /api/batches/1/move-to-destination-warehouse
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "sourceWarehouseId": 2,
  "notes": "Batch moved to destination warehouse for final processing"
}
```

---

### **Step 7: Carrier Assignment (BranchAdmin Role)**

#### **7.1 Assign Carriers to Shipments**
```http
POST /api/batches/1/assign-carriers
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "assignments": [
    {
      "shipmentId": 1,
      "carrierId": 1
    },
    {
      "shipmentId": 2,
      "carrierId": 2
    }
  ]
}
```

---

### **Step 8: Delivery Operations (CarrierOperator Role)**

#### **8.1 Update Shipment Status - Out for Delivery**
```http
PATCH /api/shipments/1/status
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "status": 7,
  "notes": "Package picked up by driver"
}
```

#### **8.2 Update Shipment Status - Delivered**
```http
PATCH /api/shipments/1/status
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "status": 8,
  "notes": "Package delivered successfully"
}
```

---

### **Step 9: Client Experience Testing**

#### **9.1 Login as Client**
```http
POST /api/auth/login
Content-Type: application/json

{
  "userName": "testclient",
  "password": "TestPass123!"
}
```

#### **9.2 View Client's Shipments**
```http
GET /api/clients/me/shipments
Authorization: Bearer {clientAccessToken}
```

#### **9.3 View Specific Shipment with Tracking**
```http
GET /api/clients/me/shipments/1
Authorization: Bearer {clientAccessToken}
```

#### **9.4 View Client Announcements**
```http
GET /api/clients/me/announcements
Authorization: Bearer {clientAccessToken}
```

---

### **Step 10: Admin Management Features**

#### **10.1 Create Announcement**
```http
POST /api/announcements
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "title": "Holiday Shipping Notice",
  "content": "Please note that shipping will be delayed during the holiday season.",
  "startDate": "2024-12-01T00:00:00Z",
  "endDate": "2024-12-31T23:59:59Z",
  "targets": [
    {
      "clientId": 2
    }
  ]
}
```

#### **10.2 View Top Clients**
```http
GET /api/clients/top?branchId=1&count=10
Authorization: Bearer {accessToken}
```

#### **10.3 View All Batches**
```http
GET /api/batches?branchId=1&status=8
Authorization: Bearer {accessToken}
```

#### **10.4 View All Shipments**
```http
GET /api/shipments?status=8
Authorization: Bearer {accessToken}
```

---

## 🔍 **Testing Checklist**

### **✅ Authentication & Authorization**
- [ ] Admin login works
- [ ] Client registration and email verification
- [ ] Role-based access control on all endpoints
- [ ] JWT token validation

### **✅ Master Data Management**
- [ ] Create/Read/Update/Delete Branches
- [ ] Create/Read/Update/Delete Warehouses  
- [ ] Create/Read/Update/Delete Ports
- [ ] Create/Read/Update/Delete Carriers
- [ ] Unique name validation
- [ ] Referential integrity checks

### **✅ Shipment Lifecycle**
- [ ] Create shipments
- [ ] Add shipments to batches
- [ ] Close batches when threshold reached
- [ ] Move batches through warehouse → port → transit → delivery
- [ ] Assign carriers to shipments
- [ ] Update shipment status through delivery

### **✅ Client Experience**
- [ ] Client can view their shipments
- [ ] Client can see tracking history
- [ ] Client can view announcements
- [ ] Client data isolation (can't see other clients' data)

### **✅ Admin Features**
- [ ] Create targeted announcements
- [ ] View top clients by branch
- [ ] Manage all master data
- [ ] View system-wide statistics

---

## 🚀 **Quick Start Testing Commands**

If you want to test quickly, here's the minimal sequence:

1. **Login as Admin** → Get token
2. **Create 1 Branch, 2 Warehouses, 2 Ports, 1 Carrier**
3. **Create 2 Shipments, 1 Batch, Add shipments to batch, Close batch**
4. **Move batch to source warehouse → Assign destination warehouse → Source Port → Clear → Return to Source Warehouse → Transit → Arrival → Destination Warehouse**
5. **Assign carriers → Update shipment status to Delivered**
6. **Login as Client → View shipments and tracking**

This covers the complete end-to-end workflow! 🎯

---

## 📚 **API Endpoints Reference**

### **Authentication Endpoints**
- `POST /api/auth/register` - Register new user
- `POST /api/auth/verify-email` - Verify email address
- `POST /api/auth/login` - User login
- `POST /api/auth/refresh` - Refresh access token
- `POST /api/auth/logout` - User logout
- `POST /api/auth/forgot-password` - Request password reset
- `POST /api/auth/reset-password` - Reset password
- `POST /api/auth/resend-verification` - Resend verification email

### **Master Data Endpoints**

#### **Branches**
- `GET /api/branches` - List all branches
- `GET /api/branches/{id}` - Get branch details
- `POST /api/branches` - Create branch (Admin)
- `PUT /api/branches/{id}` - Update branch (Admin)
- `DELETE /api/branches/{id}` - Delete branch (Admin)
- `GET /api/branches/{id}/batches` - Get batches for branch

#### **Warehouses**
- `GET /api/warehouses` - List all warehouses
- `GET /api/warehouses/{id}` - Get warehouse details (shows source/destination batch counts)
- `POST /api/warehouses` - Create warehouse (Admin)
- `PUT /api/warehouses/{id}` - Update warehouse (Admin)
- `DELETE /api/warehouses/{id}` - Delete warehouse (Admin)
- `GET /api/warehouses/{id}/batches` - Get batches in warehouse

#### **Ports**
- `GET /api/ports` - List all ports (with country filter)
- `GET /api/ports/{id}` - Get port details
- `POST /api/ports` - Create port (Admin)
- `PUT /api/ports/{id}` - Update port (Admin)
- `DELETE /api/ports/{id}` - Delete port (Admin)
- `GET /api/ports/{id}/batches` - Get batches at port

#### **Carriers**
- `GET /api/carriers` - List all carriers
- `GET /api/carriers/{id}` - Get carrier details
- `POST /api/carriers` - Create carrier (Admin)
- `PUT /api/carriers/{id}` - Update carrier (Admin)
- `DELETE /api/carriers/{id}` - Delete carrier (Admin)
- `GET /api/carriers/{id}/shipments` - Get shipments assigned to carrier

### **Batch Management**
- `GET /api/batches` - List batches (with filters)
- `POST /api/batches` - Create new batch
- `GET /api/batches/{id}` - Get batch details
- `POST /api/batches/{id}/close` - Close batch
- `POST /api/batches/{id}/shipments/{shipmentId}` - Add shipment to batch
- `POST /api/batches/{id}/move-to-warehouse` - Move to source warehouse
- `POST /api/batches/{id}/assign-destination-warehouse` - Assign destination warehouse
- `POST /api/batches/{id}/move-to-source-port` - Move to source port
- `POST /api/batches/{id}/clear-source-port` - Clear customs
- `POST /api/batches/{id}/start-transit` - Start transit
- `POST /api/batches/{id}/arrival` - Mark arrival
- `POST /api/batches/{id}/move-to-destination-warehouse` - Move to destination warehouse
- `POST /api/batches/{id}/assign-carriers` - Assign carriers
- `DELETE /api/batches/{id}` - Cancel batch

### **Shipment Management**
- `GET /api/shipments` - List shipments (with filters)
- `POST /api/shipments` - Create shipment
- `GET /api/shipments/{id}` - Get shipment details
- `PATCH /api/shipments/{id}/status` - Update shipment status
- `DELETE /api/shipments/{id}` - Cancel shipment
- `GET /api/shipments/unassigned` - Get unassigned shipments

### **Client Endpoints**
- `GET /api/clients/me` - Get current client info
- `GET /api/clients/me/shipments` - Get my shipments
- `GET /api/clients/me/shipments/{id}` - Get my shipment details
- `GET /api/clients/me/announcements` - Get my announcements
- `GET /api/clients/top` - Get top clients by branch (Admin)

### **Announcements**
- `GET /api/announcements` - Get announcements for current user
- `POST /api/announcements` - Create announcement (Admin)
- `GET /api/announcements/{id}` - Get announcement details
- `PUT /api/announcements/{id}` - Update announcement (Admin)
- `DELETE /api/announcements/{id}` - Delete announcement (Admin)

### **Role Management**
- `GET /api/roles` - List all roles (Admin)
- `GET /api/roles/{id}` - Get role details (Admin)
- `POST /api/roles` - Create role (Admin)
- `PUT /api/roles/{id}` - Update role (Admin)
- `DELETE /api/roles/{id}` - Delete role (Admin)

### **User-Role Assignment**
- `GET /api/users/{userId}/roles` - Get user's roles (Admin)
- `POST /api/users/{userId}/roles` - Assign role to user (Admin)
- `DELETE /api/users/{userId}/roles/{roleId}` - Remove role from user (Admin)

### **User Management**
- `GET /api/auth/users` - List all users (Admin)
- `GET /api/auth/users/{id}` - Get user details (Admin)
- `PUT /api/auth/users/{id}` - Update user (Admin)
- `DELETE /api/auth/users/{id}` - Deactivate user (Admin)

---

## 🎯 **Summary**

The Shipment Tracker system is now fully functional with:

- ✅ **Complete CRUD operations** for all entities
- ✅ **Full batch lifecycle management** from creation to delivery
- ✅ **Enhanced warehouse workflow** with source and destination tracking
- ✅ **Complete role management system** with user administration
- ✅ **Role-based access control** with proper authorization
- ✅ **Client tracking interface** with real-time status updates
- ✅ **Announcement system** for targeted communications
- ✅ **Master data management** for branches, warehouses, ports, and carriers
- ✅ **Consistent API responses** with proper error handling
- ✅ **Business rule validation** and referential integrity
- ✅ **Default role assignment** (Client role for new users)

### **🔄 Enhanced Warehouse Workflow**
- **Source Warehouse**: Where batches are initially processed and prepared for shipment
- **Source Port**: Where batches go for customs clearance
- **Source Warehouse (Return)**: Where batches return after customs for final preparation
- **Destination Port**: Where batches arrive for final processing
- **Destination Warehouse**: Where batches are received and prepared for final delivery
- **Complete Lifecycle**: Full tracking from source warehouse → port → source warehouse → destination port → destination warehouse → delivery
- **Flexible Assignment**: Destination warehouse can be assigned at any point during the process
- **Better Tracking**: Clear visibility of batch origin and destination throughout the journey

### **👥 Role Management System**
- **7 Predefined Roles**: DataEntry, BranchAdmin, WarehouseOperator, PortOperator, CarrierOperator, Client, Admin
- **Complete Role CRUD**: Create, read, update, delete roles
- **User-Role Assignment**: Assign and remove roles from users
- **User Management**: View, update, and deactivate users
- **Security Features**: Last admin protection, role validation, business rules

### **📊 Complete API Coverage**
- **Authentication**: Register, login, email verification, password reset
- **Master Data**: Branches, warehouses, ports, carriers
- **Batch Management**: Full lifecycle with warehouse transitions
- **Shipment Tracking**: Real-time status updates and event history
- **Client Interface**: Shipment tracking and announcements
- **Role Management**: Complete user and role administration
- **Announcements**: Targeted communication system

The system is ready for production use and can handle real-world shipment tracking operations with complete user and role management! 🚀
