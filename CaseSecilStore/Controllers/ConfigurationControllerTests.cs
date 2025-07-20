using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using CaseSecilStore.Controllers;
using Library;

namespace CaseSecilStore.Tests
{
    public class ConfigurationControllerTests
    {
        private readonly Mock<ConfigurationReader> _mockConfigReader;
        private readonly Mock<ILogger<ConfigurationController>> _mockLogger;
        private readonly ConfigurationController _controller;

        public ConfigurationControllerTests()
        {
            _mockConfigReader = new Mock<ConfigurationReader>("TestApp", "mongodb://localhost", 5000);
            _mockLogger = new Mock<ILogger<ConfigurationController>>();
            _controller = new ConfigurationController(_mockConfigReader.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetConfiguration_ValidId_ReturnsOkResult()
        {
            var configId = "507f1f77bcf86cd799439011";
            var expectedConfig = new ConfigurationItem
            {
                Id = configId,
                Name = "TestConfig",
                Value = "TestValue",
                Type = "string"
            };

            _mockConfigReader.Setup(x => x.GetConfigurationByIdAsync(configId))
                           .ReturnsAsync(expectedConfig);

            var result = await _controller.GetConfiguration(configId);

            var okResult = Assert.IsType<ActionResult<ConfigurationItem>>(result);
            var returnValue = Assert.IsType<OkObjectResult>(okResult.Result);
            var config = Assert.IsType<ConfigurationItem>(returnValue.Value);
            Assert.Equal(expectedConfig.Id, config.Id);
            Assert.Equal(expectedConfig.Name, config.Name);
        }

        [Fact]
        public async Task GetConfiguration_InvalidId_ReturnsNotFound()
        {
            var configId = "nonexistent";
            _mockConfigReader.Setup(x => x.GetConfigurationByIdAsync(configId))
                           .ReturnsAsync((ConfigurationItem)null);

            var result = await _controller.GetConfiguration(configId);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task CreateConfiguration_ValidConfig_ReturnsCreatedAtAction()
        {
            var newConfig = new ConfigurationItem
            {
                Name = "NewConfig",
                Value = "NewValue",
                Type = "string"
            };

            var createdConfig = new ConfigurationItem
            {
                Id = "507f1f77bcf86cd799439012",
                Name = "NewConfig",
                Value = "NewValue",
                Type = "string"
            };

            _mockConfigReader.Setup(x => x.CreateConfigurationAsync(It.IsAny<ConfigurationItem>()))
                           .ReturnsAsync(createdConfig);

            var result = await _controller.CreateConfiguration(newConfig);

            var actionResult = Assert.IsType<ActionResult<ConfigurationItem>>(result);
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
            Assert.Equal("GetConfiguration", createdAtActionResult.ActionName);
        }

        [Fact]
        public async Task GetValueByName_ExistingName_ReturnsValue()
        {
            var configName = "TestConfig";
            var expectedValue = "TestValue";

            _mockConfigReader.Setup(x => x.GetValue<string>(configName))
                           .ReturnsAsync(expectedValue);

            var result = await _controller.GetValueByName(configName);

            var okResult = Assert.IsType<ActionResult<string>>(result);
            var returnValue = Assert.IsType<OkObjectResult>(okResult.Result);
            Assert.Equal(expectedValue, returnValue.Value);
        }
    }
}