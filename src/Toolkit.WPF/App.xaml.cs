using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
using WpfApp = System.Windows.Application;

namespace Toolkit.WPF;

public partial class App : WpfApp
{
    private const string TessDataPath   = @"C:\Program Files\Tesseract-OCR\tessdata";
    private const string TesseractBin   = @"C:\Program Files\Tesseract-OCR\tesseract.exe";

    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddPresentation(TessDataPath, TesseractBin);
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();
        var window = _host.Services.GetRequiredService<MainWindow>();
        window.DataContext = _host.Services.GetRequiredService<Navigation.MainViewModel>();
        window.Show();
        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }
}
