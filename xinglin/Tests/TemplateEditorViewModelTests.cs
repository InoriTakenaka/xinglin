using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Threading.Tasks;
using xinglin.Models.CoreEntities;
using xinglin.Services.Data;
using xinglin.ViewModels;

namespace xinglin.Tests
{
    [TestClass]
    public class TemplateEditorViewModelTests
    {
        private Mock<ITemplateService> _templateServiceMock;
        private Mock<ILoggerService> _loggerServiceMock;
        private TemplateEditorViewModel _viewModel;

        [TestInitialize]
        public void TestInitialize()
        {
            _templateServiceMock = new Mock<ITemplateService>();
            _loggerServiceMock = new Mock<ILoggerService>();
            _viewModel = new TemplateEditorViewModel(_templateServiceMock.Object);
        }

        [TestMethod]
        public async Task CreateNewTemplateAsync_ShouldCreateNewTemplate()
        {
            // Arrange
            var expectedTemplate = new TemplateData();
            _templateServiceMock.Setup(service => service.CreateNewTemplate()).Returns(expectedTemplate);

            // Act
            await _viewModel.CreateNewTemplateCommand.ExecuteAsync(null);

            // Assert
            Assert.IsNotNull(_viewModel.CurrentTemplate);
            Assert.AreEqual(expectedTemplate, _viewModel.CurrentTemplate);
            _templateServiceMock.Verify(service => service.CreateNewTemplate(), Times.Once);
        }

        [TestMethod]
        public async Task LoadTemplateAsync_ShouldLoadTemplate()
        {
            // Arrange
            var templateId = "test-template-id";
            var expectedTemplate = new TemplateData { TemplateId = templateId };
            _templateServiceMock.Setup(service => service.GetTemplateByIdAsync(templateId)).ReturnsAsync(expectedTemplate);

            // Act
            await _viewModel.LoadTemplateCommand.ExecuteAsync(templateId);

            // Assert
            Assert.IsNotNull(_viewModel.CurrentTemplate);
            Assert.AreEqual(expectedTemplate, _viewModel.CurrentTemplate);
            _templateServiceMock.Verify(service => service.GetTemplateByIdAsync(templateId), Times.Once);
        }

        [TestMethod]
        public async Task SaveTemplateAsync_ShouldSaveTemplate()
        {
            // Arrange
            var template = new TemplateData { TemplateId = "test-template-id" };
            _viewModel.CurrentTemplate = template;
            _templateServiceMock.Setup(service => service.SaveTemplateAsync(template)).Returns(Task.CompletedTask);

            // Act
            await _viewModel.SaveTemplateCommand.ExecuteAsync(null);

            // Assert
            _templateServiceMock.Verify(service => service.SaveTemplateAsync(template), Times.Once);
        }

        [TestMethod]
        public void AddControlAtPosition_ShouldAddControl()
        {
            // Arrange
            var template = new TemplateData
            {
                Layout = new LayoutMetadata
                {
                    EditableElements = new System.Collections.ObjectModel.ObservableCollection<ControlElement>()
                }
            };
            _viewModel.CurrentTemplate = template;
            var control = new ControlElement { DisplayName = "Test Control" };
            double x = 100;
            double y = 200;

            // Act
            _viewModel.AddControlAtPosition(control, x, y);

            // Assert
            Assert.AreEqual(1, template.Layout.EditableElements.Count);
            Assert.AreEqual(control, template.Layout.EditableElements[0]);
            Assert.AreEqual(x, control.X);
            Assert.AreEqual(y, control.Y);
            Assert.AreEqual(control, _viewModel.SelectedControl);
            Assert.IsTrue(control.IsSelected);
        }

        [TestMethod]
        public void SelectControl_ShouldSelectControl()
        {
            // Arrange
            var template = new TemplateData
            {
                Layout = new LayoutMetadata
                {
                    EditableElements = new System.Collections.ObjectModel.ObservableCollection<ControlElement>()
                }
            };
            _viewModel.CurrentTemplate = template;
            var control1 = new ControlElement { DisplayName = "Control 1" };
            var control2 = new ControlElement { DisplayName = "Control 2" };
            template.Layout.EditableElements.Add(control1);
            template.Layout.EditableElements.Add(control2);
            _viewModel.SelectControl(control1);
            Assert.IsTrue(control1.IsSelected);
            Assert.IsFalse(control2.IsSelected);

            // Act
            _viewModel.SelectControl(control2);

            // Assert
            Assert.IsFalse(control1.IsSelected);
            Assert.IsTrue(control2.IsSelected);
            Assert.AreEqual(control2, _viewModel.SelectedControl);
        }

        [TestMethod]
        public void RemoveControl_ShouldRemoveControl()
        {
            // Arrange
            var template = new TemplateData
            {
                Layout = new LayoutMetadata
                {
                    EditableElements = new System.Collections.ObjectModel.ObservableCollection<ControlElement>()
                }
            };
            _viewModel.CurrentTemplate = template;
            var control = new ControlElement { DisplayName = "Test Control" };
            template.Layout.EditableElements.Add(control);
            _viewModel.SelectControl(control);
            Assert.AreEqual(1, template.Layout.EditableElements.Count);
            Assert.AreEqual(control, _viewModel.SelectedControl);

            // Act
            _viewModel.RemoveControlCommand.Execute(control);

            // Assert
            Assert.AreEqual(0, template.Layout.EditableElements.Count);
            Assert.IsNull(_viewModel.SelectedControl);
        }

        [TestMethod]
        public async Task LoadDefaultTemplateAsync_ShouldLoadDefaultTemplate()
        {
            // Arrange
            var expectedTemplate = new TemplateData { Name = "Default Template" };
            _templateServiceMock.Setup(service => service.LoadDefaultTemplateAsync()).ReturnsAsync(expectedTemplate);

            // Act
            await _viewModel.LoadDefaultTemplateCommand.ExecuteAsync(null);

            // Assert
            Assert.IsNotNull(_viewModel.CurrentTemplate);
            Assert.AreEqual(expectedTemplate, _viewModel.CurrentTemplate);
            _templateServiceMock.Verify(service => service.LoadDefaultTemplateAsync(), Times.Once);
        }

        [TestMethod]
        public void ChangePaperSize_ShouldChangePaperSize()
        {
            // Arrange
            var template = new TemplateData
            {
                Layout = new LayoutMetadata
                {
                    PaperWidth = 0,
                    PaperHeight = 0
                }
            };
            _viewModel.CurrentTemplate = template;
            string paperSize = "A4";

            // Act
            _viewModel.ChangePaperSizeCommand.Execute(paperSize);

            // Assert
            Assert.AreEqual(210, template.Layout.PaperWidth);
            Assert.AreEqual(297, template.Layout.PaperHeight);
            Assert.AreEqual(paperSize, _viewModel.PaperSize);
        }
    }
}