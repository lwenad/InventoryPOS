# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

InventoryPOS is a C# .NET WinForms desktop application for managing clothing inventory for resale. It tracks items with details like SKU, brand, condition, pricing, listing platform, and sold status. It includes a profit calculator for eBay, Poshmark, and Depop, and a picture management feature that organizes item photos by SKU in a configurable folder.

## Build and Run

```bash
# Build
dotnet build

# Run
dotnet run --project InventoryPOS.csproj

# Build specific configuration
dotnet build --configuration Release

# Clean
dotnet clean
```

The app targets `.NET 10.0-windows` (see `InventoryPOS.csproj`). The CI workflow (`.github/workflows/ci-release.yml`) uses .NET 8.0.x SDK — update that version if you bump the target framework.

## Architecture

```
InventoryPOS/                  # Root namespace
├── Program.cs                 # Entry point; sets up global exception handling (UI, thread, task)
├── MainForm.cs                # Main window: menu strip, tool strip, DataGridView, status strip
├── Models/
│   ├── InventoryItem.cs       # Data model (17 properties + computed Profit property)
│   └── UiState.cs             # Persisted UI state (sort, filter, file path, picture folder)
├── Services/
│   └── InventoryRepository.cs # JSON file-based CRUD + UI state persistence
├── Forms/
│   ├── InventoryEditForm.cs   # Item editor (add/edit) with Picture Management tab
│   ├── ProfitCalculatorForm.cs  # Platform-specific profit calculator (eBay/Poshmark/Depop)
│   ├── ApplicationConfigurationForm.cs # Configure picture folder path
│   └── ErrorDialogForm.cs     # Unhandled exception dialog with copy-to-clipboard
└── Properties/PublishProfiles/  # Click once publish profiles
```

### Key Patterns

- **File-based storage**: No database. Inventory is stored as JSON in `%LOCALAPPDATA%\InventoryPOS\inventory.json` by default. Users can load/save to any `.json` file via File → Load/Save.
- **UI state persistence**: Sort column, sort order, filter settings, last opened file path, and picture folder path are saved to `%LOCALAPPDATA%\InventoryPOS\ui_state.json` and restored on startup.
- **Repository pattern**: `InventoryRepository` handles all file I/O (async methods for data, sync for UI state). Each CRUD method (`AddAsync`, `UpdateAsync`, `DeleteAsync`) reads the full file, modifies in memory, then writes back.
- **WinForms programmatic UI**: No `.Designer.cs` files — all forms build their controls in code via `InitializeComponent()` + helper methods.
- **Defensive DataGridView updates**: Uses `BeginInvoke` to defer UI updates and `SuspendLayout`/`ResumeLayout` + `BindingList.Clear()` with `RaiseListChangedEvents = false` to avoid CurrencyManager index races. See `MainForm.BindGrid()`.

### Important Data Model Details

- `InventoryItem.Profit` is a **computed read-only property**: `Earnings - COG` when `Status == "Sold"`, otherwise `0m`.
- `ListingPlatform` stores multiple selected platforms as a comma-separated string (e.g., `"eBay, Poshmark"`).
- `Id` is a `Guid` string that uniquely identifies each item.
- `CreatedAt`/`UpdatedAt` track item lifecycle; `UpdatedAt` is reset to `DateTime.Now` on every save.

### Keyboard Shortcuts

| Shortcut              | Action                         |
|-----------------------|--------------------------------|
| Ctrl+N                | Add new item                   |
| Ctrl+O                | Load inventory file            |
| Ctrl+S                | Save inventory                 |
| Ctrl+Shift+S          | Save as                        |
| Ctrl+,                | Open configuration             |
| Ctrl+P                | Open profit calculator         |
| F5 / Ctrl+R           | Refresh data                   |
| Enter (on grid)       | Edit selected item             |
| Del (on grid)         | Delete selected item           |
| Esc (in search box)   | Clear search filter            |

### Picture Management

- Pictures are stored at `{PictureFolderPath}/pictures/{SKU}/` with a maximum of 20 images per SKU.
- Supported formats: JPG, JPEG, PNG, GIF.
- The picture folder is configured via Configuration → Browse for folder.
- The Picture Management tab in `InventoryEditForm` refreshes when navigated to (via `SelectedIndexChanged`), so it reflects external changes.

### CI/CD

GitHub Actions builds on every push to `main`/`master` and creates a GitHub Release with a zip artifact on version tag pushes (e.g., `v1.0.0`). The publish step uses `dotnet publish` with `PublishSingleFile=true` and `SelfContained=false`.
