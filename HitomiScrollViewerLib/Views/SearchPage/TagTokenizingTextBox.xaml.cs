using CommunityToolkit.WinUI.Controls;
using HitomiScrollViewerLib.ViewModels.SearchPage;

namespace HitomiScrollViewerLib.Views.SearchPage {
    public sealed partial class TagTokenizingTextBox : TokenizingTextBox {
        public TagTokenizingTextBoxVM ViewModel { get; set; }

        public TagTokenizingTextBox() {
            InitializeComponent();
        }
    }
}
