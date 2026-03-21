using Microsoft.VisualStudio.TestTools.UnitTesting;
using xinglin.Services.Data;
using System.IO;
using System;
using System.Threading.Tasks;

namespace xinglin.Tests
{
    [TestClass]
    public class DataServiceTests
    {
        private DataService _dataService;
        private string _testDataKey;

        [TestInitialize]
        public void TestInitialize()
        {
            _dataService = new DataService();
            _testDataKey = "test_data";
        }

        [TestCleanup]
        public void TestCleanup()
        {
            var dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", $"{_testDataKey}.json");
            if (File.Exists(dataPath))
            {
                File.Delete(dataPath);
            }
        }

        [TestMethod]
        public async Task SaveData_ShouldSaveDataToFile()
        {
            // Arrange
            var testData = new { Name = "Test Name", Age = 30 };

            // Act
            await _dataService.SaveDataAsync(testData, _testDataKey);

            // Assert
            var dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", $"{_testDataKey}.json");
            Assert.IsTrue(File.Exists(dataPath));
        }

        [TestMethod]
        public async Task LoadData_ShouldLoadDataFromFile()
        {
            // Arrange
            var testData = new { Name = "Test Name", Age = 30 };
            await _dataService.SaveDataAsync(testData, _testDataKey);

            // Act
            var loadedData = await _dataService.LoadDataAsync(_testDataKey);

            // Assert
            Assert.IsNotNull(loadedData);
        }

        [TestMethod]
        public void ValidateDataSimple_ShouldReturnFalse_WhenDataIsNull()
        {
            // Arrange & Act
            var result = _dataService.ValidateDataSimple(null);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidateDataSimple_ShouldReturnTrue_WhenDataIsNotNull()
        {
            // Arrange & Act
            var result = _dataService.ValidateDataSimple(new { Name = "Test" });

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ValidateData_ShouldReturnValidationResult()
        {
            // Arrange
            var testData = new { Name = "Test Name" };

            // Act
            var result = _dataService.ValidateData(testData);

            // Assert
            Assert.IsNotNull(result);
        }
    }
}
