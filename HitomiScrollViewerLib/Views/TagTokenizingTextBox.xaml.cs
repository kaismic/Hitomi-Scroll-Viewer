using CommunityToolkit.WinUI.Controls;
using HitomiScrollViewerLib.ViewModels;

namespace HitomiScrollViewerLib.Views {
    public sealed partial class TagTokenizingTextBox : TokenizingTextBox {
        public TagTokenizingTextBoxVM ViewModel { get; set; }

        public TagTokenizingTextBox() {
            InitializeComponent();
        }
    }
}
