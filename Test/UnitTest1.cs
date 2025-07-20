using NUnit.Framework;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using CaseSecilStore.Controllers;
using Library;

namespace UnitTestProject
{
    // ana projede kullanmadýðým için Interface oluþtur
    public interface IConfigurationReader
    {
        Task<ConfigurationItem?> GetConfigurationByIdAsync(string id);
        Task<IEnumerable<ConfigurationItem>> GetAllConfigurationsAsync();
        Task<IEnumerable<ConfigurationItem>> SearchConfigurationsByNameAsync(string name);
        Task<T> GetValue<T>(string key);
        Task<ConfigurationItem> CreateConfigurationAsync(ConfigurationItem configuration);
        Task<ConfigurationItem?> UpdateConfigurationAsync(string id, ConfigurationItem configuration);
        Task<bool> DeleteConfigurationAsync(string id);
    }

    public class Tests
    {
        private Mock<IConfigurationReader> _mockConfigReader;
        private Mock<ILogger<ConfigurationController>> _mockLogger;
        private TestableConfigurationController _controller;

        public class TestableConfigurationController : ControllerBase
        {
            private readonly IConfigurationReader _configurationReader;
            private readonly ILogger<ConfigurationController> _logger;

            public TestableConfigurationController(IConfigurationReader configurationReader, ILogger<ConfigurationController> logger)
            {
                _configurationReader = configurationReader;
                _logger = logger;
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
        }

        [SetUp]
        public void Setup()
        {
            _mockConfigReader = new Mock<IConfigurationReader>();
            _mockLogger = new Mock<ILogger<ConfigurationController>>();
            _controller = new TestableConfigurationController(_mockConfigReader.Object, _mockLogger.Object);
        }

        [Test]
        public async Task GetConfiguration_ReturnsNotFound_WhenConfigurationDoesNotExist()
        {
            string testId = "nonexistent";
            _mockConfigReader.Setup(x => x.GetConfigurationByIdAsync(testId))
                           .ReturnsAsync((ConfigurationItem)null);

            var result = await _controller.GetConfiguration(testId);

            Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task GetValueByName_ReturnsNotFound_WhenValueDoesNotExist()
        {
            string testName = "nonexistent";
            _mockConfigReader.Setup(x => x.GetValue<string>(testName))
                           .ReturnsAsync((string)null);

            var result = await _controller.GetValueByName(testName);

            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task GetValueByName_ReturnsOk_WhenValueExists()
        {
            string testName = "TestConfig";
            string testValue = "TestValue";
            _mockConfigReader.Setup(x => x.GetValue<string>(testName))
                           .ReturnsAsync(testValue);

            var result = await _controller.GetValueByName(testName);

            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult.Value, Is.EqualTo(testValue));
        }
    }
}