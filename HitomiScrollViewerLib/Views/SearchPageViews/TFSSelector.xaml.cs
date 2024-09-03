using HitomiScrollViewerLib.ViewModels.SearchPageVMs;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Views.SearchPageViews {
    public partial class TFSSelector : Grid {
        public TFSSelectorVM ViewModel { get; set; }

        public TFSSelector() {
            InitializeComponent();
        }
    }
}
