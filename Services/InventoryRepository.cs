using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using InventoryPOS.Models;

namespace InventoryPOS.Services
{
    public class InventoryRepository
    {
        private readonly LoggerService _logger;
        private readonly string _defaultDataFilePath;
        private string _currentDataFilePath;
        private readonly JsonSerializerOptions _jsonOptions;

        public string CurrentFilePath => _currentDataFilePath;

        public InventoryRepository(string? dataFilePath = null)
        {
            _logger = LoggerService.Instance;

            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appDataPath, "InventoryPOS");
            Directory.CreateDirectory(appFolder);

            _defaultDataFilePath = Path.Combine(appFolder, "inventory.json");
            _currentDataFilePath = dataFilePath ?? _defaultDataFilePath;

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };

            _logger.LogInfo($"InventoryRepository initialized. Data file: {_currentDataFilePath}");
        }

        public void SetFilePath(string filePath)
        {
            _logger.LogInfo($"Setting data file path: {filePath}");
            _currentDataFilePath = filePath;
        }

        public void ResetToDefaultPath()
        {
            _logger.LogInfo($"Resetting to default data file path: {_defaultDataFilePath}");
            _currentDataFilePath = _defaultDataFilePath;
        }

        public async Task<List<InventoryItem>> GetAllAsync()
        {
            if (!File.Exists(_currentDataFilePath))
            {
                _logger.LogInfo($"Data file does not exist, returning empty list: {_currentDataFilePath}");
                return new List<InventoryItem>();
            }

            try
            {
                var json = await File.ReadAllTextAsync(_currentDataFilePath);
                var items = JsonSerializer.Deserialize<List<InventoryItem>>(json, _jsonOptions) ?? new List<InventoryItem>();
                _logger.LogInfo($"Loaded {items.Count} items from {_currentDataFilePath}");
                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to read data file {_currentDataFilePath}", ex);
                return new List<InventoryItem>();
            }
        }

        public async Task SaveAllAsync(List<InventoryItem> items)
        {
            foreach (var item in items)
            {
                item.UpdatedAt = DateTime.Now;
            }

            var json = JsonSerializer.Serialize(items, _jsonOptions);
            try
            {
                await File.WriteAllTextAsync(_currentDataFilePath, json);
                _logger.LogInfo($"Saved {items.Count} items to {_currentDataFilePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save data file {_currentDataFilePath}", ex);
                throw;
            }
        }

        public async Task SaveAllAsync(List<InventoryItem> items, string filePath)
        {
            foreach (var item in items)
            {
                item.UpdatedAt = DateTime.Now;
            }

            var json = JsonSerializer.Serialize(items, _jsonOptions);
            try
            {
                await File.WriteAllTextAsync(filePath, json);
                _logger.LogInfo($"Saved {items.Count} items to {filePath} (Save As)");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save data file {filePath} (Save As)", ex);
                throw;
            }
        }

        public async Task AddAsync(InventoryItem item)
        {
            var items = await GetAllAsync();
            item.CreatedAt = DateTime.Now;
            item.UpdatedAt = DateTime.Now;
            items.Add(item);
            await SaveAllAsync(items);
            _logger.LogInfo($"Item added to repository. SKU: {item.SKU}, Title: {item.Title}, Id: {item.Id}");
        }

        public async Task UpdateAsync(InventoryItem item)
        {
            var items = await GetAllAsync();
            var index = items.FindIndex(i => i.Id == item.Id);
            if (index >= 0)
            {
                item.UpdatedAt = DateTime.Now;
                items[index] = item;
                await SaveAllAsync(items);
                _logger.LogInfo($"Item updated in repository. SKU: {item.SKU}, Title: {item.Title}, Id: {item.Id}");
            }
            else
            {
                _logger.LogWarning($"Update attempted but item not found in repository. Id: {item.Id}");
            }
        }

        public async Task DeleteAsync(string id)
        {
            var items = await GetAllAsync();
            var removedCount = items.RemoveAll(i => i.Id == id);
            await SaveAllAsync(items);
            _logger.LogInfo($"Item deleted from repository. Id: {id}, Items removed: {removedCount}");
        }

        public async Task<InventoryItem?> GetByIdAsync(string id)
        {
            var items = await GetAllAsync();
            return items.Find(i => i.Id == id);
        }

        // UI state persistence (separate JSON alongside app data)
        private string GetUiStatePath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appDataPath, "InventoryPOS");
            Directory.CreateDirectory(appFolder);
            return Path.Combine(appFolder, "ui_state.json");
        }

        public async Task SaveUiStateAsync(UiState state)
        {
            try
            {
                var path = GetUiStatePath();
                var json = JsonSerializer.Serialize(state, _jsonOptions);
                await File.WriteAllTextAsync(path, json);
                _logger.LogInfo($"UI state saved to {path}");
            }
            catch (Exception ex)
            {
                // ignore UI save errors
                _logger.LogWarning($"Failed to save UI state", ex);
            }
        }

        public UiState? LoadUiState()
        {
            try
            {
                var path = GetUiStatePath();
                if (!File.Exists(path)) return null;
                var json = File.ReadAllText(path);
                var state = JsonSerializer.Deserialize<UiState>(json, _jsonOptions);
                _logger.LogInfo($"UI state loaded from {path}");
                return state;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to load UI state", ex);
                return null;
            }
        }

        public async Task<UiState?> LoadUiStateAsync()
        {
            try
            {
                var path = GetUiStatePath();
                if (!File.Exists(path)) return null;
                var json = await File.ReadAllTextAsync(path);
                var state = JsonSerializer.Deserialize<UiState>(json, _jsonOptions);
                _logger.LogInfo($"UI state loaded (async) from {path}");
                return state;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to load UI state (async)", ex);
                return null;
            }
        }
    }
}
