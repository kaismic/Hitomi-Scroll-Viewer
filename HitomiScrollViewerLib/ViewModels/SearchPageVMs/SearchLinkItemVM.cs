using Microsoft.UI.Xaml.Input;

namespace HitomiScrollViewerLib.ViewModels.SearchPageVMs {
    public class SearchLinkItemVM(string searchLink, string displayText) {
        public string SearchLink { get; } = searchLink;
        public string DisplayText { get; } = displayText;
        public StandardUICommand DeleteCommand { get; } = new(StandardUICommandKind.Delete);
    }
}