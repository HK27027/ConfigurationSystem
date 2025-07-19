using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using CaseSecilStore.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;

namespace CaseSecilStore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigurationController : ControllerBase
    {
        private readonly IMongoCollection<ConfigurationItem> _collection;
        private readonly ILogger<ConfigurationController> _logger;
        private readonly IMemoryCache _memoryCache;

        public ConfigurationController(IMongoDatabase database, ILogger<ConfigurationController> logger, IMemoryCache cache)
        {
            _collection = database.GetCollection<ConfigurationItem>("Configurations");
            _logger = logger;
            _memoryCache = cache;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ConfigurationItem>>> GetConfigurations(
        [FromQuery] string? applicationName = null,
        [FromQuery] string? name = null)
        {
            try
            {
                var cacheKey = $"configurations_{applicationName ?? "all"}_{name ?? "all"}";

                var filterBuilder = Builders<ConfigurationItem>.Filter;
                var filter = filterBuilder.Empty;
                filter &= filterBuilder.Eq(x => x.IsActive, true);

                if (!string.IsNullOrEmpty(applicationName))
                {
                    filter &= filterBuilder.Eq(x => x.ApplicationName, applicationName);
                }
                if (!string.IsNullOrEmpty(name))
                {
                    filter &= filterBuilder.Regex(x => x.Name, new MongoDB.Bson.BsonRegularExpression(name, "i"));
                }

                var configurations = await _collection.Find(filter).ToListAsync();

               
                    var cacheEntryOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                        SlidingExpiration = TimeSpan.FromMinutes(10),
                        Priority = CacheItemPriority.High
                    };

                    _memoryCache.Set(cacheKey, configurations, cacheEntryOptions);
                    return Ok(configurations);
                
         
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving configurations");

                var fallbackCacheKey = $"configurations_{applicationName ?? "all"}_{name ?? "all"}";
                if (_memoryCache.TryGetValue(fallbackCacheKey, out List<ConfigurationItem>? fallbackConfigurations)
                    && fallbackConfigurations != null)
                {
                    _logger.LogWarning("Returning cached configurations due to error");
                    return Ok(fallbackConfigurations);
                }

                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ConfigurationItem>> GetConfiguration(string id)
        {
            try
            {
                var configuration = await _collection.Find(x => x.Id == id&&x.IsActive==true).FirstOrDefaultAsync();
                if (configuration == null)
                {
                    return NotFound();
                }
                return Ok(configuration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving configuration {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<ActionResult<ConfigurationItem>> CreateConfiguration(ConfigurationItem configuration)
        {
            try
            {
                if (string.IsNullOrEmpty(configuration.Name) ||
                    string.IsNullOrEmpty(configuration.Type) ||
                    string.IsNullOrEmpty(configuration.ApplicationName))
                {
                    return BadRequest("Name, Type, and ApplicationName are required");
                }

                var existingConfig = await _collection.Find(x =>
                    x.Name == configuration.Name &&
                    x.ApplicationName == configuration.ApplicationName).FirstOrDefaultAsync();

                if (existingConfig != null)
                {
                    return Conflict($"Configuration with name '{configuration.Name}' already exists for application '{configuration.ApplicationName}'");
                }


                await _collection.InsertOneAsync(configuration);
                return CreatedAtAction(nameof(GetConfiguration), new { id = configuration.Id }, configuration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating configuration");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ConfigurationItem>> UpdateConfiguration(string id, ConfigurationItem configuration)
        {
            try
            {
                var existingConfig = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
                if (existingConfig == null)
                {
                    return NotFound();
                }

                configuration.Id = id;

                await _collection.ReplaceOneAsync(x => x.Id == id, configuration);
                return Ok(configuration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating configuration {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteConfiguration(string id)
        {
            try
            {
                var existingConfig = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
                if (existingConfig == null)
                {
                    return NotFound();
                }
                existingConfig.IsActive = false;

                await _collection.ReplaceOneAsync(x => x.Id == id, existingConfig);
                return Ok(existingConfig);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating configuration {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("applications")]
        public async Task<ActionResult<IEnumerable<string>>> GetApplicationNames()
        {
            try
            {
                var filter = Builders<ConfigurationItem>.Filter.Eq(x => x.IsActive, true);

                var applicationNames = await _collection.Distinct<string>("ApplicationName", filter).ToListAsync();

                return Ok(applicationNames);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving application names");
                return StatusCode(500, "Internal server error");
            }
        }


    }
}