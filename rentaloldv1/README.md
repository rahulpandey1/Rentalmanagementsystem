# Boarding House Management System

A comprehensive web application for managing a 22-room boarding house with advanced billing features, electric meter tracking, and payment management.

## ?? Current Status: Ready for Database Deployment

**Entity Framework migrations have been created and are ready to deploy to Azure SQL Database.**

## ?? Quick Start

### Step 1: Configure Database Connection
1. Open `appsettings.json`
2. Replace `{your_password}` with your actual Azure SQL password

### Step 2: Deploy Database
```powershell
.\Scripts\Quick-Setup.ps1
```
OR
```bash
dotnet ef database update
```

### Step 3: Run Application
```bash
dotnet run
```

### Step 4: Access System
- **Dashboard**: https://localhost:5001
- **API Documentation**: https://localhost:5001/swagger

## ?? System Overview

### Room Management
- **22 Rooms** across 3 floors:
  - Ground Floor: G/1 to G/10 (10 rooms)
  - First Floor: 1/1 to 1/6 (6 rooms) 
  - Second Floor: 2/1 to 2/6 (6 rooms)
- Individual room tracking with availability status
- Room-specific electric meter management
- Floor-wise visualization

### Advanced Billing System
- **Rent Billing**: Monthly rent charges per room
- **Electric Billing**: Meter-based electric consumption calculation (?12 per unit)
- **Miscellaneous Charges**: Additional fees and charges with remarks
- **Advance Payments**: Track advance payments from tenants
- Comprehensive bill generation with automatic calculations

### Electric Meter Management
- Individual electric meters for each room
- Automatic calculation of units consumed
- Configurable per-unit cost (default: ?12)
- Historical reading tracking

### Payment Tracking
- Multiple payment types: Rent, Electric, Advance, Miscellaneous
- Payment status tracking (Pending, Paid, Partial, Overdue)
- Outstanding amount calculations
- Payment history per tenant

### Reporting & Analytics
- Dashboard with key metrics
- Outstanding bills summary
- Tenant-wise financial breakdown
- Floor occupancy statistics
- Revenue tracking

## ??? Technology Stack

- **Framework**: ASP.NET Core 8.0
- **Database**: Azure SQL Database with Entity Framework Core
- **Frontend**: Bootstrap 5, Font Awesome icons
- **API**: RESTful Web API with Swagger documentation

## ??? Database Configuration

### Azure PostgreSQL Database Details
- **Host**: rahulpersonal.postgres.database.azure.com
- **Database**: Rental
- **Username**: mtsuser
- **Authentication**: Password

### Migration Status
? Migration files created and ready to deploy
- Initial migration: `20250811065606_InitialCreate`
- SQL script: `Scripts/InitialCreate.sql`
- Snapshot: Current model captured

## ?? Database Schema

### Core Tables
- **Properties** - Main property information
- **Rooms** - 22 rooms with numbering G/1-G/10, 1/1-1/6, 2/1-2/6
- **Tenants** - Tenant information and contact details
- **RentAgreements** - Rental contracts linking tenants to rooms

### Billing Tables
- **Bills** - Comprehensive billing (rent + electric + misc)
- **BillItems** - Individual line items within bills
- **ElectricMeterReadings** - Electric meter readings and consumption
- **Payments** - Payment records with type classification

### Configuration Tables
- **SystemConfigurations** - System settings (electric rates, etc.)

## ?? API Endpoints

### Rooms Management
```
GET    /api/rooms                    - List all rooms
GET    /api/rooms/{id}               - Get room details
GET    /api/rooms/available          - List available rooms
GET    /api/rooms/floor/{floorNumber} - Rooms by floor
PUT    /api/rooms/{id}/availability  - Update room availability
```

### Billing System
```
GET    /api/bills                    - List all bills
GET    /api/bills/outstanding        - Outstanding bills
GET    /api/bills/summary           - Financial summary
POST   /api/bills/generate/{rentAgreementId} - Generate new bill
```

### Electric Meter Management
```
GET    /api/electricmeterreadings    - List all readings
POST   /api/electricmeterreadings    - Record new reading
GET    /api/electricmeterreadings/room/{roomId}/latest - Latest reading
```

### Payment Tracking
```
GET    /api/payments                 - List all payments
GET    /api/payments/tenant/{tenantId} - Payments by tenant
GET    /api/payments/overdue         - Overdue payments
PUT    /api/payments/{id}/markpaid   - Mark payment as paid
```

## ?? Configuration Settings

### Application Configuration (appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:rahulpandey.database.windows.net,1433;Initial Catalog=Rental;..."
  },
  "BillingSettings": {
    "ElectricUnitCost": 12.00,
    "BillDueDays": 15,
    "PropertyName": "Your Boarding House"
  }
}
```

### System Configuration (Database)
- Electric unit cost: ?12.00 per unit (configurable)
- Bill due period: 15 days (configurable)
- Property-specific settings

## ?? Pre-Configured Data

After database deployment, the system includes:

### Property Setup
- 1 Boarding house property with complete configuration
- 22 rooms across 3 floors with proper numbering
- Electric meter assignments for each room

### Room Configuration
- **Ground Floor**: G/1, G/2, G/3, ..., G/10 (10 rooms)
- **First Floor**: 1/1, 1/2, 1/3, ..., 1/6 (6 rooms)
- **Second Floor**: 2/1, 2/2, 2/3, ..., 2/6 (6 rooms)

### Electric Meters
- Ground floor: GM001 - GM010
- First floor: F1M001 - F1M006
- Second floor: F2M001 - F2M006

## ?? Dashboard Features

### Main Dashboard
- Room occupancy statistics by floor
- Financial summary (outstanding amounts, rent due, electric due)
- Advance payment tracking
- Quick action buttons for common tasks

### Billing Summary
- Outstanding amounts by category
- Tenant-wise outstanding breakdown
- Recent bills listing
- Bulk operations capabilities

### Floor View
- Visual room layout by floor
- Real-time occupancy status
- Current tenant information
- Room-wise statistics

## ?? Development Setup

### Prerequisites
- .NET 8 SDK
- Azure SQL Database access
- Entity Framework Core Tools

### Setup Steps
1. Clone the repository
2. Configure connection string in `appsettings.json`
3. Run database migration: `dotnet ef database update`
4. Start application: `dotnet run`

### Development Commands
```bash
# Check migration status
dotnet ef migrations list

# Create new migration
dotnet ef migrations add [MigrationName]

# Update database
dotnet ef database update

# Generate SQL script
dotnet ef migrations script
```

## ?? Troubleshooting

### Common Issues

1. **Database Connection Failed**
   - Verify password in appsettings.json
   - Check Azure SQL firewall settings
   - Ensure database exists

2. **Migration Errors**
   - Verify Entity Framework tools are installed
   - Check for model validation errors
   - Review connection string format

3. **IP Access Issues**
   - Add your IP to Azure SQL firewall
   - Check network connectivity

### Verification Steps
1. Test connection: `dotnet ef dbcontext info`
2. List migrations: `dotnet ef migrations list`
3. Check application: Navigate to https://localhost:5001

## ?? Usage Workflow

### Setting Up Tenants
1. Access dashboard at https://localhost:5001
2. Add tenant information via API or future web interface
3. Create rent agreements and assign rooms
4. Configure electric meter initial readings

### Monthly Operations
1. Record electric meter readings for each room
2. Generate monthly bills (rent + electric + misc charges)
3. Track payments and outstanding amounts
4. Monitor occupancy and revenue

### Billing Process
1. Record monthly electric meter readings
2. System calculates consumption (current - previous reading)
3. Apply electric rate (?12 per unit)
4. Add miscellaneous charges as needed
5. Generate comprehensive bills
6. Track payments and outstanding balances

## ?? Future Enhancements

- Mobile-responsive tenant portal
- Automated bill generation scheduling
- SMS/Email notifications
- Expense tracking and profit/loss reports
- Multi-property management
- Online payment integration
- Tenant mobile app

## ?? Support

- **API Documentation**: https://localhost:5001/swagger
- **Database Setup Guide**: See `DATABASE_SETUP.md`
- **Migration Scripts**: Available in `Scripts/` folder

## ?? License

This project is designed for boarding house management and can be customized for specific requirements.

---

**Ready to start managing your 22-room boarding house with advanced billing capabilities!** ????