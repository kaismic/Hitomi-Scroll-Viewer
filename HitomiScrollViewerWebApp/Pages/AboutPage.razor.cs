using HitomiScrollViewerData;
using HitomiScrollViewerWebApp.Layout;
using HitomiScrollViewerWebApp.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Timers;

namespace HitomiScrollViewerWebApp.Pages {
    public partial class AboutPage : IDisposable {
        [Inject] AppConfigurationService AppConfigurationService { get; set; } = default!;
        [Inject] ISnackbar Snackbar { get; set; } = default!;

        private bool _isCheckingUpdate = false;
        private const int UPDATE_CHECK_LIMIT = 3;
        private readonly TimeOnly UPDATE_CHECK_WAIT_TIME = new(0, 10);
        private int _updateCheckCount = 0;
        private TimeOnly _remainingTime;
        private readonly System.Timers.Timer _updateCheckLimitTimer = new(TimeSpan.FromSeconds(1));

        protected override void OnInitialized() {
            _updateCheckLimitTimer.Enabled = false;
            _updateCheckLimitTimer.Elapsed += UpdateRemainingTime;
        }

        private async Task CheckUpdate() {
            if (_updateCheckCount >= UPDATE_CHECK_LIMIT) {
                return;
            }
            _updateCheckCount++;
            _isCheckingUpdate = true;
            AppInfo info = await AppConfigurationService.GetAppStatus();
            if (info.NewVersion == null) {
                Snackbar.Add("Failed to get new update information. " + info.ErrorMessage, Severity.Error);
            } else {
                if (info.NewVersion.Major > AppConfigurationService.CURRENT_APP_VERSION.Major||
                    info.NewVersion.Minor > AppConfigurationService.CURRENT_APP_VERSION.Minor ||
                    info.NewVersion.Build > AppConfigurationService.CURRENT_APP_VERSION.Build) {
                    Snackbar.Add(
                        $"A new version is available: {info.NewVersion.Major}.{info.NewVersion.Minor}.{info.NewVersion.Build}",
                        Severity.Success,
                        MainLayout.DEFAULT_SNACKBAR_OPTIONS
                    );
                } else {
                    Snackbar.Add($"Your app is up to date.", Severity.Info, MainLayout.DEFAULT_SNACKBAR_OPTIONS);
                }
            }
            _isCheckingUpdate = false;
            if (_updateCheckCount >= UPDATE_CHECK_LIMIT) {
                _remainingTime = UPDATE_CHECK_WAIT_TIME;
                _updateCheckLimitTimer.Start();
            }
        }

        private void UpdateRemainingTime(object? sender, ElapsedEventArgs e) {
            _remainingTime = _remainingTime.Add(TimeSpan.FromSeconds(-1));
            if (_remainingTime.Ticks <= 0) {
                _updateCheckLimitTimer.Stop();
                _updateCheckCount = 0;
            }
            StateHasChanged();
        }

        public void Dispose() {
            GC.SuppressFinalize(this);
            _updateCheckLimitTimer?.Dispose();
        }
    }
}
