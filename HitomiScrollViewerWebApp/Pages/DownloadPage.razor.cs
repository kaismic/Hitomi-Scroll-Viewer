using HitomiScrollViewerWebApp.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Text.RegularExpressions;

namespace HitomiScrollViewerWebApp.Pages {
    public partial class DownloadPage {
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private DownloadConfigurationService DownloadConfigurationService { get; set; } = default!;
        [Inject] private DownloadManagerService DownloadManagerService { get; set; } = default!;

        private string _inputText = "";

        private bool UseParallelDownload {
            get => DownloadConfigurationService.Config.UseParallelDownload;
            set {
                if (DownloadConfigurationService.Config.UseParallelDownload == value) {
                    return;
                }
                DownloadConfigurationService.Config.UseParallelDownload = value;
                _ = DownloadConfigurationService.UpdateParallelDownload(value);
            }
        }

        private int ThreadNum {
            get => DownloadConfigurationService.Config.ThreadNum;
            set {
                if (DownloadConfigurationService.Config.ThreadNum == value) {
                    return;
                }
                DownloadConfigurationService.Config.ThreadNum = value;
                _ = DownloadConfigurationService.UpdateThreadNum(value);
            }
        }

        protected override async Task OnInitializedAsync() {
            if (!DownloadConfigurationService.IsLoaded) {
                await DownloadConfigurationService.Load();
                await DownloadManagerService.Load();
            }
        }

        [GeneratedRegex(@"\d{6,7}")] private static partial Regex IdPatternRegex();
        private void OnDownloadButtonClick() {
            MatchCollection matches = IdPatternRegex().Matches(_inputText);
            if (matches.Count == 0) {
                Snackbar.Add("No valid IDs or URLs found in the input text.", Severity.Error);
                return;
            }
            DownloadManagerService.CreateDownloads(matches.Select(m => int.Parse(m.Value)));
            _inputText = "";
        }
    }
}
