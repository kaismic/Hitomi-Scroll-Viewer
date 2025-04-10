using HitomiScrollViewerWebApp.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Text.RegularExpressions;

namespace HitomiScrollViewerWebApp.Pages {
    public partial class DownloadPage {
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private PageConfigurationService PageConfigurationService { get; set; } = default!;
        [Inject] private DownloadService DownloadService { get; set; } = default!;

        private string _inputText = "";

        private bool UseParallelDownload {
            get => PageConfigurationService.DownloadConfiguration.UseParallelDownload;
            set {
                if (PageConfigurationService.DownloadConfiguration.UseParallelDownload == value) {
                    return;
                }
                PageConfigurationService.DownloadConfiguration.UseParallelDownload = value;
                _ = DownloadService.UpdateParallelDownload(PageConfigurationService.DownloadConfiguration.Id, value);
            }
        }

        private int ThreadNum {
            get => PageConfigurationService.DownloadConfiguration.ThreadNum;
            set {
                if (PageConfigurationService.DownloadConfiguration.ThreadNum == value) {
                    return;
                }
                PageConfigurationService.DownloadConfiguration.ThreadNum = value;
                _ = DownloadService.UpdateThreadNum(PageConfigurationService.DownloadConfiguration.Id, value);
            }
        }

        protected override async Task OnInitializedAsync() {
            if (!PageConfigurationService.IsDownloadConfigurationLoaded) {
                PageConfigurationService.IsDownloadConfigurationLoaded = true;
                PageConfigurationService.DownloadConfiguration = await DownloadService.GetConfigurationAsync();
            }
        }

        [GeneratedRegex(@"\d{6,7}")] private static partial Regex IdPatternRegex();
        private void OnDownloadButtonClick() {
            MatchCollection matches = IdPatternRegex().Matches(_inputText);
            if (matches.Count == 0) {
                Snackbar.Add("No valid IDs or URLs found in the input text.", Severity.Error);
                return;
            }
            DownloadService.CreateDownloads(matches.Select(m => int.Parse(m.Value)));
            _inputText = "";
        }
    }
}
