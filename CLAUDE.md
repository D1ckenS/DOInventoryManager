# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DO Inventory Manager is a WPF (.NET 9.0) application for managing diesel oil inventory for maritime operations. It tracks fuel purchases, consumption by vessels, and uses a FIFO allocation system to calculate costs and manage inventory valuation.

## Core Architecture

### Technology Stack
- **.NET 9.0** with WPF for the UI framework
- **Entity Framework Core 8.0** with SQLite for data persistence
- **EPPlus 8.0** for Excel report generation

### Key Dependencies
- `Microsoft.EntityFrameworkCore.Sqlite` (8.0.0): SQLite database provider
- `Microsoft.EntityFrameworkCore.Design` (8.0.0): EF Core design-time tools for migrations
- `Microsoft.EntityFrameworkCore.Tools` (8.0.0): Package Manager Console commands for EF Core
- `EPPlus` (8.0.8): Excel file generation and manipulation

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
- `ThemeService`: Dynamic theme management with Windows integration and instant switching
- `SummaryReportService`: Monthly summary and dashboard statistics generation
- `InventoryValuationService`: Current inventory valuation calculations using FIFO
- `FleetEfficiencyService`: Vessel efficiency analysis and route performance metrics
- `CostAnalysisService`: Cost breakdown and supplier comparison analytics
- `RoutePerformanceService`: Trip efficiency tracking and performance analysis
- `DataRecoveryService`: Database recovery and data validation utilities
- `SmoothScrollingService`: UI scrolling enhancements for better user experience

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

## UI Modernization Implementation Status

### ✅ Completed Features (Phase 1-2)
- **Complete Fluent Design Theme System**: Light/Dark themes with Microsoft Fluent colors
- **Collapsible Navigation**: Modern hamburger menu with 56px/280px states and persistence
- **Modern Typography**: Segoe UI Variable font system with semantic text styles
- **Dashboard Modernization**: Redesigned stat cards with hover animations and micro-interactions
- **Theme Switching Service**: Dynamic theme management with Windows integration
- **Component Library**: Modern button, card, and form styles with proper hover effects
- **ConsumptionView Modernization**: ✅ **COMPLETED** - FluentStyles buttons, theme-aware colors, DataGrid virtualization, modern form controls

### ✅ **RESOLVED UI ISSUES** (January 2025)

#### **✅ Issue 1: Theme Toggle Requires Application Restart - RESOLVED**
- **Problem**: Theme changes didn't apply immediately, requiring app restart for visual changes
- **Solution**: Enhanced `ThemeService` with comprehensive refresh mechanism:
  - Added `RefreshApplicationResources()` and `RefreshWindowResources()` methods
  - Implemented proper Dispatcher threading with render priority
  - Enhanced resource dictionary refresh and style re-application
- **Location**: `Services/ThemeService.cs` - completely rewritten refresh system
- **Result**: Instant theme switching with immediate visual feedback

#### **✅ Issue 2: Missing Fluent Icons - RESOLVED**
- **Problem**: Fluent icons showed as blank squares throughout navigation and dashboard
- **Solution**: Fixed icon implementation:
  - Enhanced fallback font families: `"Segoe Fluent Icons, Segoe MDL2 Assets, Webdings, Arial Unicode MS, Segoe UI Symbol"`
  - Applied proper FluentIconSmall styles to all header buttons
  - Standardized icon usage across all UI elements
- **Location**: `Themes/Typography.xaml` and `MainWindow.xaml`
- **Result**: All icons display properly with professional consistency

#### **✅ Issue 3: Pixelated Text in Cards and Buttons - RESOLVED**
- **Problem**: Text appeared blurry/pixelated in modernized UI elements
- **Solution**: Comprehensive text rendering optimization:
  - Added global `UseLayoutRounding="True"` and `RenderOptions.ClearTypeHint="Enabled"`
  - Implemented `TextOptions.TextFormattingMode="Display"` throughout
  - Applied text rendering properties to all typography styles and components
- **Location**: App.xaml, Typography.xaml, FluentStyles.xaml
- **Result**: Professional, crisp text rendering throughout application

#### **✅ Issue 4: Theme Switching Coverage - RESOLVED**
- **Problem**: Sidebar, main content, and header weren't switching themes until restart
- **Solution**: Enhanced refresh mechanism covers all UI elements:
  - Added comprehensive resource refresh for all windows
  - Implemented proper invalidation of visual tree for complex controls
  - Enhanced default style re-application system
- **Result**: Complete theme switching across all UI elements

#### **✅ Issue 5: DataGrid Alternating Row Colors - RESOLVED**
- **Problem**: Dark mode tables had poor contrast with alternating rows
- **Solution**: Added proper `AlternatingRowBackgroundBrush` colors:
  - Light theme: Subtle `#FAFAFA` alternating rows
  - Dark theme: Subtle `#323233` alternating rows for better contrast
- **Location**: LightTheme.xaml, DarkTheme.xaml, AppTheme.xaml
- **Result**: Professional table appearance in both themes

#### **✅ Issue 6: All DataGrids White Backgrounds in Dark Mode - RESOLVED**
- **Problem**: All DataGrids across entire project showing white backgrounds in dark mode, making text invisible until hover
- **Root Cause**: Individual view files had hardcoded `Background="White"` overriding global theme system
- **Solution**: Systematically replaced hardcoded backgrounds with dynamic theme resources across ALL views:
  - **DataGrid Containers**: `Background="White"` → `Background="{DynamicResource CardBackgroundBrush}"`
  - **Border Elements**: `BorderBrush="#dee2e6"` → `BorderBrush="{DynamicResource BorderSubtleBrush}"`
  - **8 Views Fixed**: PurchasesView, ConsumptionView, AllocationView, BackupManagementView, VesselsView, SuppliersView, ReportsView (60+ instances), SummaryView
- **Result**: All DataGrids now properly theme-aware with visible text in both modes

#### **✅ Issue 7: Invisible Text in Dark Mode - RESOLVED**
- **Problem**: Hardcoded dark text colors (`#2c3e50`, `#2e7d32`, `#1976d2`) invisible on dark backgrounds
- **Solution**: Converted to dynamic theme resources across ALL views:
  - `#2c3e50` (dark headers) → `{DynamicResource TextPrimaryBrush}`
  - `#2e7d32` (success colors) → `{DynamicResource SuccessForegroundBrush}`
  - `#1976d2` (brand colors) → `{DynamicResource PrimaryBrandBrush}`
- **Views Fixed**: 8 views with 177+ hardcoded colors replaced
- **Result**: All text visible with proper contrast in both light and dark modes

#### **✅ Issue 8: Colored Section Backgrounds Not Dark Mode Compatible - RESOLVED**
- **Problem**: Light colored info/warning/success sections (`#e3f2fd`, `#fff3cd`, `#e8f5e8`) invisible in dark mode
- **Solution**: Replaced with semantic theme-aware backgrounds:
  - Info sections: `#e3f2fd` → `{DynamicResource InfoBackgroundBrush}`
  - Warning sections: `#fff3cd` → `{DynamicResource WarningBackgroundBrush}`
  - Success sections: `#e8f5e8` → `{DynamicResource SuccessBackgroundBrush}`
  - Subtle sections: `#f8f9fa` → `{DynamicResource SubtleBackgroundBrush}`
- **40+ sections** converted across all views
- **Result**: All semantic backgrounds adapt properly to theme changes

#### **✅ Issue 9: ComboBox and DatePicker White Backgrounds - RESOLVED**
- **Problem**: Dropdown popups and date picker calendars showing white backgrounds with poor contrast in dark mode
- **Solution**: Created complete custom templates with theme integration:
  - **ComboBox**: Full custom `ControlTemplate` with theme-aware popup backgrounds
  - **DatePicker**: Complete redesign with enhanced visual design
- **ComboBox Features**: Click anywhere to open, theme-aware popup (`SurfaceBrush`), proper item highlighting
- **DatePicker Features**: Calendar icon, click anywhere functionality, enhanced text rendering, placeholder text
- **Result**: Professional form controls with excellent dark mode compatibility

#### **✅ Issue 10: Enhanced Form Control User Experience - RESOLVED**
- **Problem**: ComboBox required precise clicking on arrow, DatePicker had pixelated text and white borders
- **Solution**: Complete UX overhaul:
  - **ComboBox**: Full-area clickable with `ToggleButton` spanning entire control
  - **DatePicker**: Eliminated `DatePickerTextBox`, crystal clear text rendering, calendar icon design
  - **Text Quality**: Added all text rendering properties (`UseLayoutRounding`, `ClearTypeHint`, `TextFormattingMode`)
- **Result**: Premium user experience matching modern web applications

### **UI Architecture Overview**
- **Theme System**: `ThemeService.cs` manages dynamic switching between `LightTheme.xaml`/`DarkTheme.xaml`
- **Typography**: Centralized text styles in `Typography.xaml` with semantic naming (Display, Headline, Title, Body, Label, Caption)
- **Component Library**: Modern button/card styles in `FluentStyles.xaml` with hover animations and micro-interactions
- **Navigation**: Collapsible sidebar with Fluent icons, hamburger menu, and JSON state persistence
- **Dashboard**: Modernized stat cards using `StatCardStyle` with hover lift effects and proper spacing

### **Theme System Features**
- **Multi-Theme Support**: Light, Dark, and System (follows Windows theme)
- **Windows Integration**: Automatically detects system theme changes via registry monitoring
- **State Persistence**: Theme preferences saved to `theme-settings.json`
- **Dynamic Switching**: Real-time theme changes without application restart (when working correctly)
- **Color System**: Complete Fluent Design color palette with semantic naming

### **Typography System**
- **Primary Font**: Segoe UI Variable (with fallbacks to Segoe UI, system fonts)
- **Icon Font**: Segoe Fluent Icons (with fallbacks needed)
- **Scale**: 6-level typography scale from Display (40px) to Caption (10px)
- **Semantic Styles**: Content-aware text styles (primary, secondary, tertiary text colors)

## ✅ **Phase 1 UI Modernization Implementation (January 2025)**

### **Component Consistency Modernization - COMPLETED**
**Task**: Convert hardcoded styles to centralized FluentStyles for consistent theming across all views.

#### **✅ 1. Enhanced FluentStyles.xaml with Semantic Button Variants**
- **Added**: `FluentSuccessButtonStyle`, `FluentWarningButtonStyle`, `FluentDangerButtonStyle`
- **Features**: Proper theme integration, hover effects, micro-interactions with opacity transitions
- **Integration**: All buttons use DynamicResource theme colors for perfect dark mode compatibility

#### **✅ 2. BackupManagementView.xaml Modernization**
- **Fixed**: Replaced hardcoded `#DEE2E6` borders with `{DynamicResource BorderSubtleBrush}`
- **Fixed**: Replaced hardcoded `#7F8C8D` text with `{DynamicResource TextTertiaryBrush}`
- **Upgraded**: 17 button instances converted from local semantic styles to centralized FluentStyles
- **Performance**: Added DataGrid virtualization to all 3 DataGrids (BackupHistory, PurchaseHistory, ConsumptionHistory)

#### **✅ 3. PurchasesView.xaml Complete Overhaul**
- **Form Controls**: Converted 8 hardcoded `#ced4da` borders to `FluentComboBoxStyle`, `FluentDatePickerStyle`, `FluentTextBoxStyle`
- **Button Modernization**: 11 hardcoded button colors converted to semantic FluentStyles:
  - Action buttons: Edit (Warning), Delete (Danger), Save (Success), Clear (Secondary)
  - Filter buttons: Apply (Primary), Clear (Secondary), Quick filters (Success/Warning/Primary)
  - Refresh button: FluentButtonStyle
- **Theme Integration**: Replaced hardcoded `#1976d2` with `{DynamicResource PrimaryBrandBrush}`
- **Performance**: Added DataGrid virtualization with `VirtualizingPanel.IsVirtualizing="True"`

#### **✅ 4. MainWindow.xaml Navigation Cleanup**
- **Removed**: Legacy hardcoded `NavButtonStyle` and `ActiveNavButtonStyle` with `#34495e` backgrounds
- **Result**: Navigation fully uses existing `NavigationButtonStyle` and `ActiveNavigationButtonStyle` from FluentStyles

#### **✅ 5. DataGrid Virtualization Implementation**
- **Performance Enhancement**: Implemented across BackupManagementView (3 grids) and PurchasesView
- **Configuration**: `VirtualizingPanel.IsVirtualizing="True"` and `VirtualizationMode="Recycling"`
- **Benefits**: Improved performance for large datasets, reduced memory usage

#### **✅ 6. VesselsView.xaml Modernization**
- **Button Conversion**: 5 buttons converted from hardcoded colors to FluentStyles:
  - Add Vessel: `FluentSuccessButtonStyle` (was `#27ae60`)
  - Edit Vessel: `FluentWarningButtonStyle` (was `#f39c12`) 
  - Delete Vessel: `FluentDangerButtonStyle` (was `#e74c3c`)
  - Refresh: `FluentSecondaryButtonStyle` (was `#6c757d`)
  - Save: `FluentSuccessButtonStyle`, Cancel: `FluentSecondaryButtonStyle`
- **Form Controls**: TextBox and ComboBox converted to `FluentTextBoxStyle` and `FluentComboBoxStyle`
- **Theme Integration**: 6 instances of hardcoded colors (`#dee2e6`, `Gray`, `#856404`) replaced with dynamic resources
- **Performance**: Added DataGrid virtualization with `VirtualizingPanel.IsVirtualizing="True"`
- **Accessibility**: Improved text rendering with proper theme-aware foreground colors

### **Modernization Impact**
- **Views Modernized**: 3 complete (BackupManagementView, PurchasesView, VesselsView), MainWindow navigation cleanup
- **Hardcoded Colors Eliminated**: 65+ instances across completed views
- **Button Consistency**: All buttons now use centralized FluentStyles with proper theme integration
- **Performance**: DataGrid virtualization implemented for enhanced performance
- **Theme Compatibility**: All components now fully support light/dark theme switching

### **Remaining Modernization Tasks**
Based on pattern analysis, 6 additional views require similar treatment:
- **ConsumptionView**: 7 hardcoded backgrounds detected
- **AllocationView**: 4 hardcoded backgrounds detected  
- **VesselsView**: 6 hardcoded backgrounds detected
- **SuppliersView**: 6 hardcoded backgrounds detected
- **SummaryView**: 7 hardcoded backgrounds detected
- **ReportsView**: 42 hardcoded backgrounds detected (highest priority)

**Pattern Established**: The modernization approach is proven and can be systematically applied to remaining views using the same FluentStyles integration pattern.

# Update files
- Always keep CLAUDE.md and todos.txt up-to-date after I confirm the test and always before committing and pushing to github

# ⚠️ CRITICAL GIT RULES - NEVER VIOLATE ⚠️
**ABSOLUTELY NEVER COMMIT AND PUSH TO GITHUB UNTIL THE USER EXPLICITLY CONFIRMS TO COMMIT AND PUSH!**
- ALWAYS build and test changes first
- ALWAYS ask user to test the changes before committing
- ALWAYS wait for explicit user confirmation: "commit and push" or "push to github"
- NEVER assume user approval for git operations
- If changes break styling or functionality, fix before any git operations
- User must verify everything works correctly before any commits

# important-instruction-reminders
Do what has been asked; nothing more, nothing less.
NEVER create files unless they're absolutely necessary for achieving your goal.
ALWAYS prefer editing an existing file to creating a new one.
NEVER proactively create documentation files (*.md) or README files. Only create documentation files if explicitly requested by the User.