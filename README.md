# Shipment Tracker Backend

A comprehensive shipment tracking system built with ASP.NET Core Web API, following clean architecture principles. This system allows branch staff, warehouse operators, port operators, carriers, clients, and administrators to track batches and shipments throughout their lifecycle.

## Features

### Authentication & Authorization
- **JWT-based authentication** with access and refresh tokens
- **Email verification** for new user registrations
- **Password reset** functionality via email
- **Role-based access control** (DataEntry, BranchAdmin, WarehouseOperator, PortOperator, CarrierOperator, Client, Admin)
- **Secure password hashing** using BCrypt

### Core Functionality
- **Batch Management**: Create, close, and track batches through their lifecycle
- **Shipment Tracking**: Real-time shipment status updates and event history
- **Multi-role Support**: Different user types with appropriate permissions
- **Announcement System**: Targeted announcements to specific user groups
- **Audit Logging**: Complete audit trail for all system changes

### Technical Features
- **Clean Architecture**: Separation of concerns with Core, Infrastructure, and API layers
- **Repository Pattern**: Generic and specific repositories for data access
- **Unit of Work**: Transaction management and data consistency
- **Email Service**: SMTP-based email notifications
- **Outbox Pattern**: Reliable event publishing for external integrations
- **SQL Server**: Robust database with proper indexing and relationships

## Project Structure

```
ShipmentTracker/
├── ShipmentTracker.Core/           # Domain layer
│   ├── Entities/                   # Domain entities
│   ├── Enums/                      # Domain enums
│   └── Interfaces/                 # Repository and service interfaces
├── ShipmentTracker.Infrastructure/ # Data access layer
│   ├── Data/                       # DbContext and configurations
│   ├── Repositories/               # Repository implementations
│   ├── Services/                   # Infrastructure services
│   └── SQL/                        # Database migration scripts
└── ShipmentTracker.API/            # Presentation layer
    ├── Controllers/                # API controllers
    ├── DTOs/                       # Data transfer objects
    └── Program.cs                  # Application configuration
```

## Prerequisites

- .NET 8.0 SDK
- SQL Server (LocalDB, Express, or Full)
- SMTP email service (Gmail, SendGrid, etc.)

## Setup Instructions

### 1. Clone and Build

```bash
git clone <repository-url>
cd ShipmentTracker
dotnet restore
dotnet build
```

### 2. Database Setup

#### Option A: Using SQL Scripts (Recommended)
1. Create a new database named `ShipmentTrackerDb` in SQL Server
2. Execute the SQL scripts in order:
   ```sql
   -- Execute these scripts in order:
   ShipmentTracker.Infrastructure/SQL/01_CreateTables.sql
   ShipmentTracker.Infrastructure/SQL/02_CreateIndexes.sql
   ```

#### Option B: Using Entity Framework
The application will automatically create the database on first run using `EnsureCreated()`.

### 3. Configuration

Update `appsettings.json` with your settings:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ShipmentTrackerDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "Jwt": {
    "Secret": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "ShipmentTracker",
    "Audience": "ShipmentTrackerUsers",
    "ExpiryMinutes": 15
  },
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-app-password",
    "FromEmail": "noreply@shipmenttracker.com",
    "FromName": "Shipment Tracker"
  },
  "App": {
    "BaseUrl": "https://localhost:7000"
  }
}
```

### 4. Email Configuration

For Gmail:
1. Enable 2-factor authentication
2. Generate an App Password
3. Use the App Password in `SmtpPassword`

For other providers, update the SMTP settings accordingly.

### 5. Run the Application

```bash
cd ShipmentTracker.API
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:7000`
- Swagger UI: `https://localhost:7000/swagger`

## API Endpoints

### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/verify-email` - Verify email address
- `POST /api/auth/login` - User login
- `POST /api/auth/refresh` - Refresh access token
- `POST /api/auth/logout` - User logout
- `POST /api/auth/forgot-password` - Request password reset
- `POST /api/auth/reset-password` - Reset password
- `POST /api/auth/resend-verification` - Resend verification email

### Batch Management
- `GET /api/batches` - List batches
- `POST /api/batches` - Create new batch
- `GET /api/batches/{id}` - Get batch details
- `POST /api/batches/{id}/close` - Close batch
- `POST /api/batches/{id}/move-to-warehouse` - Move to warehouse
- `POST /api/batches/{id}/move-to-source-port` - Move to source port
- `POST /api/batches/{id}/clear-source-port` - Clear customs
- `POST /api/batches/{id}/start-transit` - Start transit
- `POST /api/batches/{id}/arrival` - Mark arrival
- `POST /api/batches/{id}/assign-carriers` - Assign carriers

### Shipment Management
- `GET /api/shipments` - List shipments
- `POST /api/shipments` - Create shipment
- `GET /api/shipments/{id}` - Get shipment details
- `PATCH /api/shipments/{id}/status` - Update shipment status

### Announcements
- `GET /api/announcements` - Get user announcements
- `POST /api/announcements` - Create announcement (Admin)
- `PUT /api/announcements/{id}` - Update announcement (Admin)
- `DELETE /api/announcements/{id}` - Delete announcement (Admin)

## User Roles

1. **DataEntry**: Create shipments and batches at branch level
2. **BranchAdmin**: Manage branch operations and view branch data
3. **WarehouseOperator**: Process batches in warehouses
4. **PortOperator**: Handle port operations and customs clearance
5. **CarrierOperator**: Manage delivery operations
6. **Client**: View their own shipments and announcements
7. **Admin**: Full system access and user management

## Authentication Flow

### Registration
1. User registers with username, email, password
2. System creates inactive user account
3. Email verification token is generated and sent
4. User clicks verification link
5. Account is activated and welcome email is sent

### Login
1. User provides username and password
2. System validates credentials and account status
3. JWT access token (15 min) and refresh token (7 days) are generated
4. Tokens are returned to client

### Password Reset
1. User requests password reset with email
2. Reset token is generated and sent via email
3. User clicks reset link and provides new password
4. Password is updated and token is invalidated

## Database Schema

The system uses a comprehensive database schema with the following key entities:

- **Users & Authentication**: User, Role, UserRole, RefreshToken, EmailVerificationToken, PasswordResetToken
- **Core Business**: Client, Branch, Warehouse, Port, Carrier
- **Shipment Tracking**: Batch, Shipment, ShipmentEvent
- **Communication**: Announcement, AnnouncementTarget
- **Audit & Events**: AuditHeader, AuditDetail, OutboxEvent

## Security Features

- **JWT Authentication**: Secure token-based authentication
- **Password Hashing**: BCrypt for secure password storage
- **Email Verification**: Required for account activation
- **Role-based Authorization**: Fine-grained access control
- **Audit Logging**: Complete change tracking
- **HTTPS**: Secure communication
- **CORS**: Configurable cross-origin policies

## Development

### Adding New Features
1. Define entities in `ShipmentTracker.Core/Entities`
2. Create interfaces in `ShipmentTracker.Core/Interfaces`
3. Implement repositories in `ShipmentTracker.Infrastructure/Repositories`
4. Add services in `ShipmentTracker.Infrastructure/Services`
5. Create controllers in `ShipmentTracker.API/Controllers`
6. Define DTOs in `ShipmentTracker.API/DTOs`

### Testing
```bash
dotnet test
```

### Database Migrations
```bash
# Add migration
dotnet ef migrations add MigrationName --project ShipmentTracker.Infrastructure --startup-project ShipmentTracker.API

# Update database
dotnet ef database update --project ShipmentTracker.Infrastructure --startup-project ShipmentTracker.API
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

## License

This project is licensed under the Zakaria licence

## Support

For support and questions, please contact the development team or create an issue in the repository.
