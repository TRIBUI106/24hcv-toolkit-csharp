using System.Windows;

namespace Toolkit.WPF.Common;

public sealed class WpfProgressReporter : IProgressReporter
{
    private readonly Action<BatchProgress> _callback;

    public WpfProgressReporter(Action<BatchProgress> callback)
    {
        _callback = callback;
    }

    public void Report(BatchProgress progress)
    {
        var app = System.Windows.Application.Current;
        if (app is null) return;

        if (app.Dispatcher.CheckAccess())
            _callback(progress);
        else
            app.Dispatcher.Invoke(() => _callback(progress));
    }
}
