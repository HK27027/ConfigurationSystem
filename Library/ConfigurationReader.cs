using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Timers;

namespace Library
{
    public class ConfigurationReader : IDisposable
    {
        private readonly ILogger<ConfigurationReader>? _logger;
        private readonly string _applicationName;
        private readonly ConcurrentDictionary<string, ConfigurationItem> _configCache;
        private readonly IMongoCollection<ConfigurationItem> _collection;
        private readonly System.Timers.Timer _refreshTimer;
        private bool _disposed = false;

        public ConfigurationReader(string applicationName, string connectionString, int refreshTimerIntervalInMs)
        {
            if (string.IsNullOrEmpty(applicationName))
                throw new ArgumentException("Application name cannot be null or empty", nameof(applicationName));
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

            _applicationName = applicationName;
            _configCache = new ConcurrentDictionary<string, ConfigurationItem>();

            try
            {
                // MongoDB bağlantısını kur
                var client = new MongoClient(connectionString);
                var database = client.GetDatabase("CaseSecilStoreDb");
                _collection = database.GetCollection<ConfigurationItem>("Configurations");

                // İlk yükleme
                LoadConfigurations();

                // Timer kurulumu
                _refreshTimer = new System.Timers.Timer(refreshTimerIntervalInMs);
                _refreshTimer.Elapsed += async (sender, e) => await RefreshConfigurations();
                _refreshTimer.Start();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize ConfigurationReader");
                throw;
            }
        }

     
        public async Task SyncConfigurationsFromAppSettings(IConfiguration configuration)
        {
            try
            {
                var configsToSync = new List<ConfigurationItem>();

                // appsettings.json'dan tüm ayarları alıyorum yoksa kaydet varsa güncelle
                ExtractConfigurationsFromSection(configuration, "", configsToSync);

                foreach (var config in configsToSync)
                {
                    config.ApplicationName = _applicationName;
                    config.IsActive = true;

                    // MongoDB'de kayıt var mı kontrol ediyorum
                    var filter = Builders<ConfigurationItem>.Filter.And(
                        Builders<ConfigurationItem>.Filter.Eq(c => c.Name, config.Name),
                        Builders<ConfigurationItem>.Filter.Eq(c => c.ApplicationName, _applicationName)
                    );

                    var existingConfig = await _collection.Find(filter).FirstOrDefaultAsync();

                    if (existingConfig == null)
                    {
                        await _collection.InsertOneAsync(config);
                        _logger?.LogInformation($"Added new configuration: {config.Name} = {config.Value}");
                    }
                    else if (existingConfig.Value != config.Value || existingConfig.Type != config.Type)
                    {
                        var update = Builders<ConfigurationItem>.Update
                            .Set(c => c.Value, config.Value)
                            .Set(c => c.Type, config.Type)
                            .Set(c => c.IsActive, config.IsActive);

                        await _collection.UpdateOneAsync(filter, update);
                        _logger?.LogInformation($"Updated configuration: {config.Name} = {config.Value}");
                    }
                }

                await LoadConfigurations();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to sync configurations from appsettings");
            }
        }

        private void ExtractConfigurationsFromSection(IConfiguration configuration, string sectionPrefix, List<ConfigurationItem> configs, HashSet<string> excludeSections = null)
        {
            excludeSections ??= new HashSet<string> { "Logging", "AllowedHosts", "ConnectionStrings", "ConfigurationReader" };

            foreach (var child in configuration.GetChildren())
            {
                var currentPath = string.IsNullOrEmpty(sectionPrefix) ? child.Key : $"{sectionPrefix}:{child.Key}";

                if (string.IsNullOrEmpty(sectionPrefix) && excludeSections.Contains(child.Key))
                    continue;

                if (child.GetChildren().Any())
                {
                    ExtractConfigurationsFromSection(child, currentPath, configs, excludeSections);
                }
                else
                {
                    var value = child.Value;
                    if (!string.IsNullOrEmpty(value))
                    {
                        var configItem = new ConfigurationItem
                        {
                            Name = currentPath,
                            Value = value,
                            Type = DetermineValueType(value)
                        };
                        configs.Add(configItem);
                    }
                }
            }
        }

        private string DetermineValueType(string value)
        {
            if (bool.TryParse(value, out _))
                return "bool";
            if (int.TryParse(value, out _))
                return "int";
            if (double.TryParse(value, out _))
                return "double";
            return "string";
        }

        public T GetValue<T>(string key)
        {
            if (_configCache.TryGetValue(key, out var configItem))
            {
                return ConvertValue<T>(configItem.Value, configItem.Type);
            }

            _logger?.LogWarning($"Configuration key '{key}' not found for application '{_applicationName}'");
            return default(T);
        }

        private T ConvertValue<T>(string value, string type)
        {
            if (string.IsNullOrEmpty(value))
                return default(T);

            var targetType = typeof(T);
            var converter = TypeDescriptor.GetConverter(targetType);

            if (converter != null && converter.CanConvertFrom(typeof(string)))
            {
                return (T)converter.ConvertFromString(value);
            }

            return (T)Convert.ChangeType(value, targetType);
        }

        private async Task LoadConfigurations()
        {
            try
            {
                var filter = Builders<ConfigurationItem>.Filter.And(
                    Builders<ConfigurationItem>.Filter.Eq(c => c.ApplicationName, _applicationName),
                    Builders<ConfigurationItem>.Filter.Eq(c => c.IsActive, true)
                );

                var configurations = await _collection.Find(filter).ToListAsync();

                _configCache.Clear();
                foreach (var config in configurations)
                {
                    _configCache.TryAdd(config.Name, config);
                }

                _logger?.LogInformation($"Loaded {configurations.Count} configurations for application '{_applicationName}'");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load configurations from storage");
            }
        }

        private async Task RefreshConfigurations()
        {
            await LoadConfigurations();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _refreshTimer?.Stop();
                _refreshTimer?.Dispose();
                _disposed = true;
            }
        }
    }

    public class ConfigurationItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public bool IsActive { get; set; }=true;
        public string ApplicationName { get; set; }
    }
}