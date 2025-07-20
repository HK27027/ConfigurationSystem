using Microsoft.AspNetCore.Mvc;
using Library;

namespace CaseSecilStore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigurationController : ControllerBase
    {
        private readonly ConfigurationReader _configurationReader;
        private readonly ILogger<ConfigurationController> _logger;

        public ConfigurationController(ConfigurationReader configurationReader, ILogger<ConfigurationController> logger)
        {
            _configurationReader = configurationReader;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ConfigurationItem>>> GetConfigurations([FromQuery] string? name = null)
        {
            try
            {
                IEnumerable<ConfigurationItem> configurations;

                if (!string.IsNullOrEmpty(name))
                {
                    configurations = await _configurationReader.SearchConfigurationsByNameAsync(name);
                }
                else
                {
                    configurations = await _configurationReader.GetAllConfigurationsAsync();
                }

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
                var configuration = await _configurationReader.GetConfigurationByIdAsync(id);
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

        [HttpGet("value/{name}")]
        public async Task<ActionResult<string>> GetValueByName(string name)
        {
            try
            {
                 var value = await _configurationReader.GetValue<string>(name);
                if (value == null)
                {
                    return NotFound($"Configuration with name '{name}' not found");
                }
                return Ok(value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving value for configuration name {Name}", name);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<ActionResult<ConfigurationItem>> CreateConfiguration(ConfigurationItem configuration)
        {
            try
            {
                var createdConfiguration = await _configurationReader.CreateConfigurationAsync(configuration);
                return CreatedAtAction(nameof(GetConfiguration), new { id = createdConfiguration.Id }, createdConfiguration);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
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
                var updatedConfiguration = await _configurationReader.UpdateConfigurationAsync(id, configuration);
                if (updatedConfiguration == null)
                {
                    return NotFound();
                }
                return Ok(updatedConfiguration);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
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
                var deleted = await _configurationReader.DeleteConfigurationAsync(id);
                if (!deleted)
                {
                    return NotFound();
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting configuration {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}