using CommunityToolkit.Mvvm.ComponentModel;

namespace Toolkit.WPF.Common;

public abstract class ViewModelBase : ObservableObject, IDisposable
{
    private bool _disposed;

    public virtual void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
