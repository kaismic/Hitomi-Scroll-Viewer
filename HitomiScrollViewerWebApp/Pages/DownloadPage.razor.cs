﻿using HitomiScrollViewerWebApp.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Text.RegularExpressions;

namespace HitomiScrollViewerWebApp.Pages {
    public partial class DownloadPage {
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private DownloadConfigurationService DownloadConfigurationService { get; set; } = default!;
        [Inject] private DownloadClientManagerService DownloadManager { get; set; } = default!;

        private string _inputText = "";

        private async Task OnParallelDownloadChanged(bool value) {
            DownloadConfigurationService.Config.UseParallelDownload = value;
            await DownloadConfigurationService.UpdateParallelDownload(value);
        }
        
        private async Task OnThreadNumChanged(int value) {
            DownloadConfigurationService.Config.ThreadNum = value;
            await DownloadConfigurationService.UpdateThreadNum(value);
        }

        protected override async Task OnInitializedAsync() {
            await DownloadConfigurationService.Load();
            DownloadManager.DownloadPageStateHasChanged = () => InvokeAsync(StateHasChanged);
            if (!DownloadManager.IsHubConnectionOpen) {
                DownloadManager.OpenHubConnection();
            }
        }

        [GeneratedRegex(@"\d{6,7}")] private static partial Regex IdPatternRegex();
        private void OnDownloadButtonClick() {
            MatchCollection matches = IdPatternRegex().Matches(_inputText);
            if (matches.Count == 0) {
                Snackbar.Add("No valid IDs or URLs found in the input text.", Severity.Error);
                return;
            }
            DownloadManager.AddDownloads(matches.Select(m => int.Parse(m.Value)));
            _inputText = "";
        }
    }
}
