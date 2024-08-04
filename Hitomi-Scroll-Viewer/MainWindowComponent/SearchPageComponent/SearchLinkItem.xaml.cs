using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace Hitomi_Scroll_Viewer.MainWindowComponent.SearchPageComponent {
    public sealed partial class SearchLinkItem : Grid {
        internal readonly string SearchLink;
        private readonly string _displayText;
        public SearchLinkItem(
            string searchLink,
            string displayText,
            TypedEventHandler<XamlUICommand, ExecuteRequestedEventArgs> handler
        ) {
            SearchLink = searchLink;
            _displayText = displayText;
            InitializeComponent();
            DeleteCommand.ExecuteRequested += handler;
        }
    }
}
