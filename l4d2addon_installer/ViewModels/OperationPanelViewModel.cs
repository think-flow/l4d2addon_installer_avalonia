using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using l4d2addon_installer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace l4d2addon_installer.ViewModels;

public partial class OperationPanelViewModel : ViewModelBase
{
    private readonly LoggerService _logger = null!;
    private readonly VpkFileService _vpkFileService = null!;

    [ObservableProperty]
    private bool _showLoading;

    public OperationPanelViewModel()
    {
#if DEBUG
        if (IsDesignMode) return;
#endif

        _vpkFileService = Services.GetRequiredService<VpkFileService>();
        _logger = Services.GetRequiredService<LoggerService>();
    }

    [RelayCommand]
    private async Task OpenAddonsFolder()
    {
        try
        {
            await _vpkFileService.OpenAddonsFloderAsync();
        }
        catch (ServiceException e)
        {
            _logger.LogError(e.Message);
        }
    }

    [RelayCommand]
    private async Task OpenGameFolder()
    {
        try
        {
            await _vpkFileService.OpenGameFolderAsync();
        }
        catch (ServiceException e)
        {
            _logger.LogError(e.Message);
        }
    }

    [RelayCommand]
    private async Task OpenDownloadsFolder()
    {
        try
        {
            await _vpkFileService.OpenDownloadsFolderAsync();
        }
        catch (ServiceException e)
        {
            _logger.LogError(e.Message);
        }
    }

    [RelayCommand]
    private async Task OpenRecycleBinFolder()
    {
        try
        {
            await _vpkFileService.OpenRecycleBinFolderAsync();
        }
        catch (ServiceException e)
        {
            _logger.LogError(e.Message);
        }
    }

    [RelayCommand]
    private async Task StartMapinfoProgram()
    {
        try
        {
            await _vpkFileService.StartMapinfoProgramAsync();
        }
        catch (ServiceException e)
        {
            _logger.LogError(e.Message);
        }
    }

    [RelayCommand]
    private async Task StartGame()
    {
        try
        {
            await _vpkFileService.StartGameAsync();
        }
        catch (ServiceException e)
        {
            _logger.LogError(e.Message);
        }
    }
}
