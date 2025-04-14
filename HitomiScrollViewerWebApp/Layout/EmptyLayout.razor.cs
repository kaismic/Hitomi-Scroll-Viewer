using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HitomiScrollViewerWebApp.Layout {
    public partial class EmptyLayout : LayoutComponentBase {
        private MudThemeProvider _mudThemeProvider = null!;
        private readonly MudTheme _theme = new();
        private bool _isDarkMode;

        protected override async Task OnAfterRenderAsync(bool firstRender) {
            if (firstRender) {
                _isDarkMode = await _mudThemeProvider.GetSystemPreference();
            }
        }
    }
}
