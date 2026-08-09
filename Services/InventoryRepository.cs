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
        private readonly string _defaultDataFilePath;
        private string _currentDataFilePath;
        private readonly JsonSerializerOptions _jsonOptions;

        public string CurrentFilePath => _currentDataFilePath;

        public InventoryRepository(string? dataFilePath = null)
        {
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
        }

        public void SetFilePath(string filePath)
        {
            _currentDataFilePath = filePath;
        }

        public void ResetToDefaultPath()
        {
            _currentDataFilePath = _defaultDataFilePath;
        }

        public async Task<List<InventoryItem>> GetAllAsync()
        {
            if (!File.Exists(_currentDataFilePath))
                return new List<InventoryItem>();

            try
            {
                var json = await File.ReadAllTextAsync(_currentDataFilePath);
                return JsonSerializer.Deserialize<List<InventoryItem>>(json, _jsonOptions) ?? new List<InventoryItem>();
            }
            catch
            {
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
            await File.WriteAllTextAsync(_currentDataFilePath, json);
        }

        public async Task SaveAllAsync(List<InventoryItem> items, string filePath)
        {
            foreach (var item in items)
            {
                item.UpdatedAt = DateTime.Now;
            }

            var json = JsonSerializer.Serialize(items, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }

        public async Task AddAsync(InventoryItem item)
        {
            var items = await GetAllAsync();
            item.CreatedAt = DateTime.Now;
            item.UpdatedAt = DateTime.Now;
            items.Add(item);
            await SaveAllAsync(items);
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
            }
        }

        public async Task DeleteAsync(string id)
        {
            var items = await GetAllAsync();
            items.RemoveAll(i => i.Id == id);
            await SaveAllAsync(items);
        }

        public async Task<InventoryItem?> GetByIdAsync(string id)
        {
            var items = await GetAllAsync();
            return items.Find(i => i.Id == id);
        }
    }
}