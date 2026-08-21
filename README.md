# InventoryPOS

A C# .NET WinForms desktop application for managing clothing inventory for resale on platforms like eBay, Poshmark, and Depop.

## Business Overview

**InventoryPOS** is a purpose-built inventory management tool for clothing resellers who sell across multiple platforms (eBay, Poshmark, Depop). It solves the core challenges of running a resale business:

- **Know your numbers instantly** — Track cost, listing price, sold price, and **automatically calculate true profit** after platform fees and shipping for each platform
- **Never lose track of inventory** — Organize every item with SKU, brand, size, condition, category, and color; filter and search in seconds
- **Visual proof for buyers** — Attach up to 20 photos per SKU, organized automatically in folders — perfect for creating listings or resolving disputes
- **Work offline, stay portable** — No database setup, no cloud dependency. Your inventory lives in a single JSON file you can back up, move, or sync however you choose
- **Speed matters** — Keyboard-first design with shortcuts for every common action (add, edit, delete, search, calculate profit) keeps you in flow

**Who it's for:** Individual resellers, small teams, and anyone flipping clothing who needs a lightweight, fast, and reliable way to manage inventory and calculate real profitability across platforms.

### Target Audience / User Personas

| Reseller Type | Why InventoryPOS Fits |
|---------------|----------------------|
| **Solo eBay/Poshmark/Depop seller** | Manages 50–500+ SKUs; needs quick profit checks before listing; wants photos organized by SKU for easy listing creation |
| **Thrift/garage sale flipper** | High-volume sourcing; adds items in batches; uses keyboard shortcuts to stay fast; tracks COG per item to know true margin |
| **Consignment / vintage curator** | Detailed condition grading, categories, colors; needs search/filter to find specific pieces for buyers or shows |
| **Multi-platform cross-lister** | Lists same item on 2–3 platforms; tracks which platform each item is on; calculates platform-specific fees to decide where to list first |
| **Part-time side hustler** | Limited time; works offline; no database to maintain; portable JSON file moves between home/office laptops |
| **Small team (2–5 people)** | Shared inventory file on network drive or synced folder; each person sees same data; UI state (sorts/filters) remembered per user |

---

## Features

### Inventory Management
- **Item Tracking**: Track items with SKU, brand, condition, size, category, color, and more
- **Pricing**: Cost of goods (COG), listing price, sold price, and computed profit
- **Listing Platforms**: Support for multiple platforms (eBay, Poshmark, Depop) stored as comma-separated values
- **Status Tracking**: Track item status (Available, Listed, Sold, etc.)
- **Search & Filter**: Real-time search across SKU, brand, and other fields
- **Sortable Grid**: Click column headers to sort; sort state persists across sessions

### Profit Calculator
- Platform-specific fee calculations for **eBay**, **Poshmark**, and **Depop**
- Calculates net profit after fees and shipping
- Helps determine optimal listing prices

### Picture Management
- Organizes photos by SKU in folders: `{PictureFolder}/pictures/{SKU}/`
- Supports JPG, JPEG, PNG, GIF formats
- Maximum 20 images per SKU
- Configurable picture folder path via Settings
- Auto-refreshes when tab is selected to reflect external changes

### Data Persistence
- **File-based JSON storage** — no database required
- Default location: `%LOCALAPPDATA%\InventoryPOS\inventory.json`
- Load/Save/Load As support for portable inventory files
- **UI State Persistence**: Remembers sort column, sort order, filter settings, last opened file, and picture folder path

### Keyboard Shortcuts
| Shortcut | Action |
|----------|--------|
| `Ctrl+N` | Add new item |
| `Ctrl+O` | Load inventory file |
| `Ctrl+S` | Save inventory |
| `Ctrl+Shift+S` | Save as |
| `Ctrl+,` | Open configuration |
| `Ctrl+P` | Open profit calculator |
| `F5` / `Ctrl+R` | Refresh data |
| `Enter` (on grid) | Edit selected item |
| `Del` (on grid) | Delete selected item |
| `Esc` (in search) | Clear search filter |

## Architecture

```
InventoryPOS/
├── Program.cs                      # Entry point, global exception handling
├── MainForm.cs                     # Main window: menu, toolbar, DataGridView, status bar
├── Models/
│   ├── InventoryItem.cs           # Data model (17 properties + computed Profit)
│   └── UiState.cs                 # Persisted UI state
├── Services/
│   └── InventoryRepository.cs     # JSON file-based CRUD + UI state persistence
├── Forms/
│   ├── InventoryEditForm.cs       # Item editor with Picture Management tab
│   ├── ProfitCalculatorForm.cs    # Platform-specific profit calculator
│   ├── ApplicationConfigurationForm.cs # Configure picture folder path
│   └── ErrorDialogForm.cs         # Unhandled exception dialog
└── Properties/PublishProfiles/    # ClickOnce publish profiles
```

### Key Technical Details
- **Target Framework**: .NET 10.0-windows
- **UI Pattern**: Programmatic WinForms (no `.Designer.cs` files)
- **Repository Pattern**: `InventoryRepository` handles all async file I/O
- **Thread-Safe UI Updates**: Uses `BeginInvoke`, `SuspendLayout`/`ResumeLayout`, and `BindingList` with `RaiseListChangedEvents = false` to avoid CurrencyManager index races
- **Computed Properties**: `InventoryItem.Profit` = `Earnings - COG` when `Status == "Sold"`, otherwise `0`

## Build & Run

```bash
# Build
dotnet build

# Run
dotnet run --project InventoryPOS.csproj

# Build Release
dotnet build --configuration Release

# Clean
dotnet clean
```

## CI/CD

GitHub Actions workflow (`.github/workflows/ci-release.yml`):
- Builds on every push to `main`/`master`
- Creates GitHub Release with zip artifact on version tag pushes (e.g., `v1.0.0`)
- Uses `dotnet publish` with `PublishSingleFile=true` and `SelfContained=false`

## Requirements

- .NET 10.0 SDK (or compatible)
- Windows (WinForms desktop application)

## License

[Add your license here]