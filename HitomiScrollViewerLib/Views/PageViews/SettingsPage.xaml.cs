using HitomiScrollViewerLib.Models;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Views.PageViews {
    public sealed partial class SettingsPage : Page {
        public CommonSettings CommonSettings { get; } = CommonSettings.Main;

        public SettingsPage() {
            InitializeComponent();
        }
    }
}
