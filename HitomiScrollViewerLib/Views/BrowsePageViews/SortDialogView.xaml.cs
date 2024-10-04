using HitomiScrollViewerLib.ViewModels.BrowsePageVMs;
using Microsoft.UI.Xaml.Controls;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Views.BrowsePageViews {
    public sealed partial class SortDialog : ContentDialog {
        public SortDialogVM ViewModel { get; }
        public SortDialog(SortDialogVM vm) {
            InitializeComponent();
            CloseButtonText = TEXT_CLOSE;
            ViewModel = vm;
        }

        private void CandidateSortItem_Clicked(object _0, ItemClickEventArgs e) {
            (e.ClickedItem as SortItemVM).InvokeAddRequested();
            CandidateSortItemsFlyout.Hide();
        }
    }
}
