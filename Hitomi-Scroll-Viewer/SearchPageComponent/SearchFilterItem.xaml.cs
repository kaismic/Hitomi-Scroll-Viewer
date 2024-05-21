using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace Hitomi_Scroll_Viewer.SearchPageComponent {
    public sealed partial class SearchFilterItem : StackPanel
    {
        public readonly string TagName;
        public SearchFilterItem(string tagName, TypedEventHandler<XamlUICommand, ExecuteRequestedEventArgs> handler)
        {
            TagName = tagName;
            InitializeComponent();
            DeleteCommand.ExecuteRequested += handler;
        }
    }
}
