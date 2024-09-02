using CommunityToolkit.WinUI.Controls;
using HitomiScrollViewerLib.ViewModels.SearchPageVMs;

namespace HitomiScrollViewerLib.Views.SearchPageViews {
    public sealed partial class TagTokenizingTextBox : TokenizingTextBox {
        public TagTokenizingTextBoxVM ViewModel { get; set; }

        public TagTokenizingTextBox() {
            InitializeComponent();
        }
    }
}
