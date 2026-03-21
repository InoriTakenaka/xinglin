using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using xinglin.Services.Data;
using xinglin.Services.Pdf;
using xinglin.ViewModels;

namespace xinglin;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static ServiceProvider ServiceProvider { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        ServiceProvider = serviceCollection.BuildServiceProvider();
    }

    private void ConfigureServices(ServiceCollection services)
    {
        // 注册服务
        services.AddSingleton<ILoggerService, ConsoleLoggerService>();
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();
        services.AddSingleton<ITemplateService, TemplateService>();
        services.AddSingleton<IDataService, DataService>();
        services.AddSingleton<IPdfPrintService, PdfPrintService>();

        // 注册视图模型
        services.AddTransient<TemplateEditorViewModel>();
        services.AddTransient<ToolboxViewModel>();
        services.AddTransient<TemplateViewerViewModel>();
        services.AddTransient<DataEntryViewModel>();
    }
}

