# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DO Inventory Manager is a WPF (.NET 9.0) application for managing diesel oil inventory for maritime operations. It tracks fuel purchases, consumption by vessels, and uses a FIFO allocation system to calculate costs and manage inventory valuation.

## Core Architecture

### Technology Stack
- **.NET 9.0** with WPF for the UI framework
- **Entity Framework Core 8.0** with SQLite for data persistence
- **EPPlus 8.0** for Excel report generation

### Key Domain Models
- **Purchase**: Fuel purchases from suppliers with FIFO tracking via `RemainingQuantity`
- **Consumption**: Monthly fuel consumption records by vessel
- **Allocation**: FIFO-based linking of purchases to consumptions for cost tracking
- **Vessel**: Ships that consume fuel
- **Supplier**: Fuel suppliers with currency support (USD/JOD)

### Database Architecture
- SQLite database stored at `AppDomain.CurrentDomain.BaseDirectory/DOInventory.db`
- Entity Framework migrations in `/Migrations` folder
- Context class: `Data/InventoryContext.cs` with pre-seeded suppliers

### Application Structure
- **MainWindow**: Navigation hub with status bar and alert system
- **Views/**: XAML views for each functional area (Purchases, Consumption, Vessels, etc.)
- **Services/**: Business logic services including FIFO allocation, reporting, and backup
- **Models/**: Entity models with computed properties and navigation relationships

## Key Business Logic

### FIFO Allocation System
The core business logic is in `Services/FIFOAllocationService.cs`. This service:
- Processes consumption records chronologically by month
- Allocates oldest purchases first (FIFO) to consumption records
- Tracks remaining quantities on purchases for proper inventory valuation
- Handles multi-currency calculations (original currency + USD conversion)

### Payment Due Date Alerts
- Automated alert system checks purchase due dates on application startup
- Critical alerts (overdue/due today) show popup notifications
- Status bar displays alert summaries with color coding

### Multi-Currency Support
- Suppliers have base currency (USD/JOD) with exchange rates
- All financial calculations maintain both original currency and USD values
- Exchange rates stored in supplier records for consistency

## Development Commands

### Build and Run
```bash
# Build the solution
dotnet build

# Build in Release configuration
dotnet build -c Release

# Run the application
dotnet run

# Clean build artifacts
dotnet clean
```

### Database Management
```bash
# Add new migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Drop database (reset)
dotnet ef database drop
```

### No Test Suite
This project does not currently have automated tests. Manual testing is performed through the WPF interface.

## Project Structure Notes

### Services Layer
- `FIFOAllocationService`: Core inventory allocation logic
- `ReportService`: Excel report generation for various business scenarios
- `BackupService`: Database backup functionality with versioning
- `AlertService`: Due date monitoring and notification system
- `PrintService`: Report printing with custom print views

### Views Architecture
- Each major functional area has its own View (Purchases, Consumption, etc.)
- Print views in `Views/Print/` folder for formatted report output
- Navigation managed through MainWindow with frame-based content loading

### Data Integrity
- Entity relationships enforced through EF Core foreign keys
- FIFO allocation maintains referential integrity between purchases and consumption
- Computed properties handle currency conversions and density calculations

### Purchase Model Calculations
The Purchase model uses a specific calculation approach where users provide base data and the system calculates derived values:

**Stored Fields (User Input Required):**
- `QuantityLiters` - Fuel quantity in liters (decimal(18,3))
- `QuantityTons` - Fuel quantity in tons (decimal(18,3))
- `TotalValue` - Total cost in supplier's currency (decimal(18,2))
- `TotalValueUSD` - Total cost converted to USD (decimal(18,2))

**Calculated Properties (Auto-computed):**
- `PricePerLiter = TotalValue ÷ QuantityLiters` (decimal(18,6))
- `PricePerTon = TotalValue ÷ QuantityTons` (decimal(18,2))
- `Density = QuantityTons ÷ (QuantityLiters ÷ 1000)` (decimal(8,6))
- `PricePerLiterUSD = TotalValueUSD ÷ QuantityLiters` (decimal(18,6))
- `PricePerTonUSD = TotalValueUSD ÷ QuantityTons` (decimal(18,2))

**FIFO Tracking:**
- `RemainingQuantity` - Set to QuantityLiters initially, decremented during allocation

## Important Conventions

### Decimal Precision
- Quantities: `decimal(18,3)` for liter/ton measurements
- Monetary values: `decimal(18,2)` for currency amounts
- Rates/ratios: `decimal(18,6)` for precise calculations

### Date Handling
The application uses a dual date format approach:

**Display Format (User Interface):**
- DD-MM-YYYY format (e.g., "19/01/2025") for all user-facing dates
- Used in reports, forms, alerts, and data grids
- Consistent across all views and printed reports

**Internal Storage Format:**
- YYYY-MM-DD format for database operations and file operations
- YYYY-MM format for monthly consumption grouping and FIFO processing
- Backup filenames use YYYY-MM-DD_HH-mm-ss for chronological sorting
- Enables proper date sorting and chronological processing

**Key Usage:**
- Purchase dates for FIFO ordering (stored internally, displayed as DD-MM-YYYY)
- Monthly consumption periods (stored as YYYY-MM, displayed as "MMM YYYY")
- Due date tracking for payment alerts (displayed as DD-MM-YYYY)

### Currency Conventions
- Always maintain both original currency and USD equivalent
- Exchange rates stored at supplier level for consistency
- USD used as base currency for reporting and comparisons