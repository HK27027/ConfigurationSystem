using CaseSecilStore.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Collections.Concurrent;
using System.Text.Json;

namespace ConfigurationReader
{
    public class ConfigurationReader : IDisposable
    {
        private readonly string _applicationName;
        private readonly IMongoCollection<ConfigurationItem> _collection;
        private readonly Timer _refreshTimer;
        private readonly ConcurrentDictionary<string, ConfigurationItem> _cache;
        private readonly ILogger<ConfigurationReader> _logger;
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed = false;

        public ConfigurationReader(string applicationName, string connectionString, int refreshTimerIntervalInMs)
        {
            _applicationName = applicationName ?? throw new ArgumentNullException(nameof(applicationName));
            _cache = new ConcurrentDictionary<string, ConfigurationItem>();
            _semaphore = new SemaphoreSlim(1, 1);

            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<ConfigurationReader>();

            try
            {
                var client = new MongoClient(connectionString);
                var database = client.GetDatabase("ConfigurationDB");
                _collection = database.GetCollection<ConfigurationItem>("Configurations");

                _ = LoadConfigurationsAsync();

                _refreshTimer = new Timer(async _ => await RefreshConfigurationsAsync(),
                    null, TimeSpan.FromMilliseconds(refreshTimerIntervalInMs),
                    TimeSpan.FromMilliseconds(refreshTimerIntervalInMs));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize ConfigurationReader");
            }
        }

        public T GetValue<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            if (_cache.TryGetValue(key, out var item) && item.IsActive)
            {
                return ConvertValue<T>(item.Value, item.Type);
            }

            throw new KeyNotFoundException($"Configuration key '{key}' not found or inactive for application '{_applicationName}'");
        }

        public async Task<T> GetValueAsync<T>(string key)
        {
            return await Task.Run(() => GetValue<T>(key));
        }

        private async Task LoadConfigurationsAsync()
        {
            try
            {
                await _semaphore.WaitAsync();

                var filter = Builders<ConfigurationItem>.Filter.And(
                    Builders<ConfigurationItem>.Filter.Eq(x => x.ApplicationName, _applicationName),
                    Builders<ConfigurationItem>.Filter.Eq(x => x.IsActive, true)
                );

                var configurations = await _collection.Find(filter).ToListAsync();

                _cache.Clear();
                foreach (var config in configurations)
                {
                    _cache.TryAdd(config.Name, config);
                }

                _logger?.LogInformation($"Loaded {configurations.Count} configurations for application '{_applicationName}'");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load configurations from storage");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task RefreshConfigurationsAsync()
        {
            try
            {
                await LoadConfigurationsAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to refresh configurations");
            }
        }

        private T ConvertValue<T>(string value, string type)
        {
            try
            {
                return type.ToLower() switch
                {
                    "string" => (T)(object)value,
                    "int" or "integer" => (T)(object)int.Parse(value),
                    "bool" or "boolean" => (T)(object)bool.Parse(value),
                    "double" => (T)(object)double.Parse(value),
                    "decimal" => (T)(object)decimal.Parse(value),
                    "long" => (T)(object)long.Parse(value),
                    "float" => (T)(object)float.Parse(value),
                    _ => JsonSerializer.Deserialize<T>(value) ?? throw new InvalidOperationException($"Cannot convert value '{value}' to type '{typeof(T).Name}'")
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Cannot convert value '{value}' of type '{type}' to '{typeof(T).Name}'", ex);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _refreshTimer?.Dispose();
                _semaphore?.Dispose();
                _disposed = true;
            }
        }
    }
}