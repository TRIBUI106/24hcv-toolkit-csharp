using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Toolkit.WPF.Common;
using Toolkit.WPF.Features.PdfAnalysis;
using Toolkit.WPF.Features.BatchRenaming;
using Toolkit.WPF.Features.ImagePreprocessing;
using Toolkit.WPF.Features.Ocr;
using Toolkit.WPF.Features.OcrTraining;

namespace Toolkit.WPF.Navigation;

public sealed partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private ViewModelBase _currentPage;
    [ObservableProperty] private string _currentPageTitle = "PDF Analysis";

    private readonly PdfAnalysisViewModel _pdfAnalysis;
    private readonly BatchRenamingViewModel _batchRenaming;
    private readonly ImagePreprocessingViewModel _imagePreprocessing;
    private readonly OcrViewModel _ocr;
    private readonly OcrTrainingViewModel _ocrTraining;

    public MainViewModel(
        PdfAnalysisViewModel pdfAnalysis,
        BatchRenamingViewModel batchRenaming,
        ImagePreprocessingViewModel imagePreprocessing,
        OcrViewModel ocr,
        OcrTrainingViewModel ocrTraining)
    {
        _pdfAnalysis = pdfAnalysis;
        _batchRenaming = batchRenaming;
        _imagePreprocessing = imagePreprocessing;
        _ocr = ocr;
        _ocrTraining = ocrTraining;
        _currentPage = pdfAnalysis;
    }

    [RelayCommand] private void NavigateToPdfAnalysis()    { CurrentPage = _pdfAnalysis;       CurrentPageTitle = "PDF Analysis"; }
    [RelayCommand] private void NavigateToBatchRenaming()  { CurrentPage = _batchRenaming;      CurrentPageTitle = "Batch Renaming"; }
    [RelayCommand] private void NavigateToPreprocessing()  { CurrentPage = _imagePreprocessing; CurrentPageTitle = "Image Preprocessing"; }
    [RelayCommand] private void NavigateToOcr()            { CurrentPage = _ocr;                CurrentPageTitle = "OCR Engine"; }
    [RelayCommand] private void NavigateToOcrTraining()    { CurrentPage = _ocrTraining;        CurrentPageTitle = "OCR Training"; }
}
