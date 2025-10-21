# Shipment Tracking System Backend Design

## Executive Summary

This document proposes a complete backend design for a shipment‑tracking system. The system must allow branch staff (data entry users and branch administrators), warehouse and port operators, carriers, clients and administrators to track batches and shipments throughout their lifecycle. Batches group multiple shipments and move through branches, warehouses and ports until final delivery by carriers. Clients should be able to log in and view the status and history of their shipments. Administrators can publish targeted announcements to specific groups of clients. The backend should be **scalable**, **secure**, **reliable**, and **observable**. The chosen tech stack is **ASP.NET Web API** (C#) with **SQL Server** as the primary data store.

The design follows **clean architecture** so that the core business logic and domain model are independent of infrastructure concerns. As Microsoft's guidelines describe, clean architecture places the application's business logic in the **Application Core** and inverts the dependency: infrastructure (data access) and the UI depend on the core, not the other way around[\[1\]](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures#:~:text=Clean%20architecture). This separation simplifies unit testing and makes it easy to evolve the data access layer or switch to microservices in the future. The system is designed to support high read volume by employing horizontal scaling strategies such as **read replicas**, **caching**, and **partitioning**[\[2\]](https://www.red-gate.com/simple-talk/databases/sql-server/performance-sql-server/designing-highly-scalable-database-architectures/#:~:text=If%20you%20want%20to%20handle,instances%20by%20implementing%20horizontal%20scaling). Security is achieved through **JWT** and **refresh tokens**[\[3\]](https://medium.com/@MatinGhanbari/building-a-secure-api-with-asp-net-core-jwt-and-refresh-tokens-03dac37b4055#:~:text=Step%201%3A%20Setting%20Up%20JWT,NET%20Core) with role‑based access control.

## Architecture Overview

### Clean Architecture Projects

Clean architecture advocates separating concerns into concentric layers[\[1\]](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures#:~:text=Clean%20architecture):

- **Core (.Core)** - Contains domain entities (Batch, Shipment, Client, etc.), value objects, enums, interfaces, and domain services. Entities encapsulate business rules and track state transitions. Interfaces define repositories and service contracts.
- **Infrastructure (.Data)** - Implements the interfaces defined in Core, such as EF Core repositories and external services (email, caching, etc.). It holds the DbContext and EF Core migrations, and implements the **Outbox** pattern for reliable event publication.
- **API (.API)** - ASP.NET Web API project hosting controllers, DTOs, authentication middleware, and dependency‑injection configuration. Controllers map HTTP requests to Core commands/queries and return DTOs. This layer references Core and Infrastructure.

flowchart TD  
subgraph API  
direction TB  
C1\[Controllers\]  
C2\[DTOs\]  
C3\[Auth & Middleware\]  
end  
subgraph Core  
direction TB  
E\[Entities & Value Objects\]  
S\[Domain Services\]  
I\[Interfaces / Repositories\]  
D\[Domain Events\]  
end  
subgraph Infrastructure  
direction TB  
R\[EF Core DbContext\]  
Impl\[Repository Implementations\]  
Outbox\[Outbox event publisher\]  
Cfg\[SQL & caching configuration\]  
end  
C1 -->|Calls| I  
C2 --> C1  
C3 -->|Auth| C1  
Impl -->|Implements| I  
R --> Impl  
Outbox --> Impl

### High‑level Data Flow

- **Shipment creation** - Data entry users at a branch create shipments. Each shipment is initially in Created status. Shipments can be grouped into an open batch.
- **Batch lifecycle** - When the number of shipments or weight threshold is reached, the batch is closed and moves to the warehouse. Warehouse and port operators update the batch and shipments through several status transitions until carriers deliver them.
- **Tracking** - Each significant status change produces a **ShipmentEvent**. Clients query the API to see the latest shipment status and event history. A **read‑model** or cache can accelerate these queries.
- **Announcements** - Administrators create announcements targeted to particular branches or top clients. Clients retrieve announcements that match their identity and filters.

## Workflow & State Machines

### Batch State Machine

Batches move through specific states. Only certain roles may trigger each transition. The status progression is linear but may be cancelled or archived. Unhappy paths (e.g., port clearance failure) should record an event and move the batch to an exception state requiring manual intervention.

| Status | Allowed Transitions (Next) | Actor Roles / Validations |
| --- | --- | --- |
| **Draft** | Open (when first shipment added) | DataEntry; batch must contain at least one shipment. |
| **Open** | Closed | DataEntry; threshold of shipments/weight reached; no pending shipment validations. |
| **Closed** | InWarehouse | WarehouseOperator; warehouse location assigned. |
| **InWarehouse** | AtSourcePort (batch leaves warehouse), Open (if reopened for corrections) | WarehouseOperator; quality control complete. |
| **AtSourcePort** | ClearedSourcePort (customs cleared), InWarehouse (if failed inspection) | PortOperator; customs clearance documents validated. |
| **ClearedSourcePort** | InTransit | PortOperator; vessel assignment recorded. |
| **InTransit** | ArrivedDestinationPort | System/PortOperator via integration; arrival confirmed. |
| **ArrivedDestinationPort** | AssignedToCarriers | DestinationPortOperator; carriers assigned to shipments. |
| **AssignedToCarriers** | Delivered or PartiallyDelivered (once all shipments delivered) | CarrierOperator updates shipments individually. |
| **Delivered / PartiallyDelivered** | Archived | System or Admin; after retention period. |
| **Cancelled** | - (terminal) | Admin; only possible before InWarehouse. |

### Shipment State Machine

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

### End‑to‑End Workflow Sequence

sequenceDiagram  
participant DE as DataEntry  
participant WH as WarehouseOperator  
participant SP as SourcePortOperator  
participant DP as DestinationPortOperator  
participant CO as CarrierOperator  
participant SYS as System  
Note over DE: Create Shipments  
DE->>API: POST /batches {branchId, name}  
API->>DB: Insert Batch (Status=Draft)  
loop adding shipments  
DE->>API: POST /shipments {clientId, weight, ...}  
API->>DB: Insert Shipment (Status=Created)  
DE->>API: POST /batches/{batchId}/shipments/{shipmentId}  
API->>DB: Update Shipment.Status=InBatch & link to batch  
end  
Note over DE: Close batch once threshold reached  
DE->>API: POST /batches/{batchId}/close  
API->>DB: Update Batch.Status=Closed  
API->>Outbox: record BatchClosed event  
WH->>API: POST /batches/{batchId}/move-to-warehouse  
API->>DB: Update Batch.Status=InWarehouse; update shipments  
API->>DB: Insert ShipmentEvents (InWarehouse) for each shipment  
SP->>API: POST /batches/{batchId}/move-to-source-port  
API->>DB: Update statuses to AtSourcePort; record events  
SP->>API: POST /batches/{batchId}/clear-source-port  
API->>DB: Update statuses to ClearedSourcePort  
SP->>API: POST /batches/{batchId}/start-transit  
API->>DB: Update statuses to InTransit  
SYS->>API: (Integration) PATCH /batches/{batchId}/arrival  
API->>DB: Update statuses to ArrivedDestinationPort  
DP->>API: POST /batches/{batchId}/assign-carriers  
API->>DB: Assign shipments to carriers; shipments status=WithCarrier  
CO->>API: PATCH /shipments/{id}/status {Delivered}  
API->>DB: Update shipment & batch statuses; record events

### Validation & Failure Handling

- **Threshold validation** - On closing a batch, the API must check that the number of shipments or weight exceeds the configured threshold and that all shipments in the batch are valid. If the threshold is not met, return a 400 Bad Request.
- **Concurrency** - Use optimistic concurrency (row version/timestamp columns) when updating batch or shipment status. On conflict, retry or return 409 Conflict.
- **Partial failures** - When moving a batch, if some shipments fail validation (e.g., missing customs documents), the API should skip those shipments and return them as part of the response so the user can fix them; unaffected shipments proceed.
- **Outbox** - Significant state changes write to an Outbox table. A background process publishes events to message brokers, ensuring reliable integration with external systems.

## Database Design

### Entity‑Relationship Overview

The system uses SQL Server. All tables use BIGINT identities for primary keys. Foreign‑key constraints enforce referential integrity. Time‑based columns use DATETIME2. To support audit trails, the design uses **shadow/generic log tables**; a generic audit header stores table, key, operation, user and timestamp, and a detail table stores the changed fields and old/new values[\[4\]](https://vertabelo.com/blog/database-design-for-audit-logging/#:~:text=A%20third%20option%20is%20to,are%20required%20for%20this%20technique). This approach avoids requiring a shadow table per audited table and captures minimal data per change.

Key tables:

| Table | Purpose |
| --- | --- |
| Branch | Physical branches where shipments originate. |
| User | Application users with hashed passwords and roles. |
| Role and UserRole | Roles: DataEntry, BranchAdmin, WarehouseOperator, PortOperator, CarrierOperator, Client, Admin. |
| Client | Customers who create shipments; may have many shipments. |
| Batch | Group of shipments created at a branch. Includes status, thresholds and location references. |
| Shipment | Individual parcel. References client, batch, weight/volume, current status. |
| Warehouse | Warehouses for storage/processing. |
| Port | Ports (source and destination). |
| Carrier | Shipping companies responsible for last‑mile delivery. |
| ShipmentEvent | Records chronological tracking events for shipments. Contains actor, location, timestamp. |
| Announcement | Messages targeted to clients; includes title, content, start/end dates and filters. |
| AnnouncementTarget | Linking table specifying which branches, clients or client groups the announcement targets. |
| AuditHeader / AuditDetail | Generic audit log tables capturing changes to important entities[\[4\]](https://vertabelo.com/blog/database-design-for-audit-logging/#:~:text=A%20third%20option%20is%20to,are%20required%20for%20this%20technique). |
| OutboxEvent | Stores pending domain events for asynchronous processing. |

### Important Columns & Constraints

Below is a summary of core tables and columns. Nullable columns are marked (NULL); otherwise they are NOT NULL.

#### Branch

| Column | Type | Description | Constraints |
| --- | --- | --- | --- |
| Id  | BIGINT | Primary key. | PK, Identity |
| Name | NVARCHAR(100) | Branch name. | UNIQUE |
| Address | NVARCHAR(200) | Full address. |     |
| CreatedAt | DATETIME2 | When branch was created. | DEFAULT GETUTCDATE() |

#### User / Role

| Column | Type | Description | Constraints |
| --- | --- | --- | --- |
| Id  | BIGINT | Primary key. | PK, Identity |
| UserName | NVARCHAR(50) | Unique username or email. | UNIQUE |
| PasswordHash | VARBINARY(256) | Hashed password (e.g., BCrypt). |     |
| DisplayName | NVARCHAR(100) | Friendly name. |     |
| IsActive | BIT | Soft‑delete flag. | DEFAULT 1 |
| CreatedAt | DATETIME2 | Creation timestamp. | DEFAULT GETUTCDATE() |
| RoleId | (none) | Roles assigned via UserRole join table. |     |

Role table has Id and Name. UserRole has UserId and RoleId (composite PK). Use an index on RoleId for fast role lookups.

#### Client

| Column | Type | Description | Constraints |
| --- | --- | --- | --- |
| Id  | BIGINT | Primary key. | PK, Identity |
| UserId | BIGINT | FK to User; each client is a user. | UNIQUE, FK |
| PhoneNumber | NVARCHAR(20) | Contact phone. | NULL |
| CreatedAt | DATETIME2 | Timestamp. | DEFAULT GETUTCDATE() |

#### Batch

| Column | Type | Description | Constraints |
| --- | --- | --- | --- |
| Id  | BIGINT | Primary key. | PK, Identity |
| BranchId | BIGINT | FK to branch where batch was created. | FK  |
| Name | NVARCHAR(50) | Optional name/identifier for batch. |     |
| Status | TINYINT | Enum (Draft=0…Archived=10). | DEFAULT 0 |
| ShipmentCount | INT | Number of shipments in batch (denormalized). | DEFAULT 0 |
| TotalWeight | DECIMAL(10,2) | Sum of shipment weights; used for threshold. | DEFAULT 0 |
| ThresholdCount | INT | Target number of shipments to trigger closure. |     |
| ThresholdWeight | DECIMAL(10,2) | Target weight threshold. |     |
| WarehouseId | BIGINT | FK to warehouse once assigned. | NULL, FK |
| SourcePortId | BIGINT | FK to source port when moved to port. | NULL, FK |
| DestinationPortId | BIGINT | FK to destination port. | NULL, FK |
| CarrierAssignedAt | DATETIME2 | When carriers assigned. | NULL |
| CreatedAt | DATETIME2 | Timestamp. | DEFAULT GETUTCDATE() |
| UpdatedAt | DATETIME2 | Timestamp updated via triggers. | DEFAULT GETUTCDATE() |

Indexes:  
_IX_Batch_BranchId_Status non‑clustered to fetch batches by branch and status._  
Partition on CreatedAt (e.g., monthly) to improve archival operations.  
\* IX_Batch_UpdatedAt for retrieving recently updated batches.

#### Shipment

| Column | Type | Description | Constraints |
| --- | --- | --- | --- |
| Id  | BIGINT | Primary key. | PK, Identity |
| ClientId | BIGINT | FK to client who requested the shipment. | FK  |
| BatchId | BIGINT | FK to batch; nullable until added. | NULL, FK |
| Status | TINYINT | Enum (Created=0…Cancelled=10). | DEFAULT 0 |
| Weight | DECIMAL(10,2) | Weight of the package. | NOT NULL |
| Volume | DECIMAL(10,2) | Volume (for optional threshold). | NULL |
| PickupAddress | NVARCHAR(200) | Pickup address if different from branch. | NULL |
| DeliveryAddress | NVARCHAR(200) | Client's delivery address. | NOT NULL |
| CarrierId | BIGINT | FK to carrier once assigned. | NULL, FK |
| CreatedAt | DATETIME2 | Timestamp. | DEFAULT GETUTCDATE() |
| UpdatedAt | DATETIME2 | Timestamp. | DEFAULT GETUTCDATE() |

Indexes:  
_IX_Shipment_BatchId - find all shipments in a batch._  
IX_Shipment_ClientId_Status - fetch shipments for client by status.  
_IX_Shipment_CarrierId_Status - for carriers retrieving assigned shipments._  
Partition shipments by CreatedAt or BranchId to support scalability.

#### ShipmentEvent (Tracking)

| Column | Type | Description | Constraints |
| --- | --- | --- | --- |
| Id  | BIGINT | Primary key. | PK, Identity |
| ShipmentId | BIGINT | FK to shipment. | FK  |
| EventType | NVARCHAR(50) | e.g., "InWarehouse", "AtSourcePort", etc. | NOT NULL |
| ActorUserId | BIGINT | FK to user who performed the action (may be system). | FK, NULL (system events). |
| Location | NVARCHAR(100) | Descriptive location (branch/warehouse/port/carrier). |     |
| Message | NVARCHAR(200) | Human‑readable description. | NULL |
| CreatedAt | DATETIME2 | When event occurred. | DEFAULT GETUTCDATE() |

Indexes:  
_IX_ShipmentEvent_ShipmentId_CreatedAt - retrieving timeline of events._  
Partition by CreatedAt for large volumes.

#### Announcement & Targeting

| Table/Column | Type | Description | Constraints |
| --- | --- | --- | --- |
| Announcement.Id | BIGINT | Primary key. | PK, Identity |
| Title | NVARCHAR(100) | Title. | NOT NULL |
| Content | NVARCHAR(MAX) | Message body (rich text). | NOT NULL |
| StartDate | DATETIME2 | When announcement becomes visible. | NOT NULL |
| EndDate | DATETIME2 | Expiration date. | NOT NULL |
| CreatedByUserId | BIGINT | Admin user who created it. | FK  |
| CreatedAt | DATETIME2 | Timestamp. | DEFAULT GETUTCDATE() |
| AnnouncementTarget.Id | BIGINT | Primary key. | PK, Identity |
| AnnouncementId | BIGINT | FK to announcement. | FK  |
| BranchId | BIGINT | Optional branch filter. | NULL, FK |
| ClientId | BIGINT | Optional specific client. | NULL, FK |
| Tag | NVARCHAR(50) | Optional tag (e.g., top‑shipper group). | NULL |

Queries should select announcements where StartDate <= GETUTCDATE() <= EndDate and where the current client matches one of the targets (client‑specific, branch, tag). Indexes on (StartDate, EndDate) and (BranchId, Tag) accelerate filtering.

#### Audit Log (Generic)

The system needs to record significant changes and who performed them. Instead of individual shadow tables for each entity (which are difficult to maintain[\[5\]](https://vertabelo.com/blog/database-design-for-audit-logging/#:~:text=Shadow%20Tables)), the design uses a **generic audit log** composed of two tables[\[4\]](https://vertabelo.com/blog/database-design-for-audit-logging/#:~:text=A%20third%20option%20is%20to,are%20required%20for%20this%20technique):

- **AuditHeader** - captures table name, primary key, operation (INSERT/UPDATE/DELETE), user, and timestamp.
- **AuditDetail** - captures property name, old value and new value for each changed column.

CREATE TABLE AuditHeader (  
Id BIGINT IDENTITY PRIMARY KEY,  
TableName NVARCHAR(128) NOT NULL,  
RowKey NVARCHAR(128) NOT NULL,  
Operation NVARCHAR(20) NOT NULL,  
UserId BIGINT NULL,  
RecordDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),  
ActualDate DATETIME2 NOT NULL  
);  
<br/>CREATE TABLE AuditDetail (  
Id BIGINT IDENTITY PRIMARY KEY,  
AuditHeaderId BIGINT NOT NULL FOREIGN KEY REFERENCES AuditHeader(Id) ON DELETE CASCADE,  
ColumnName NVARCHAR(128) NOT NULL,  
OldValue NVARCHAR(MAX) NULL,  
NewValue NVARCHAR(MAX) NULL  
);

Application code or EF Core interceptors populate these tables on entity changes.

#### Outbox

An OutboxEvent table ensures reliable event publishing. Services insert events when business actions occur; a background worker reads unsent events and publishes them to message brokers. This pattern decouples database transactions from external event delivery, ensuring that events are not lost.

### Example Migration Scripts

Below are simplified CREATE TABLE statements for key entities:

\-- Branch table  
CREATE TABLE Branch (  
Id BIGINT IDENTITY PRIMARY KEY,  
Name NVARCHAR(100) NOT NULL UNIQUE,  
Address NVARCHAR(200) NOT NULL,  
CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()  
);  
<br/>\-- Role and User  
CREATE TABLE Role (  
Id BIGINT IDENTITY PRIMARY KEY,  
Name NVARCHAR(50) NOT NULL UNIQUE  
);  
<br/>CREATE TABLE \[User\] (  
Id BIGINT IDENTITY PRIMARY KEY,  
UserName NVARCHAR(50) NOT NULL UNIQUE,  
PasswordHash VARBINARY(256) NOT NULL,  
DisplayName NVARCHAR(100) NULL,  
IsActive BIT NOT NULL DEFAULT 1,  
CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()  
);  
<br/>CREATE TABLE UserRole (  
UserId BIGINT NOT NULL,  
RoleId BIGINT NOT NULL,  
PRIMARY KEY (UserId, RoleId),  
FOREIGN KEY (UserId) REFERENCES \[User\](Id) ON DELETE CASCADE,  
FOREIGN KEY (RoleId) REFERENCES Role(Id) ON DELETE CASCADE  
);  
<br/>\-- Client (user is also client)  
CREATE TABLE Client (  
Id BIGINT IDENTITY PRIMARY KEY,  
UserId BIGINT NOT NULL UNIQUE,  
PhoneNumber NVARCHAR(20) NULL,  
CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),  
FOREIGN KEY (UserId) REFERENCES \[User\](Id)  
);  
<br/>\-- Batch  
CREATE TABLE Batch (  
Id BIGINT IDENTITY PRIMARY KEY,  
BranchId BIGINT NOT NULL,  
Name NVARCHAR(50) NULL,  
Status TINYINT NOT NULL DEFAULT 0,  
ShipmentCount INT NOT NULL DEFAULT 0,  
TotalWeight DECIMAL(10,2) NOT NULL DEFAULT 0,  
ThresholdCount INT NOT NULL DEFAULT 0,  
ThresholdWeight DECIMAL(10,2) NOT NULL DEFAULT 0,  
WarehouseId BIGINT NULL,  
SourcePortId BIGINT NULL,  
DestinationPortId BIGINT NULL,  
CarrierAssignedAt DATETIME2 NULL,  
CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),  
UpdatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),  
FOREIGN KEY (BranchId) REFERENCES Branch(Id),  
FOREIGN KEY (WarehouseId) REFERENCES Warehouse(Id),  
FOREIGN KEY (SourcePortId) REFERENCES Port(Id),  
FOREIGN KEY (DestinationPortId) REFERENCES Port(Id)  
);  
CREATE INDEX IX_Batch_BranchId_Status ON Batch (BranchId, Status);  
<br/>\-- Shipment  
CREATE TABLE Shipment (  
Id BIGINT IDENTITY PRIMARY KEY,  
ClientId BIGINT NOT NULL,  
BatchId BIGINT NULL,  
Status TINYINT NOT NULL DEFAULT 0,  
Weight DECIMAL(10,2) NOT NULL,  
Volume DECIMAL(10,2) NULL,  
PickupAddress NVARCHAR(200) NULL,  
DeliveryAddress NVARCHAR(200) NOT NULL,  
CarrierId BIGINT NULL,  
CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),  
UpdatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),  
FOREIGN KEY (ClientId) REFERENCES Client(Id),  
FOREIGN KEY (BatchId) REFERENCES Batch(Id),  
FOREIGN KEY (CarrierId) REFERENCES Carrier(Id)  
);  
CREATE INDEX IX_Shipment_BatchId ON Shipment (BatchId);  
CREATE INDEX IX_Shipment_ClientId_Status ON Shipment (ClientId, Status);  
<br/>\-- ShipmentEvent  
CREATE TABLE ShipmentEvent (  
Id BIGINT IDENTITY PRIMARY KEY,  
ShipmentId BIGINT NOT NULL,  
EventType NVARCHAR(50) NOT NULL,  
ActorUserId BIGINT NULL,  
Location NVARCHAR(100) NULL,  
Message NVARCHAR(200) NULL,  
CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),  
FOREIGN KEY (ShipmentId) REFERENCES Shipment(Id),  
FOREIGN KEY (ActorUserId) REFERENCES \[User\](Id)  
);  
CREATE INDEX IX_ShipmentEvent_ShipmentId_CreatedAt ON ShipmentEvent (ShipmentId, CreatedAt);  
<br/>\-- Announcement & Target tables  
CREATE TABLE Announcement (  
Id BIGINT IDENTITY PRIMARY KEY,  
Title NVARCHAR(100) NOT NULL,  
Content NVARCHAR(MAX) NOT NULL,  
StartDate DATETIME2 NOT NULL,  
EndDate DATETIME2 NOT NULL,  
CreatedByUserId BIGINT NOT NULL,  
CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),  
FOREIGN KEY (CreatedByUserId) REFERENCES \[User\](Id)  
);  
<br/>CREATE TABLE AnnouncementTarget (  
Id BIGINT IDENTITY PRIMARY KEY,  
AnnouncementId BIGINT NOT NULL,  
BranchId BIGINT NULL,  
ClientId BIGINT NULL,  
Tag NVARCHAR(50) NULL,  
FOREIGN KEY (AnnouncementId) REFERENCES Announcement(Id),  
FOREIGN KEY (BranchId) REFERENCES Branch(Id),  
FOREIGN KEY (ClientId) REFERENCES Client(Id)  
);  
<br/>\-- OutboxEvent  
CREATE TABLE OutboxEvent (  
Id BIGINT IDENTITY PRIMARY KEY,  
EventType NVARCHAR(100) NOT NULL,  
Payload NVARCHAR(MAX) NOT NULL,  
OccurredAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),  
ProcessedAt DATETIME2 NULL  
);

## API Design (REST)

The API follows standard REST conventions. All responses use camel‑cased JSON with RFC 7807 problem details for errors. Authentication uses **JWT** with **refresh tokens**. Access tokens are short‑lived (e.g., 15 minutes) while refresh tokens are long‑lived and single‑use. The article on building secure APIs with JWT shows how to configure JwtBearer in ASP.NET Core[\[3\]](https://medium.com/@MatinGhanbari/building-a-secure-api-with-asp-net-core-jwt-and-refresh-tokens-03dac37b4055#:~:text=Step%201%3A%20Setting%20Up%20JWT,NET%20Core) and how to generate access and refresh tokens[\[6\]](https://medium.com/@MatinGhanbari/building-a-secure-api-with-asp-net-core-jwt-and-refresh-tokens-03dac37b4055#:~:text=public%20string%20GenerateAccessToken%28IEnumerable,AddMinutes%2815). The Code Maze guide describes the refresh‑token workflow: the client authenticates, receives access/refresh tokens, uses the access token until expiry, then exchanges the refresh token for a new pair[\[7\]](https://code-maze.com/using-refresh-tokens-in-asp-net-core-authentication/#:~:text=Refresh%20token).

### Authentication Endpoints

| Method & Path | Description | Body / Response | Roles |
| --- | --- | --- | --- |
| **POST** /auth/register | Register a new client. Creates User and Client records. | Request: {userName, password, displayName, phoneNumber}. Response: {userId} or error. | Public |
| **POST** /auth/login | Authenticate user and issue tokens. | Request: {userName, password}. Response: {accessToken, refreshToken, expiresIn, roles}. | Public |
| **POST** /auth/refresh | Exchange refresh token for new access and refresh tokens. | Request: {accessToken, refreshToken}. Response: {accessToken, refreshToken}. | Public |
| **POST** /auth/logout | Revoke refresh token. Deletes refresh token record. | Request: {refreshToken}. Response: 204 NoContent. | Authenticated |

_Role‑based authorization_ - Each endpoint is decorated with \[Authorize(Roles="DataEntry")\] etc. Roles correspond to UserRole assignments in the database. Use policy names for fine‑grained checks (e.g., only BranchAdmins can see all batches in their branch).

### Batch Management

| Method & Path | Description | Body / Response Example |
| --- | --- | --- |
| **GET** /batches | List batches. Supports filtering by branch, status and paging. | 200 OK with paged list of batch summaries. |
| **POST** /batches | Create a new batch. Requires branch ID, thresholds. | Request: {branchId, name, thresholdCount, thresholdWeight}. Response: new batch with status Draft. |
| **POST** /batches/{batchId}/shipments/{shipmentId} | Add shipment to batch (changes shipment status to InBatch). | Returns updated batch summary. |
| **POST** /batches/{batchId}/close | Close an open batch. Validates thresholds and sets status to Closed. | Returns batch with status Closed. |
| **POST** /batches/{batchId}/move-to-warehouse | Assign batch to warehouse and change status to InWarehouse. | Requires {warehouseId}. Returns updated batch. |
| **POST** /batches/{batchId}/move-to-source-port | Move batch to source port (status AtSourcePort). | Requires {portId}. |
| **POST** /batches/{batchId}/clear-source-port | Mark customs clearance complete (ClearedSourcePort). | Returns updated batch. |
| **POST** /batches/{batchId}/start-transit | Set status to InTransit. | Returns updated batch. |
| **POST** /batches/{batchId}/arrival | Mark arrival at destination port (ArrivedDestinationPort). | Requires {destinationPortId}. |
| **POST** /batches/{batchId}/assign-carriers | Assign shipments to carriers. Accepts a list of {shipmentId, carrierId} mappings. | Returns updated shipments and batch status. |
| **GET** /batches/{batchId} | Retrieve batch details, including shipment summaries. | 200 OK with details. |
| **DELETE** /batches/{batchId} | Cancel batch (only allowed in Draft or Open state). | 204 NoContent or error. |

### Shipment Management

| Method & Path | Description |
| --- | --- |
| **POST** /shipments | Create new shipment. Request: {clientId, weight, volume, deliveryAddress}. Returns new shipment with status Created. |
| **GET** /shipments/{id} | Get shipment details with current status and tracking events. |
| **PATCH** /shipments/{id}/status | Update shipment status (used by carrier operators). Body: {status, message}. Validates allowed transitions. |
| **GET** /shipments | List shipments with filters by client, status, batch, date range. |
| **DELETE** /shipments/{id} | Cancel shipment (before being batched). |

### Announcement Management

| Method & Path | Description | Roles |
| --- | --- | --- |
| **POST** /announcements | Create announcement. Body: {title, content, startDate, endDate, targets\[\]}. | Admin |
| **GET** /announcements | List announcements visible to the current client (filters by Start/EndDate & targets). | Authenticated |
| **GET** /announcements/{id} | Get announcement details. | Authenticated |
| **PUT** /announcements/{id} | Update announcement. | Admin |
| **DELETE** /announcements/{id} | Delete announcement. | Admin |

### Clients & Users

| Method & Path | Description |
| --- | --- |
| **GET** /clients/me/shipments | Get shipments belonging to the authenticated client. |
| **GET** /clients/me/shipments/{id} | View a specific shipment including events. |
| **GET** /clients/top (Admin only) | Get top N clients per branch (by shipment count) for targeting. |

### DTO Examples (C#)

public record CreateBatchRequest(long BranchId, string Name, int ThresholdCount, decimal ThresholdWeight);  
public record BatchResponse(long Id, string Name, string Status, int ShipmentCount, decimal TotalWeight);  
<br/>public record CreateShipmentRequest(long ClientId, decimal Weight, decimal Volume, string DeliveryAddress);  
public record ShipmentResponse(long Id, string Status, decimal Weight, decimal Volume, string DeliveryAddress, IEnumerable&lt;ShipmentEventDto&gt; Events);  
<br/>public record ShipmentEventDto(DateTime CreatedAt, string EventType, string Location, string Message, string ActorName);  
<br/>public record AnnouncementRequest(string Title, string Content, DateTime StartDate, DateTime EndDate, List&lt;AnnouncementTargetDto&gt; Targets);  
public record AnnouncementTargetDto(long? BranchId, long? ClientId, string? Tag);  
public record AnnouncementResponse(long Id, string Title, string Content, DateTime StartDate, DateTime EndDate);

## Service & Data Flow

### Create & Close Batch

- **Create Batch** - POST /batches. The API validates the branch and thresholds, inserts a Batch with Status=Draft, and returns the new batch ID.
- **Add Shipments** - POST /batches/{batchId}/shipments/{shipmentId}. The API verifies that the batch is in Draft or Open, updates the shipment's BatchId and status to InBatch, increments ShipmentCount and TotalWeight, and writes a ShipmentEvent (e.g., "Added to batch").
- **Close Batch** - POST /batches/{batchId}/close. The API checks that ShipmentCount >= ThresholdCount and/or TotalWeight >= ThresholdWeight. It sets Status=Closed and writes an OutboxEvent (e.g., BatchClosed). The batch can no longer accept shipments.

### Move to Warehouse, Ports, Carriers

- **Move to Warehouse** - POST /batches/{id}/move-to-warehouse. The user provides the warehouse ID. The API updates the batch's WarehouseId and Status=InWarehouse and updates each shipment's status accordingly. A ShipmentEvent is inserted for each shipment. The system may notify the warehouse via an event from the outbox.
- **Move to Source Port** - POST /batches/{id}/move-to-source-port. Similar to above; sets Status=AtSourcePort, updates shipments, and records events.
- **Clear Customs** - POST /batches/{id}/clear-source-port. Sets Status=ClearedSourcePort.
- **Start Transit** - POST /batches/{id}/start-transit. Sets Status=InTransit and updates shipments.
- **Arrival at Destination** - POST /batches/{id}/arrival. The destination port operator sets the destination port; status becomes ArrivedDestinationPort.
- **Assign Carriers** - POST /batches/{id}/assign-carriers. Accepts a list of shipment/carrier mappings and updates each shipment's CarrierId and status WithCarrier. When a carrier receives a shipment, they call PATCH /shipments/{id}/status to mark OutForDelivery and later Delivered.

### Client Tracking View

Clients call GET /clients/me/shipments with optional filters. The API retrieves shipments from the database or a cache. For each shipment, the latest status and the last few events are returned. To optimize performance for read‑heavy tracking, maintain a **materialized view or read model** (e.g., ShipmentStatusView) that stores the latest status and timestamp per shipment, updated by triggers or background jobs. Alternatively, use a **Redis cache** to cache shipment status and events for quick retrieval. According to the Redgate article, caching frequent reads can reduce database load and improve response time[\[8\]](https://www.red-gate.com/simple-talk/databases/sql-server/performance-sql-server/designing-highly-scalable-database-architectures/#:~:text=While%20building%20distributed%20systems%20that,millisecond%20latency).

## Scalability & Performance Considerations

- **Horizontal scaling and read replicas** - For high read traffic, create read‑only replicas of the SQL Server database. The Redgate article suggests adding read replicas to handle read‑heavy workloads[\[2\]](https://www.red-gate.com/simple-talk/databases/sql-server/performance-sql-server/designing-highly-scalable-database-architectures/#:~:text=If%20you%20want%20to%20handle,instances%20by%20implementing%20horizontal%20scaling) and routing queries to replicas[\[9\]](https://www.red-gate.com/simple-talk/databases/sql-server/performance-sql-server/designing-highly-scalable-database-architectures/#:~:text=Database%20Read%20Replicas). Ensure the application is read‑after‑write consistent for client queries by reading from the primary or adding small delays.
- **Caching** - Use an in‑memory cache (Redis) for frequently accessed data such as shipment statuses and announcements. Caching reduces round‑trips to the database[\[8\]](https://www.red-gate.com/simple-talk/databases/sql-server/performance-sql-server/designing-highly-scalable-database-architectures/#:~:text=While%20building%20distributed%20systems%20that,millisecond%20latency). Employ a cache‑aside strategy: check cache first; if missing, fetch from DB and populate cache.
- **Partitioning** - Partition large tables (e.g., Shipment, ShipmentEvent) by CreatedAt or BranchId. Partitioning improves performance and eases archival of old data.
- **CQRS / Read Models** - For the tracking UI, consider a **CQRS** pattern: separate write and read models. Write operations update the core tables and publish events to update a denormalized read model (e.g., ShipmentStatusView stored in SQL or NoSQL), optimized for client queries. This reduces load on the transactional tables.
- **Outbox & Events** - Use the **Outbox pattern** to publish domain events reliably. A background service reads events from the OutboxEvent table and publishes them to external systems (e.g., integration with warehouse/port systems, email notifications). This decouples transaction commits from event delivery and improves resilience.
- **Bulk Operations** - When moving batches, perform updates in SQL set‑based statements rather than per‑row updates to improve performance. Use stored procedures or EF Core bulk updates.
- **Read Isolation** - Use READ COMMITTED SNAPSHOT isolation to reduce locking and improve concurrency on read queries.

## Security & Compliance

- **Authentication & Authorization** - Use **JWT** access tokens and **refresh tokens** as described above. Configure JwtBearer authentication using issuer, audience and signing key[\[3\]](https://medium.com/@MatinGhanbari/building-a-secure-api-with-asp-net-core-jwt-and-refresh-tokens-03dac37b4055#:~:text=Step%201%3A%20Setting%20Up%20JWT,NET%20Core). Implement a token service that generates access tokens (short expiry) and refresh tokens (random 32‑byte strings)[\[6\]](https://medium.com/@MatinGhanbari/building-a-secure-api-with-asp-net-core-jwt-and-refresh-tokens-03dac37b4055#:~:text=public%20string%20GenerateAccessToken%28IEnumerable,AddMinutes%2815). Store refresh tokens in a RefreshToken table with user association, expiry and revocation flags.
- **Password Storage** - Hash passwords using a strong algorithm (e.g., BCrypt or PBKDF2). Do not store plain passwords.
- **Role‑based Access Control** - Assign roles via UserRole table and use \[Authorize(Roles="...")\] attributes on controllers. For complex permissions (e.g., DataEntry can only access their branch), implement authorization handlers.
- **Input Validation** - Validate all request payloads using data annotations or FluentValidation. Protect against SQL injection by using parameterized queries or EF Core.
- **Rate Limiting** - Protect endpoints against brute force and DDoS. Use ASP.NET Core rate‑limiting middleware to restrict requests per IP/user.
- **Transport Security** - Require HTTPS. Use HSTS and TLS 1.2+.
- **Data Encryption at Rest** - Enable SQL Server **Transparent Data Encryption (TDE)** or use cloud database encryption. Encrypt sensitive fields (e.g., refresh tokens) in the database.
- **Secrets Management** - Store JWT signing keys and database connection strings in secure secret management (Azure Key Vault / environment variables). Do not commit secrets to source control.
- **Logging & Observability** - Use structured logging (e.g., Serilog) and integrate with Application Insights or ELK stack. Log requests, responses (without sensitive data), errors and performance metrics. Implement distributed tracing for long workflows.
- **Audit Trail** - Write audit logs for create/update/delete operations to the AuditHeader and AuditDetail tables as described. Include both record date and actual date[\[4\]](https://vertabelo.com/blog/database-design-for-audit-logging/#:~:text=A%20third%20option%20is%20to,are%20required%20for%20this%20technique) for temporal reconstruction. Provide an admin endpoint to query audit logs.

## Announcements & Targeting Model

- **Creating Announcements** - Admins create an announcement with title, content, active date range and a list of targets (branch IDs, specific client IDs or tags such as "top‑client").
- **Target Resolution** - When a client requests announcements, the API filters announcements where current datetime falls within Start/End date and at least one AnnouncementTarget matches the user's branch or client ID or one of the client's tags (e.g., membership group). Use SQL queries with EXISTS subqueries for efficient lookup.
- **Top Clients** - Provide an endpoint GET /clients/top?branchId=&topN= that returns clients sorted by total shipment count or volume. The admin can then target announcements to those clients by specifying their IDs or using a "top‑client" tag.
- **Caching** - Cache active announcements in Redis keyed by branch or client group to avoid recomputing filters for every request.

## Example User Stories & Acceptance Tests

### 1\. DataEntry Creates Shipment and Attaches to Batch

**As a DataEntry user** I can create a shipment and attach it to a batch so that the shipment appears in the batch list and has status InBatch.

_Test data & steps:_ 1. Authenticate as a DataEntry user for branch A.  
2\. POST /shipments with {clientId: 1, weight: 5, volume: 1, deliveryAddress: "Client Address"} → returns shipment with status Created.  
3\. POST /batches with {branchId: 1, thresholdCount: 2, thresholdWeight: 20} → returns batch in Draft status.  
4\. POST /batches/{batchId}/shipments/{shipmentId} → response shows shipment added and batch status Open.  
5\. GET /batches/{batchId} shows the shipment in the list with status InBatch.

_Acceptance:_ Shipment's BatchId equals the batch ID; shipment's Status is InBatch. Batch's ShipmentCount equals 1.

### 2\. DataEntry Closes Batch When Threshold Reached

**As a DataEntry user** I can close the batch when the threshold is reached.  
_Steps:_ Create two shipments as above, add them to the batch; call POST /batches/{batchId}/close.  
_Acceptance:_ Response has batch Status=Closed; ShipmentCount >= ThresholdCount. A ShipmentEvent and OutboxEvent are recorded.

### 3\. WarehouseOperator Processes Batch

**As a WarehouseOperator** I can mark a batch processed and move it to the source port.  
_Steps:_ Authenticate as WarehouseOperator, call POST /batches/{id}/move-to-warehouse specifying the warehouse. Then call POST /batches/{id}/move-to-source-port with source port ID.  
_Acceptance:_ Batch statuses transition to InWarehouse then AtSourcePort; each shipment's status updates accordingly; events are recorded. Only warehouse operators are authorized.

### 4\. Client Views Shipments and Timeline

**As a Client** I can log in and view all shipments I created with a timeline of events for each shipment.  
_Steps:_ Client authenticates via /auth/login, calls GET /clients/me/shipments, selects a shipment, then calls GET /shipments/{id}.  
_Acceptance:_ Response includes a chronological list of events with actor names and locations. The latest status matches the shipment table.

### 5\. Announcement Targeting to Top Clients

**As an Admin** I can create an announcement targeted to the top 10 clients of a branch.  
_Steps:_ Call GET /clients/top?branchId=1&topN=10 to retrieve top clients. Use their IDs to create an announcement via POST /announcements with the target list. Log in as one of these clients and call GET /announcements - ensure the announcement is present. Log in as a client not in the top list - the announcement should not appear.  
_Acceptance:_ Only specified clients see the announcement.

## Conclusion

This design provides a clear roadmap for implementing a shipment‑tracking backend using ASP.NET Web API and SQL Server. It balances simplicity for a junior developer with scalability and robustness. Clean architecture separates concerns[\[1\]](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures#:~:text=Clean%20architecture), while horizontal scaling, caching and the outbox pattern prepare the system for growth[\[2\]](https://www.red-gate.com/simple-talk/databases/sql-server/performance-sql-server/designing-highly-scalable-database-architectures/#:~:text=If%20you%20want%20to%20handle,instances%20by%20implementing%20horizontal%20scaling)[\[8\]](https://www.red-gate.com/simple-talk/databases/sql-server/performance-sql-server/designing-highly-scalable-database-architectures/#:~:text=While%20building%20distributed%20systems%20that,millisecond%20latency). The defined workflows, state machines, database schema, API endpoints and acceptance tests give the development team all they need to build a production‑ready system.
