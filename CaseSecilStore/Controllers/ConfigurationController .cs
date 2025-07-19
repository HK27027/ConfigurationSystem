using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using CaseSecilStore.Models;
using Microsoft.Extensions.Configuration;

namespace CaseSecilStore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigurationController : ControllerBase
    {
        private readonly IMongoCollection<ConfigurationItem> _collection;
        private readonly ILogger<ConfigurationController> _logger;

        public ConfigurationController(IMongoDatabase database, ILogger<ConfigurationController> logger)
        {
            _collection = database.GetCollection<ConfigurationItem>("Configurations");
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ConfigurationItem>>> GetConfigurations(
            [FromQuery] string? applicationName = null,
            [FromQuery] string? name = null)
        {
            try
            {
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
                return Ok(configurations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving configurations");
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
                // Validate configuration
                if (string.IsNullOrEmpty(configuration.Name) ||
                    string.IsNullOrEmpty(configuration.Type) ||
                    string.IsNullOrEmpty(configuration.ApplicationName))
                {
                    return BadRequest("Name, Type, and ApplicationName are required");
                }

                // Check if configuration with same name exists for the application
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