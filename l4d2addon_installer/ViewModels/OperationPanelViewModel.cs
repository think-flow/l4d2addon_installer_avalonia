using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using l4d2addon_installer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace l4d2addon_installer.ViewModels;

public partial class OperationPanelViewModel : ServiceViewModelBase
{
    private readonly LoggerService _logger;
    private readonly VpkFileService _vpkFileService;

    [ObservableProperty]
    private bool _isCoverd;

    [ObservableProperty]
    private bool _showLoading;

    [Obsolete("专供设计器调用", true)]
    public OperationPanelViewModel() : base(null!)
    {
    }

    public OperationPanelViewModel(IServiceProvider provider)
        : base(provider)
    {
        _vpkFileService = provider.GetRequiredService<VpkFileService>();
        _logger = provider.GetRequiredService<LoggerService>();
    }

    [RelayCommand]
    private async Task OpenAddonsFloder()
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
