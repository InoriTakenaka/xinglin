using System;
using System.Windows;
using xinglin.Models.CoreEntities;
using xinglin.Services.Pdf;

namespace xinglin.Views
{
    public partial class PrintPreviewWindow : Window
    {
        private readonly ReportInstance _report;
        private readonly TemplateData _template;
        private readonly IPdfPrintService _printService;

        public PrintPreviewWindow(ReportInstance report, TemplateData template,
                                  IPdfPrintService printService)
        {
            InitializeComponent();
            _report = report;
            _template = template;
            _printService = printService;
            PreviewView.SetReportInstance(report, template);
        }

        private async void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            PrintButton.IsEnabled = false;
            try
            {
                var path = await _printService.GeneratePdfAsync(_report, _template);
                await _printService.PrintAsync(path);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打印失败：{ex.Message}", "错误",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                PrintButton.IsEnabled = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
