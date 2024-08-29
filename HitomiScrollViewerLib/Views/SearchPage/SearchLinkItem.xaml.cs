using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace HitomiScrollViewerLib.Views.SearchPage {
    public sealed partial class SearchLinkItem : Grid {
        public readonly string SearchLink;
        private string DisplayText { get; }
        public SearchLinkItem(
            string searchLink,
            string displayText,
            TypedEventHandler<XamlUICommand, ExecuteRequestedEventArgs> handler
        ) {
            SearchLink = searchLink;
            DisplayText = displayText;
            InitializeComponent();
            DeleteCommand.ExecuteRequested += handler;
        }
    }
}
