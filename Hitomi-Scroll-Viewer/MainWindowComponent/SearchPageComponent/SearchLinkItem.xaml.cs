using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace Hitomi_Scroll_Viewer.MainWindowComponent.SearchPageComponent {
    public sealed partial class SearchLinkItem : Grid {
        private readonly string _searchLink;
        private readonly string _displayText;
        public SearchLinkItem(
            string searchLink,
            string displayText,
            TypedEventHandler<XamlUICommand, ExecuteRequestedEventArgs> handler
            ) {
            _searchLink = searchLink;
            _displayText = displayText;
            InitializeComponent();
            DeleteCommand.ExecuteRequested += handler;
        }
    }
}
