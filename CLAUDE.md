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
- **Consumption**: Monthly fuel consumption records by vessel with optional trip tracking (`LegsCompleted` nullable for stationary operations)
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

### Bulk Data Management System
The application includes comprehensive bulk data management capabilities:
- **Advanced Filtering**: Multi-criteria filtering for Purchases (date range, vessel, supplier, invoice) and Consumptions (month, vessel)
- **Selective Deletion**: Checkbox-based selection system for precise bulk operations
- **Safety Validation**: Pre-deletion checks for FIFO allocations and data dependencies
- **Automatic Backups**: Creates backup before any bulk deletion operation
- **Transaction Safety**: All-or-nothing approach ensures data consistency
- **Detailed Reporting**: Comprehensive success/failure feedback with error details

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
- `ExcelExportService`: Comprehensive Excel export functionality for all reports and data views
- `BackupService`: Database backup functionality with versioning
- `BulkDataService`: Bulk data operations, filtering, and selective deletion
- `AlertService`: Due date monitoring and notification system
- `PrintService`: Report printing with custom print views

### Views Architecture
- Each major functional area has its own View (Purchases, Consumption, etc.)
- Print views in `Views/Print/` folder for formatted report output
- Navigation managed through MainWindow with frame-based content loading
- **BackupManagementView**: Tabbed interface with Backup Management and History functionality
  - History Tab: Selective bulk deletion with advanced filtering for Purchases and Consumptions
  - Checkbox-based selection system with "Select All" functionality
  - Safety features: automatic backups, validation, and detailed confirmation dialogs
- **PurchasesView**: Enhanced with comprehensive search and filtering capabilities
  - Advanced filter panel: date range, vessel, supplier, and invoice reference filters
  - Quick filter buttons: Last 30 Days, Last 6 Months, This Year for common searches
  - Smart data loading: default view shows recent 50 purchases, filtered view searches entire database
  - Real-time result counter showing exact match counts

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

## Print System Implementation Status

### ✅ Completed Features
- **Print Buttons Added**: All major report tabs now have 🖨️ Print Report buttons
- **Export & Print Integration**: Complete dual functionality (Excel + Print) for all reports
- **Print Handlers Implemented**: Comprehensive print functionality for:
  - Monthly Summary (already working)
  - Vessel Account Statement (already working)
  - Supplier Account Report ✅ NEW
  - Payment Due Report ✅ NEW
  - Inventory Valuation ✅ NEW
  - Fleet Efficiency ✅ NEW
  - FIFO Allocation Detail ✅ NEW
  - Cost Analysis ✅ NEW
  - Route Performance ✅ NEW

### ✅ Print Layout System
- **GenericReportPrint**: Flexible print layout system for all report types
- **Star-based Column Sizing**: Columns now auto-fit to page width using proportional sizing (1.5*, 0.6*, etc.)
- **Compact Layout Option**: `AddCompactDataGrid()` method for reports with many columns
- **Smart Column Selection**: Automatically prioritizes essential columns for printing
- **Header Abbreviation**: Shortens long headers ("Quantity" → "Qty") for better fit

### ⚠️ CURRENT PRINT ISSUES (Critical - Resume Work Here)

#### **Issue 1: Summary Cards Layout Problem**
- **Problem**: Metadata/summary cards (Total Inventory, Total Weight, FIFO Value, Purchase Lots, Avg Cost/L) are displaying VERTICALLY instead of horizontally
- **Impact**: Vertical layout is pushing content downward, consuming excessive vertical space
- **Root Cause**: `AddSummaryCard()` method in GenericReportPrint.xaml.cs creates vertical StackPanel instead of horizontal layout
- **Expected**: Cards should display in horizontal row like working MonthlySummaryPrint
- **Location**: `Views/Print/GenericReportPrint.xaml.cs` lines 19-69

#### **Issue 2: Footer Overflow Still Occurring**
- **Problem**: Despite MaxHeight constraints (150px for DataGrids, 480px for main content), data still bleeds into footer section
- **Root Cause**: Vertical summary cards layout + content height is exceeding the allocated print page space
- **Expected**: Content must NEVER enter footer space regardless of data volume
- **Current Constraints**: 
  - Normal DataGrids: `MaxHeight = 150px`
  - Compact DataGrids: `MaxHeight = 120px`
  - Main content: `MaxHeight = 480px`
- **Status**: Constraints are insufficient; vertical metadata is the primary culprit

#### **Next Steps for Tomorrow**
1. **Fix AddSummaryCard() method**: Change from vertical to horizontal layout like MonthlySummaryPrint
2. **Analyze MonthlySummaryPrint card layout**: Study working 4-column horizontal card system
3. **Implement proper height mathematics**: Calculate actual available space after header/title/cards
4. **Test all report types**: Ensure no report can exceed footer boundary
5. **Consider ScrollViewer solution**: If content truly exceeds space, implement proper scrolling within bounds

### 🔧 Build Process Enhancement
- **Build Script Created**: `build.bat` automatically terminates running application before building
- **Usage**: Run `build.bat` to avoid file locking issues during development

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

### Consumption Model Handling
The Consumption model supports flexible fuel consumption tracking for maritime operations:

**LegsCompleted Field:**
- `LegsCompleted` is nullable (`int?`) to accommodate different operational scenarios
- **Null/0 Values**: Represent stationary consumption (engines running without vessel movement)
- **Positive Values**: Represent actual vessel trips with movement
- **UI Display**: Shows "Stationary Operation" or "No Movement" when legs is null/0
- **Calculations**: All efficiency calculations safely handle nullable values with fallbacks

**Business Scenarios:**
- **Moving Operations**: Vessel travels routes → Record liters consumed + legs completed → Calculate efficiency per leg
- **Stationary Operations**: Vessel at port/anchor but engines running → Record liters consumed + leave legs empty/0 → Track stationary fuel usage
- **Mixed Operations**: Same vessel can have both stationary and moving consumption records

**Database Migration:** `MakeLegsCompletedNullable` migration updates existing databases to support nullable legs.

# Update files
- Always keep CLAUDE.md and todos.txt up-to-date after I confirm the test