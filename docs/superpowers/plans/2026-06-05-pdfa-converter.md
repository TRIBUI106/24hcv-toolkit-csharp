# PDF/A Converter Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a PDF/A-2b converter feature that recursively scans a root folder, converts un-converted PDFs to PDF/A-2b, preserves+optionally-overrides metadata, and marks converted files via Keywords so they are skipped on re-runs.

**Architecture:** New `PdfAConverter` feature follows the existing MVVM + DI pattern exactly (ViewModel → Service → Interface → Implementation). `IText7PdfAConverter` wraps iText7 8.x to produce PDF/A-2b output. A `sRGB2014.icc` ICC profile is embedded as a managed resource and streamed at runtime.

**Tech Stack:** iText7 8.x, iText7.bouncy-castle-adapter, existing CommunityToolkit.Mvvm, Microsoft.Extensions.Hosting DI, OpenCvSharp4 (not used here), PdfSharpCore (not used here).

---

## File Map

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `src/Toolkit.WPF/Resources/sRGB2014.icc` | Embedded ICC profile (binary) |
| Create | `src/Toolkit.WPF/Models/PdfAConverter/PdfAConversionResult.cs` | Result record per file |
| Create | `src/Toolkit.WPF/Models/PdfAConverter/PdfAConversionOptions.cs` | Options record (output mode, metadata overrides) |
| Create | `src/Toolkit.WPF/Services/PdfAConverter/IPdfAConverter.cs` | Interface |
| Create | `src/Toolkit.WPF/Services/PdfAConverter/IText7PdfAConverter.cs` | iText7 implementation |
| Create | `src/Toolkit.WPF/Services/PdfAConverter/PdfAConversionService.cs` | Batch orchestration, skip logic |
| Create | `src/Toolkit.WPF/Features/PdfAConverter/PdfAConverterViewModel.cs` | MVVM ViewModel |
| Create | `src/Toolkit.WPF/Features/PdfAConverter/PdfAConverterView.xaml` | WPF UserControl |
| Create | `src/Toolkit.WPF/Features/PdfAConverter/PdfAConverterView.xaml.cs` | Code-behind (empty) |
| Modify | `src/Toolkit.WPF/Toolkit.WPF.csproj` | Add iText7 packages + ICC EmbeddedResource |
| Modify | `src/Toolkit.WPF/DependencyInjection.cs` | Register new services + ViewModel |
| Modify | `src/Toolkit.WPF/Navigation/MainViewModel.cs` | Add navigation command |
| Modify | `src/Toolkit.WPF/MainWindow.xaml` | Add DataTemplate + nav button |

---

## Task 1: Add iText7 NuGet packages and embed ICC profile

**Files:**
- Modify: `src/Toolkit.WPF/Toolkit.WPF.csproj`
- Create: `src/Toolkit.WPF/Resources/sRGB2014.icc`

- [ ] **Step 1: Download sRGB2014.icc**

Download the ICC profile from the ICC website and place it at `src/Toolkit.WPF/Resources/sRGB2014.icc`.
You can also copy from Windows: `C:\Windows\System32\spool\drivers\color\sRGB Color Space Profile.icm` — rename it to `sRGB2014.icc`.

- [ ] **Step 2: Add packages and embed ICC resource**

Edit `src/Toolkit.WPF/Toolkit.WPF.csproj`, add inside the existing `<ItemGroup>` with PackageReferences:

```xml
<PackageReference Include="itext7" Version="8.0.5" />
<PackageReference Include="itext7.bouncy-castle-adapter" Version="8.0.5" />
```

Add a new `<ItemGroup>` for the resource:

```xml
<ItemGroup>
  <EmbeddedResource Include="Resources\sRGB2014.icc" />
</ItemGroup>
```

- [ ] **Step 3: Verify build**

```powershell
cd D:\Code\24hcv-toolkit-csharp
dotnet restore src/Toolkit.WPF/Toolkit.WPF.csproj
dotnet build src/Toolkit.WPF/Toolkit.WPF.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```powershell
git add src/Toolkit.WPF/Toolkit.WPF.csproj src/Toolkit.WPF/Resources/sRGB2014.icc
git commit -m "feat(pdfa): add iText7 packages and embed sRGB ICC profile"
```

---

## Task 2: Create Models

**Files:**
- Create: `src/Toolkit.WPF/Models/PdfAConverter/PdfAConversionResult.cs`
- Create: `src/Toolkit.WPF/Models/PdfAConverter/PdfAConversionOptions.cs`

- [ ] **Step 1: Create PdfAConversionOptions.cs**

```csharp
// src/Toolkit.WPF/Models/PdfAConverter/PdfAConversionOptions.cs
namespace Toolkit.WPF.Models.PdfAConverter;

public sealed record PdfAConversionOptions
{
    /// <summary>When true, overwrite source file. When false, write to OutputDirectory.</summary>
    public bool InPlace { get; init; } = true;

    public string OutputDirectory { get; init; } = string.Empty;

    // Optional metadata overrides — null means "keep original value"
    public string? TitleOverride   { get; init; }
    public string? AuthorOverride  { get; init; }
    public string? SubjectOverride { get; init; }
}
```

- [ ] **Step 2: Create PdfAConversionResult.cs**

```csharp
// src/Toolkit.WPF/Models/PdfAConverter/PdfAConversionResult.cs
namespace Toolkit.WPF.Models.PdfAConverter;

public enum ConversionStatus { Converted, Skipped, Error }

public sealed class PdfAConversionResult
{
    public string SourcePath      { get; init; } = string.Empty;
    public string OutputPath      { get; init; } = string.Empty;
    public ConversionStatus Status { get; init; }
    public long ProcessingMs      { get; init; }
    public string? ErrorMessage   { get; init; }
}
```

- [ ] **Step 3: Build**

```powershell
dotnet build src/Toolkit.WPF/Toolkit.WPF.csproj
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```powershell
git add src/Toolkit.WPF/Models/PdfAConverter/
git commit -m "feat(pdfa): add PdfAConversionOptions and PdfAConversionResult models"
```

---

## Task 3: Create IPdfAConverter interface and IText7PdfAConverter implementation

**Files:**
- Create: `src/Toolkit.WPF/Services/PdfAConverter/IPdfAConverter.cs`
- Create: `src/Toolkit.WPF/Services/PdfAConverter/IText7PdfAConverter.cs`

- [ ] **Step 1: Create IPdfAConverter.cs**

```csharp
// src/Toolkit.WPF/Services/PdfAConverter/IPdfAConverter.cs
using Toolkit.WPF.Models.PdfAConverter;

namespace Toolkit.WPF.Services.PdfAConverter;

public interface IPdfAConverter
{
    /// <summary>
    /// Convert a single PDF file to PDF/A-2b.
    /// Reads metadata from source, applies overrides, appends [PDFA-CONVERTED] to Keywords.
    /// </summary>
    Task<PdfAConversionResult> ConvertAsync(
        string sourcePath,
        string outputPath,
        PdfAConversionOptions options,
        CancellationToken ct = default);
}
```

- [ ] **Step 2: Create IText7PdfAConverter.cs**

```csharp
// src/Toolkit.WPF/Services/PdfAConverter/IText7PdfAConverter.cs
using iText.Commons.Bouncycastle.Cert;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Pdfa;
using System.Diagnostics;
using System.Reflection;
using Toolkit.WPF.Models.PdfAConverter;

namespace Toolkit.WPF.Services.PdfAConverter;

public sealed class IText7PdfAConverter : IPdfAConverter
{
    private const string ConvertedTag = "[PDFA-CONVERTED]";
    private const string FallbackFontName = "Helvetica";

    public Task<PdfAConversionResult> ConvertAsync(
        string sourcePath,
        string outputPath,
        PdfAConversionOptions options,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var sw = Stopwatch.StartNew();

        try
        {
            using var iccStream = Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream("Toolkit.WPF.Resources.sRGB2014.icc")
                ?? throw new InvalidOperationException("Embedded ICC profile not found. Ensure Resources/sRGB2014.icc is marked as EmbeddedResource.");

            var intent = new PdfOutputIntent(
                "Custom", "", "http://www.color.org",
                "sRGB IEC61966-2.1", iccStream);

            // Use temp file to avoid corrupting source on failure
            var tempPath = outputPath + ".tmp";

            using (var reader = new PdfReader(sourcePath))
            using (var writer = new PdfWriter(tempPath))
            using (var pdfADoc = new PdfADocument(writer, PdfAConformanceLevel.PDF_A_2B, intent))
            {
                // Copy all pages from source
                using var srcDoc = new PdfDocument(reader);
                srcDoc.CopyPagesTo(1, srcDoc.GetNumberOfPages(), pdfADoc);

                // Read original metadata
                var srcInfo   = srcDoc.GetDocumentInfo();
                var origTitle   = srcInfo.GetTitle();
                var origAuthor  = srcInfo.GetAuthor();
                var origSubject = srcInfo.GetSubject();
                var origKeywords = srcInfo.GetKeywords() ?? string.Empty;

                // Apply overrides
                var dstInfo = pdfADoc.GetDocumentInfo();
                dstInfo.SetTitle(options.TitleOverride   ?? origTitle   ?? string.Empty);
                dstInfo.SetAuthor(options.AuthorOverride  ?? origAuthor  ?? string.Empty);
                dstInfo.SetSubject(options.SubjectOverride ?? origSubject ?? string.Empty);

                // Append converted tag to Keywords
                var newKeywords = string.IsNullOrWhiteSpace(origKeywords)
                    ? ConvertedTag
                    : origKeywords.Contains(ConvertedTag)
                        ? origKeywords
                        : $"{origKeywords}; {ConvertedTag}";
                dstInfo.SetKeywords(newKeywords);

                // Embed fallback font for any unembedded fonts
                EmbedFallbackFontIfNeeded(pdfADoc);
            }

            // Replace output file atomically
            if (File.Exists(outputPath)) File.Delete(outputPath);
            File.Move(tempPath, outputPath);

            sw.Stop();
            return Task.FromResult(new PdfAConversionResult
            {
                SourcePath   = sourcePath,
                OutputPath   = outputPath,
                Status       = ConversionStatus.Converted,
                ProcessingMs = sw.ElapsedMilliseconds
            });
        }
        catch (Exception ex)
        {
            sw.Stop();
            return Task.FromResult(new PdfAConversionResult
            {
                SourcePath    = sourcePath,
                OutputPath    = outputPath,
                Status        = ConversionStatus.Error,
                ProcessingMs  = sw.ElapsedMilliseconds,
                ErrorMessage  = ex.Message
            });
        }
    }

    private static void EmbedFallbackFontIfNeeded(PdfADocument doc)
    {
        // iText7 PDF/A validation will flag unembedded fonts.
        // We set a document-level default font so new/copied content uses an embedded font.
        var font = PdfFontFactory.CreateFont(
            iText.IO.Font.Constants.StandardFonts.HELVETICA,
            PdfFontFactory.EmbeddingStrategy.FORCE_EMBEDDED);
        doc.SetDefaultFont(font);
    }
}
```

- [ ] **Step 3: Build**

```powershell
dotnet build src/Toolkit.WPF/Toolkit.WPF.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```powershell
git add src/Toolkit.WPF/Services/PdfAConverter/
git commit -m "feat(pdfa): add IPdfAConverter interface and IText7PdfAConverter implementation"
```

---

## Task 4: Create PdfAConversionService (batch orchestration + skip logic)

**Files:**
- Create: `src/Toolkit.WPF/Services/PdfAConverter/PdfAConversionService.cs`

- [ ] **Step 1: Create PdfAConversionService.cs**

```csharp
// src/Toolkit.WPF/Services/PdfAConverter/PdfAConversionService.cs
using iText.Kernel.Pdf;
using Toolkit.WPF.Models.Common;
using Toolkit.WPF.Models.PdfAConverter;
using Toolkit.WPF.Services.BatchRenaming;

namespace Toolkit.WPF.Services.PdfAConverter;

public sealed class PdfAConversionService
{
    private const string ConvertedTag = "[PDFA-CONVERTED]";

    private readonly IPdfAConverter _converter;
    private readonly IFileSystemService _fileSystem;

    public PdfAConversionService(IPdfAConverter converter, IFileSystemService fileSystem)
    {
        _converter  = converter;
        _fileSystem = fileSystem;
    }

    public async Task<OperationResult<IReadOnlyList<PdfAConversionResult>>> ConvertFolderAsync(
        string rootFolder,
        PdfAConversionOptions options,
        IProgressReporter progress,
        CancellationToken ct = default)
    {
        if (!_fileSystem.DirectoryExists(rootFolder))
            return OperationResult<IReadOnlyList<PdfAConversionResult>>.Failure(
                $"Directory not found: {rootFolder}");

        var files = _fileSystem.GetFiles(rootFolder, "*.pdf", recursive: true);
        if (files.Count == 0)
            return OperationResult<IReadOnlyList<PdfAConversionResult>>.Success(
                Array.Empty<PdfAConversionResult>());

        if (!options.InPlace && !string.IsNullOrWhiteSpace(options.OutputDirectory))
            Directory.CreateDirectory(options.OutputDirectory);

        var results   = new List<PdfAConversionResult>();
        var completed = 0;

        foreach (var filePath in files)
        {
            ct.ThrowIfCancellationRequested();
            var fileName = Path.GetFileName(filePath);

            // Skip already-converted files
            if (IsAlreadyConverted(filePath))
            {
                var skipped = new PdfAConversionResult
                {
                    SourcePath = filePath,
                    OutputPath = filePath,
                    Status     = ConversionStatus.Skipped
                };
                lock (results) results.Add(skipped);
                var s = Interlocked.Increment(ref completed);
                progress.Report(new BatchProgress(files.Count, s, fileName));
                continue;
            }

            var outputPath = options.InPlace
                ? filePath
                : Path.Combine(options.OutputDirectory, fileName);

            var result = await _converter.ConvertAsync(filePath, outputPath, options, ct);
            lock (results) results.Add(result);

            var count = Interlocked.Increment(ref completed);
            progress.Report(new BatchProgress(files.Count, count, fileName,
                result.Status == ConversionStatus.Error ? result.ErrorMessage : null));
        }

        return OperationResult<IReadOnlyList<PdfAConversionResult>>.Success(results);
    }

    private static bool IsAlreadyConverted(string filePath)
    {
        try
        {
            using var reader = new PdfReader(filePath);
            using var doc    = new PdfDocument(reader);
            var keywords = doc.GetDocumentInfo().GetKeywords() ?? string.Empty;
            return keywords.Contains(ConvertedTag);
        }
        catch
        {
            return false;
        }
    }
}
```

- [ ] **Step 2: Build**

```powershell
dotnet build src/Toolkit.WPF/Toolkit.WPF.csproj
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```powershell
git add src/Toolkit.WPF/Services/PdfAConverter/PdfAConversionService.cs
git commit -m "feat(pdfa): add PdfAConversionService with skip-if-converted logic"
```

---

## Task 5: Create PdfAConverterViewModel

**Files:**
- Create: `src/Toolkit.WPF/Features/PdfAConverter/PdfAConverterViewModel.cs`

- [ ] **Step 1: Create PdfAConverterViewModel.cs**

```csharp
// src/Toolkit.WPF/Features/PdfAConverter/PdfAConverterViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Toolkit.WPF.Common;
using Toolkit.WPF.Models.PdfAConverter;

namespace Toolkit.WPF.Features.PdfAConverter;

public sealed partial class PdfAConverterViewModel : ViewModelBase
{
    private readonly PdfAConversionService _service;
    private CancellationTokenSource? _cts;

    [ObservableProperty] private string _rootFolder       = string.Empty;
    [ObservableProperty] private bool   _inPlace          = true;
    [ObservableProperty] private string _outputFolder     = string.Empty;
    [ObservableProperty] private string _titleOverride    = string.Empty;
    [ObservableProperty] private string _authorOverride   = string.Empty;
    [ObservableProperty] private string _subjectOverride  = string.Empty;
    [ObservableProperty] private bool   _isRunning;
    [ObservableProperty] private double _progressPercent;
    [ObservableProperty] private string _statusMessage    = "Ready";

    public ObservableCollection<PdfAResultRow> Results { get; } = [];

    public PdfAConverterViewModel(PdfAConversionService service)
    {
        _service = service;
    }

    [RelayCommand]
    private void BrowseRoot()
    {
        var d = new System.Windows.Forms.FolderBrowserDialog { Description = "Select root folder containing PDFs" };
        if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            RootFolder = d.SelectedPath;
    }

    [RelayCommand]
    private void BrowseOutput()
    {
        var d = new System.Windows.Forms.FolderBrowserDialog { Description = "Select output folder" };
        if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            OutputFolder = d.SelectedPath;
    }

    [RelayCommand(CanExecute = nameof(CanConvert))]
    private async Task ConvertAsync()
    {
        _cts = new CancellationTokenSource();
        IsRunning = true;
        Results.Clear();
        StatusMessage = "Converting...";

        var options = new PdfAConversionOptions
        {
            InPlace         = InPlace,
            OutputDirectory = OutputFolder,
            TitleOverride   = string.IsNullOrWhiteSpace(TitleOverride)   ? null : TitleOverride,
            AuthorOverride  = string.IsNullOrWhiteSpace(AuthorOverride)  ? null : AuthorOverride,
            SubjectOverride = string.IsNullOrWhiteSpace(SubjectOverride) ? null : SubjectOverride
        };

        var reporter = new WpfProgressReporter(p =>
        {
            ProgressPercent = p.PercentComplete;
            StatusMessage   = $"[{p.CompletedItems}/{p.TotalItems}] {p.CurrentItemName}";
        });

        var result = await _service.ConvertFolderAsync(RootFolder, options, reporter, _cts.Token);

        IsRunning = false;

        if (result.IsSuccess)
        {
            foreach (var r in result.Value!)
                Results.Add(new PdfAResultRow(r));

            var converted = result.Value.Count(r => r.Status == ConversionStatus.Converted);
            var skipped   = result.Value.Count(r => r.Status == ConversionStatus.Skipped);
            var errors    = result.Value.Count(r => r.Status == ConversionStatus.Error);
            StatusMessage   = $"Done — {converted} converted, {skipped} skipped, {errors} error(s).";
            ProgressPercent = 100;
        }
        else
        {
            StatusMessage = $"Error: {result.ErrorMessage}";
        }
    }

    private bool CanConvert() =>
        !IsRunning &&
        !string.IsNullOrWhiteSpace(RootFolder) &&
        (InPlace || !string.IsNullOrWhiteSpace(OutputFolder));

    [RelayCommand]
    private void Cancel() => _cts?.Cancel();

    partial void OnIsRunningChanged(bool value)      => ConvertCommand.NotifyCanExecuteChanged();
    partial void OnRootFolderChanged(string value)   => ConvertCommand.NotifyCanExecuteChanged();
    partial void OnInPlaceChanged(bool value)        => ConvertCommand.NotifyCanExecuteChanged();
    partial void OnOutputFolderChanged(string value) => ConvertCommand.NotifyCanExecuteChanged();
}

public sealed class PdfAResultRow(PdfAConversionResult r)
{
    public string FileName    => Path.GetFileName(r.SourcePath);
    public string Status      => r.Status.ToString();
    public long   ProcessingMs => r.ProcessingMs;
    public string Error       => r.ErrorMessage ?? string.Empty;
}
```

- [ ] **Step 2: Build**

```powershell
dotnet build src/Toolkit.WPF/Toolkit.WPF.csproj
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```powershell
git add src/Toolkit.WPF/Features/PdfAConverter/PdfAConverterViewModel.cs
git commit -m "feat(pdfa): add PdfAConverterViewModel"
```

---

## Task 6: Create PdfAConverterView (XAML)

**Files:**
- Create: `src/Toolkit.WPF/Features/PdfAConverter/PdfAConverterView.xaml`
- Create: `src/Toolkit.WPF/Features/PdfAConverter/PdfAConverterView.xaml.cs`

- [ ] **Step 1: Create PdfAConverterView.xaml.cs (code-behind)**

```csharp
// src/Toolkit.WPF/Features/PdfAConverter/PdfAConverterView.xaml.cs
namespace Toolkit.WPF.Features.PdfAConverter;

public partial class PdfAConverterView : System.Windows.Controls.UserControl
{
    public PdfAConverterView()
    {
        InitializeComponent();
    }
}
```

- [ ] **Step 2: Create PdfAConverterView.xaml**

```xml
<UserControl x:Class="Toolkit.WPF.Features.PdfAConverter.PdfAConverterView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <ScrollViewer VerticalScrollBarVisibility="Auto">
        <StackPanel Margin="20" MaxWidth="1000">

            <!-- Input / Output -->
            <Border Style="{StaticResource Card}" Margin="0,0,0,12">
                <StackPanel>
                    <Grid Margin="0,0,0,10">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>
                        <StackPanel Grid.Column="0" Margin="0,0,8,0">
                            <TextBlock Text="Root Folder (contains PDFs)" Style="{StaticResource FieldLabel}"/>
                            <TextBox Text="{Binding RootFolder, UpdateSourceTrigger=PropertyChanged}"/>
                        </StackPanel>
                        <Button Grid.Column="1" Content="Browse" Style="{StaticResource SecondaryButton}"
                                Command="{Binding BrowseRootCommand}" VerticalAlignment="Bottom"/>
                    </Grid>

                    <CheckBox Content="In-place (overwrite source files)" IsChecked="{Binding InPlace}" Margin="0,0,0,8"/>

                    <Grid>
                        <Grid.Style>
                            <Style TargetType="Grid">
                                <Setter Property="Visibility" Value="Collapsed"/>
                                <Style.Triggers>
                                    <DataTrigger Binding="{Binding InPlace}" Value="False">
                                        <Setter Property="Visibility" Value="Visible"/>
                                    </DataTrigger>
                                </Style.Triggers>
                            </Style>
                        </Grid.Style>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>
                        <StackPanel Grid.Column="0" Margin="0,0,8,0">
                            <TextBlock Text="Output Folder" Style="{StaticResource FieldLabel}"/>
                            <TextBox Text="{Binding OutputFolder, UpdateSourceTrigger=PropertyChanged}"/>
                        </StackPanel>
                        <Button Grid.Column="1" Content="Browse" Style="{StaticResource SecondaryButton}"
                                Command="{Binding BrowseOutputCommand}" VerticalAlignment="Bottom"/>
                    </Grid>
                </StackPanel>
            </Border>

            <!-- Metadata overrides -->
            <Border Style="{StaticResource Card}" Margin="0,0,0,12">
                <StackPanel>
                    <TextBlock Text="Metadata Overrides (leave blank to keep original)" Style="{StaticResource FieldLabel}" FontSize="13" Margin="0,0,0,10"/>
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <StackPanel Grid.Column="0" Margin="0,0,8,0">
                            <TextBlock Text="Title" Style="{StaticResource FieldLabel}"/>
                            <TextBox Text="{Binding TitleOverride, UpdateSourceTrigger=PropertyChanged}"/>
                        </StackPanel>
                        <StackPanel Grid.Column="1" Margin="0,0,8,0">
                            <TextBlock Text="Author" Style="{StaticResource FieldLabel}"/>
                            <TextBox Text="{Binding AuthorOverride, UpdateSourceTrigger=PropertyChanged}"/>
                        </StackPanel>
                        <StackPanel Grid.Column="2">
                            <TextBlock Text="Subject" Style="{StaticResource FieldLabel}"/>
                            <TextBox Text="{Binding SubjectOverride, UpdateSourceTrigger=PropertyChanged}"/>
                        </StackPanel>
                    </Grid>
                </StackPanel>
            </Border>

            <!-- Actions -->
            <Border Style="{StaticResource Card}" Margin="0,0,0,12">
                <StackPanel>
                    <StackPanel Orientation="Horizontal" Margin="0,0,0,10">
                        <Button Content="Convert to PDF/A-2b" Style="{StaticResource PrimaryButton}"
                                Command="{Binding ConvertCommand}" Margin="0,0,8,0"/>
                        <Button Content="Cancel" Style="{StaticResource SecondaryButton}"
                                Command="{Binding CancelCommand}"/>
                    </StackPanel>
                    <ProgressBar Value="{Binding ProgressPercent}" Maximum="100" Height="4" Margin="0,0,0,4"
                                 Visibility="{Binding IsRunning, Converter={StaticResource BoolToVisibility}}"/>
                    <TextBlock Text="{Binding StatusMessage}" FontSize="12" Foreground="#64748B"/>
                </StackPanel>
            </Border>

            <!-- Results -->
            <Border Style="{StaticResource Card}">
                <DataGrid ItemsSource="{Binding Results}" AutoGenerateColumns="False"
                          CanUserAddRows="False" HeadersVisibility="Column"
                          GridLinesVisibility="Horizontal" BorderThickness="0"
                          Background="Transparent" MaxHeight="420">
                    <DataGrid.Columns>
                        <DataGridTextColumn Header="File"       Binding="{Binding FileName}"     Width="*"   IsReadOnly="True"/>
                        <DataGridTextColumn Header="Status"     Binding="{Binding Status}"        Width="100" IsReadOnly="True"/>
                        <DataGridTextColumn Header="Time (ms)"  Binding="{Binding ProcessingMs}"  Width="80"  IsReadOnly="True"/>
                        <DataGridTextColumn Header="Error"      Binding="{Binding Error}"          Width="300" IsReadOnly="True"/>
                    </DataGrid.Columns>
                </DataGrid>
            </Border>

        </StackPanel>
    </ScrollViewer>
</UserControl>
```

- [ ] **Step 3: Build**

```powershell
dotnet build src/Toolkit.WPF/Toolkit.WPF.csproj
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```powershell
git add src/Toolkit.WPF/Features/PdfAConverter/
git commit -m "feat(pdfa): add PdfAConverterView XAML and code-behind"
```

---

## Task 7: Wire up DI, Navigation, and MainWindow

**Files:**
- Modify: `src/Toolkit.WPF/DependencyInjection.cs`
- Modify: `src/Toolkit.WPF/Navigation/MainViewModel.cs`
- Modify: `src/Toolkit.WPF/MainWindow.xaml`

- [ ] **Step 1: Register services in DependencyInjection.cs**

Add usings at top:
```csharp
using Toolkit.WPF.Features.PdfAConverter;
using Toolkit.WPF.Services.PdfAConverter;
```

Inside `AddPresentation`, add after the PDF block:
```csharp
// PDF/A Conversion
services.AddTransient<IPdfAConverter, IText7PdfAConverter>();
services.AddTransient<PdfAConversionService>();
```

Add ViewModel registration after the other ViewModels:
```csharp
services.AddSingleton<PdfAConverterViewModel>();
```

- [ ] **Step 2: Add navigation in MainViewModel.cs**

Add using:
```csharp
using Toolkit.WPF.Features.PdfAConverter;
```

Add field:
```csharp
private readonly PdfAConverterViewModel _pdfAConverter;
```

Update constructor signature and body:
```csharp
public MainViewModel(
    PdfAnalysisViewModel pdfAnalysis,
    BatchRenamingViewModel batchRenaming,
    ImagePreprocessingViewModel imagePreprocessing,
    OcrViewModel ocr,
    OcrTrainingViewModel ocrTraining,
    PdfAConverterViewModel pdfAConverter)
{
    _pdfAnalysis        = pdfAnalysis;
    _batchRenaming      = batchRenaming;
    _imagePreprocessing = imagePreprocessing;
    _ocr                = ocr;
    _ocrTraining        = ocrTraining;
    _pdfAConverter      = pdfAConverter;
    _currentPage        = pdfAnalysis;
}
```

Add navigation command:
```csharp
[RelayCommand] private void NavigateToPdfAConverter() { CurrentPage = _pdfAConverter; CurrentPageTitle = "PDF/A Converter"; }
```

- [ ] **Step 3: Add DataTemplate and nav button in MainWindow.xaml**

Add namespace at top of Window element:
```xml
xmlns:pdfa="clr-namespace:Toolkit.WPF.Features.PdfAConverter"
```

Add DataTemplate inside `<Window.Resources>`:
```xml
<DataTemplate DataType="{x:Type pdfa:PdfAConverterViewModel}">
    <pdfa:PdfAConverterView/>
</DataTemplate>
```

Add nav button inside the sidebar `<StackPanel>` after the OCR Training button:
```xml
<Button Content="📋  PDF/A Converter"
        Style="{StaticResource NavButton}"
        Command="{Binding NavigateToPdfAConverterCommand}"/>
```

- [ ] **Step 4: Build**

```powershell
dotnet build src/Toolkit.WPF/Toolkit.WPF.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Commit**

```powershell
git add src/Toolkit.WPF/DependencyInjection.cs src/Toolkit.WPF/Navigation/MainViewModel.cs src/Toolkit.WPF/MainWindow.xaml
git commit -m "feat(pdfa): wire PDF/A Converter into DI, navigation, and MainWindow"
```

---

## Task 8: Manual smoke test

- [ ] **Step 1: Run the app**

```powershell
dotnet run --project src/Toolkit.WPF/Toolkit.WPF.csproj
```

- [ ] **Step 2: Verify navigation**

Click "PDF/A Converter" in sidebar — page loads with 3 cards (Input/Output, Metadata, Results).

- [ ] **Step 3: Test in-place conversion**

1. Select a root folder containing at least one PDF
2. Leave "In-place" checked
3. Optionally fill Author override
4. Click "Convert to PDF/A-2b"
5. Expected: Results grid shows file with Status=Converted, ProcessingMs > 0

- [ ] **Step 4: Verify skip logic**

1. Without changing folder, click "Convert to PDF/A-2b" again
2. Expected: Same file now shows Status=Skipped (Keywords contains `[PDFA-CONVERTED]`)

- [ ] **Step 5: Verify metadata**

Open converted PDF in any PDF viewer → File Properties → Keywords contains `[PDFA-CONVERTED]`.

- [ ] **Step 6: Final commit**

```powershell
git add -A
git commit -m "feat(pdfa): complete PDF/A-2b converter feature"
```
