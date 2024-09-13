using HitomiScrollViewerLib.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Views {
    public sealed partial class TagItemsRepeater : Grid {
        public TagItemsRepeaterVM ViewModel { get; set; }
        public TagItemsRepeater() {
            InitializeComponent();
        }
    }
}
