using HitomiScrollViewerLib.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Views {
    public partial class TFSSelector : Grid {
        public TFSelectorVM ViewModel { get; set; }

        public TFSSelector() {
            InitializeComponent();
        }
    }
}
