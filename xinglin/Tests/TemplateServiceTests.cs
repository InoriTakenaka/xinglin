using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using xinglin.Models.CoreEntities;
using xinglin.Services.Data;

namespace xinglin.Tests
{
    [TestClass]
    public class TemplateServiceTests
    {
        private Mock<IFileStorageService> _fileStorageServiceMock;
        private Mock<ILoggerService> _loggerServiceMock;
        private TemplateService _templateService;

        [TestInitialize]
        public void TestInitialize()
        {
            _fileStorageServiceMock = new Mock<IFileStorageService>();
            _loggerServiceMock = new Mock<ILoggerService>();
            _templateService = new TemplateService(_fileStorageServiceMock.Object, _loggerServiceMock.Object);
        }

        [TestMethod]
        public async Task GetAllTemplatesAsync_ShouldReturnTemplates()
        {
            // Arrange
            var template1 = new TemplateData { TemplateId = "template1", Name = "Template 1" };
            var template2 = new TemplateData { TemplateId = "template2", Name = "Template 2" };
            var templateFiles = new[] { "template1.json", "template2.json" };
            _fileStorageServiceMock.Setup(service => service.GetFiles(It.IsAny<string>(), "*.json", true)).Returns(templateFiles);
            _fileStorageServiceMock.Setup(service => service.ReadAllTextAsync("template1.json")).ReturnsAsync(JsonSerializer.Serialize(template1));
            _fileStorageServiceMock.Setup(service => service.ReadAllTextAsync("template2.json")).ReturnsAsync(JsonSerializer.Serialize(template2));

            // Act
            var result = await _templateService.GetAllTemplatesAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(template1.TemplateId, result[0].TemplateId);
            Assert.AreEqual(template2.TemplateId, result[1].TemplateId);
        }

        [TestMethod]
        public async Task GetTemplateByIdAsync_ShouldReturnTemplate()
        {
            // Arrange
            var templateId = "template1";
            var template = new TemplateData { TemplateId = templateId, Name = "Template 1" };
            var filePath = $"{templateId}.json";
            _fileStorageServiceMock.Setup(service => service.FileExists(filePath)).Returns(true);
            _fileStorageServiceMock.Setup(service => service.ReadAllTextAsync(filePath)).ReturnsAsync(JsonSerializer.Serialize(template));

            // Act
            var result = await _templateService.GetTemplateByIdAsync(templateId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(templateId, result.TemplateId);
        }

        [TestMethod]
        public async Task GetTemplateByIdAsync_ShouldReturnNullWhenTemplateNotFound()
        {
            // Arrange
            var templateId = "template1";
            var filePath = $"{templateId}.json";
            _fileStorageServiceMock.Setup(service => service.FileExists(filePath)).Returns(false);

            // Act
            var result = await _templateService.GetTemplateByIdAsync(templateId);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task SaveTemplateAsync_ShouldSaveTemplate()
        {
            // Arrange
            var template = new TemplateData { TemplateId = "template1", Name = "Template 1" };
            var filePath = $"{template.TemplateId}.json";
            var expectedJson = JsonSerializer.Serialize(template, new JsonSerializerOptions { WriteIndented = true });

            // Act
            await _templateService.SaveTemplateAsync(template);

            // Assert
            _fileStorageServiceMock.Verify(service => service.WriteAllTextAsync(filePath, expectedJson), Times.Once);
        }

        [TestMethod]
        public async Task DeleteTemplateAsync_ShouldDeleteTemplate()
        {
            // Arrange
            var templateId = "template1";
            var filePath = $"{templateId}.json";
            _fileStorageServiceMock.Setup(service => service.FileExists(filePath)).Returns(true);

            // Act
            await _templateService.DeleteTemplateAsync(templateId);

            // Assert
            _fileStorageServiceMock.Verify(service => service.DeleteFile(filePath), Times.Once);
        }

        [TestMethod]
        public async Task ImportTemplateAsync_ShouldImportTemplate()
        {
            // Arrange
            var sourceFilePath = "source-template.json";
            var template = new TemplateData { TemplateId = "template1", Name = "Template 1" };
            _fileStorageServiceMock.Setup(service => service.FileExists(sourceFilePath)).Returns(true);
            _fileStorageServiceMock.Setup(service => service.ReadAllTextAsync(sourceFilePath)).ReturnsAsync(JsonSerializer.Serialize(template));

            // Act
            await _templateService.ImportTemplateAsync(sourceFilePath);

            // Assert
            var expectedFilePath = $"{template.TemplateId}.json";
            var expectedJson = JsonSerializer.Serialize(template, new JsonSerializerOptions { WriteIndented = true });
            _fileStorageServiceMock.Verify(service => service.WriteAllTextAsync(expectedFilePath, expectedJson), Times.Once);
        }

        [TestMethod]
        public async Task LoadDefaultTemplateAsync_ShouldReturnDefaultTemplate()
        {
            // Act
            var result = await _templateService.LoadDefaultTemplateAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("默认模板", result.Name);
            Assert.AreEqual("默认的空白模板", result.Description);
        }

        [TestMethod]
        public async Task SaveReportInstanceAsync_ShouldSaveReportInstance()
        {
            // Arrange
            var reportInstance = new ReportInstance { ReportId = "report1" };
            var filePath = $"{reportInstance.ReportId}.json";
            var expectedJson = JsonSerializer.Serialize(reportInstance, new JsonSerializerOptions { WriteIndented = true });

            // Act
            await _templateService.SaveReportInstanceAsync(reportInstance);

            // Assert
            _fileStorageServiceMock.Verify(service => service.WriteAllTextAsync(filePath, expectedJson), Times.Once);
        }

        [TestMethod]
        public async Task LoadReportInstanceAsync_ShouldReturnReportInstance()
        {
            // Arrange
            var reportId = "report1";
            var reportInstance = new ReportInstance { ReportId = reportId };
            var filePath = $"{reportId}.json";
            _fileStorageServiceMock.Setup(service => service.FileExists(filePath)).Returns(true);
            _fileStorageServiceMock.Setup(service => service.ReadAllTextAsync(filePath)).ReturnsAsync(JsonSerializer.Serialize(reportInstance));

            // Act
            var result = await _templateService.LoadReportInstanceAsync(reportId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(reportId, result.ReportId);
        }

        [TestMethod]
        public async Task GetReportInstancesAsync_ShouldReturnReportInstances()
        {
            // Arrange
            var report1 = new ReportInstance { ReportId = "report1" };
            var report2 = new ReportInstance { ReportId = "report2" };
            var reportFiles = new[] { "report1.json", "report2.json" };
            _fileStorageServiceMock.Setup(service => service.GetFiles(It.IsAny<string>(), "*.json", false)).Returns(reportFiles);
            _fileStorageServiceMock.Setup(service => service.ReadAllTextAsync("report1.json")).ReturnsAsync(JsonSerializer.Serialize(report1));
            _fileStorageServiceMock.Setup(service => service.ReadAllTextAsync("report2.json")).ReturnsAsync(JsonSerializer.Serialize(report2));

            // Act
            var result = await _templateService.GetReportInstancesAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(report1.ReportId, result[0].ReportId);
            Assert.AreEqual(report2.ReportId, result[1].ReportId);
        }

        [TestMethod]
        public async Task DeleteReportInstanceAsync_ShouldDeleteReportInstance()
        {
            // Arrange
            var reportId = "report1";
            var filePath = $"{reportId}.json";
            _fileStorageServiceMock.Setup(service => service.FileExists(filePath)).Returns(true);

            // Act
            await _templateService.DeleteReportInstanceAsync(reportId);

            // Assert
            _fileStorageServiceMock.Verify(service => service.DeleteFile(filePath), Times.Once);
        }
    }
}