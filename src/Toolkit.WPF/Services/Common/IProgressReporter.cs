using Toolkit.WPF.Models.Common;

namespace Toolkit.WPF.Services.Common;

public interface IProgressReporter
{
    void Report(BatchProgress progress);
}
