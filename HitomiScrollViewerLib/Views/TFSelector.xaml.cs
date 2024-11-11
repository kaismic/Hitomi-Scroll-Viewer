using HitomiScrollViewerLib.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Views {
    public partial class TFSelector : Grid {
        public TFSelectorVM ViewModel { get; set; }

        public TFSelector() {
            InitializeComponent();
        }
    }
}
