# Database Setup Instructions - Azure SQL Database

## ? Migration Status: READY TO DEPLOY

**Good news!** The Entity Framework migration has already been created and is ready to deploy to your Azure SQL Database.

## Prerequisites

1. **Azure SQL Database Access**
   - Server: `rahulpandey.database.windows.net`
   - Database: `Rental`
   - Username: `rahuladmin`
   - Password: `{your_password}` (replace with actual password)

2. **Required Tools** ? Already Installed
   - .NET 8 SDK
   - Entity Framework Core Tools

## ?? Quick Setup (Recommended)

### Option 1: Use Quick Setup Script

1. **Update your password** in `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=tcp:rahulpandey.database.windows.net,1433;Initial Catalog=Rental;Persist Security Info=False;User ID=rahuladmin;Password=YOUR_ACTUAL_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
     }
   }
   ```

2. **Run the quick setup script**:
   ```powershell
   .\Scripts\Quick-Setup.ps1
   ```
   OR
   ```batch
   .\Scripts\Quick-Setup.bat
   ```

### Option 2: Manual Command

1. Update password in `appsettings.json` (as above)
2. Run: `dotnet ef database update`

## ?? Advanced Setup

For more control and detailed logging, use the comprehensive setup script:

```powershell
.\Scripts\Setup-Database.ps1
```

This script provides:
- Step-by-step progress tracking
- Error handling and troubleshooting
- Connection testing
- Automatic password configuration

## ?? Pre-Migration Checklist

Before running the migration, ensure:

- [ ] **Database exists**: The `Rental` database must exist in Azure SQL
- [ ] **Firewall configured**: Your IP address is whitelisted
- [ ] **Password updated**: Correct password in `appsettings.json`
- [ ] **Server accessible**: Azure SQL server is running and reachable

## ??? What Gets Created

The migration will create the following database structure:

### Core Tables
- **Properties** - Main property information (1 boarding house)
- **Rooms** - 22 rooms with proper numbering (G/1-G/10, 1/1-1/6, 2/1-2/6)
- **Tenants** - Tenant information and contact details
- **RentAgreements** - Rental contracts linking tenants to rooms

### Billing Tables
- **Bills** - Comprehensive billing (rent + electric + misc)
- **BillItems** - Individual line items within bills
- **ElectricMeterReadings** - Electric meter readings and consumption
- **Payments** - Payment records with type classification

### Configuration Tables
- **SystemConfigurations** - System settings (electric rates, etc.)

## ?? Pre-Configured Data

The system will be populated with:

### Property Setup
- 1 Boarding house property
- 22 rooms across 3 floors
- Electric meter assignments for each room

### Room Layout
```
Ground Floor: G/1, G/2, G/3, G/4, G/5, G/6, G/7, G/8, G/9, G/10
First Floor:  1/1, 1/2, 1/3, 1/4, 1/5, 1/6
Second Floor: 2/1, 2/2, 2/3, 2/4, 2/5, 2/6
```

### System Configuration
- Electric unit cost: ?12.00 per unit
- Bill due days: 15 days
- Property name: "Your Boarding House"

### Electric Meters
Each room has a pre-assigned electric meter:
- Ground floor: GM001, GM002, ... GM010
- First floor: F1M001, F1M002, ... F1M006
- Second floor: F2M001, F2M002, ... F2M006

## ?? Verification Steps

After successful migration:

1. **Start the application**:
   ```bash
   dotnet run
   ```

2. **Access the dashboard**: https://localhost:5001

3. **Verify room setup**: Check that all 22 rooms are displayed

4. **Test API**: Visit https://localhost:5001/swagger

## ? Troubleshooting

### Common Issues

1. **"Database 'Rental' does not exist"**
   ```sql
   -- Connect to Azure SQL and run:
   CREATE DATABASE [Rental];
   ```

2. **Connection timeout/failed**
   - Verify password in `appsettings.json`
   - Check Azure SQL firewall settings
   - Confirm server name: `rahulpandey.database.windows.net`

3. **IP not whitelisted**
   - Go to Azure Portal ? SQL Server ? Networking
   - Add your current IP address

4. **Permission denied**
   - Verify username: `rahuladmin`
   - Check if user has db_owner permissions

### Verification Commands

```bash
# Check migration status
dotnet ef migrations list

# Test database connection
dotnet ef dbcontext info

# View generated SQL (without applying)
dotnet ef migrations script
```

## ?? Generated Files

The migration process has created:

- `Migrations/20250811065606_InitialCreate.cs` - Migration code
- `Migrations/20250811065606_InitialCreate.Designer.cs` - Migration metadata
- `Migrations/RentManagementContextModelSnapshot.cs` - Current model snapshot
- `Scripts/InitialCreate.sql` - SQL script for manual execution

## ?? Next Steps

After successful database setup:

### 1. Start the Application
```bash
dotnet run
```

### 2. Access the System
- **Dashboard**: https://localhost:5001
- **API Docs**: https://localhost:5001/swagger

### 3. Begin Using
- Add tenants through the API
- Create rent agreements
- Record electric meter readings
- Generate bills

### 4. Customize Settings
- Update property name and address
- Adjust electric unit costs
- Configure bill due periods

## ?? Development Workflow

For future changes:

1. **Modify models** in the `Models/` folder
2. **Create new migration**: `dotnet ef migrations add [MigrationName]`
3. **Apply to database**: `dotnet ef database update`

## ?? Important Notes

- **Backup**: Always backup your database before applying migrations in production
- **Testing**: Test migrations in a development environment first
- **Security**: Never commit actual passwords to source control
- **Monitoring**: Set up Azure SQL monitoring and alerts for production use

## ?? Support

If you encounter issues:

1. Check the application logs
2. Review Azure SQL firewall settings
3. Verify connection string format
4. Test with Azure SQL Management Studio

For additional help, check the comprehensive API documentation at `/swagger` after starting the application.