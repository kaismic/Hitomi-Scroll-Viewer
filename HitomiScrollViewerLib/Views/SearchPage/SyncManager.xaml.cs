using HitomiScrollViewerLib.ViewModels.SearchPage;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Views.SearchPage {
    public sealed partial class SyncManager : Grid {
        public SyncManagerVM ViewModel => new();

        public SyncManager() {
            InitializeComponent();
        }
    }
}
