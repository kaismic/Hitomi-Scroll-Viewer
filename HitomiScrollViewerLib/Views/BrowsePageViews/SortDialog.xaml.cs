using HitomiScrollViewerLib.ViewModels.BrowsePageVMs;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Specialized;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Views.BrowsePageViews {
    public sealed partial class SortDialog : ContentDialog {
        public SortDialogVM ViewModel { get; }
        public SortDialog(SortDialogVM vm) {
            InitializeComponent();
            CloseButtonText = TEXT_CLOSE;
            vm.ActiveSortItemVMs.CollectionChanged += ActiveSortItemVMs_CollectionChanged;
            vm.InactiveSortItemVMs.CollectionChanged += InactiveSortItemVMs_CollectionChanged;
            ViewModel = vm;
        }

        private void InactiveSortItemVMs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            InactiveItemsView.ItemsSource = null;
            InactiveItemsView.ItemsSource = ViewModel.InactiveSortItemVMs;
        }

        private void ActiveSortItemVMs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            ActiveItemsView.ItemsSource = null;
            ActiveItemsView.ItemsSource = ViewModel.ActiveSortItemVMs;
        }

        private void CandidateSortItem_Clicked(object _0, ItemClickEventArgs e) {
            (e.ClickedItem as SortItemVM).InvokeAddRequested();
            CandidateSortItemsFlyout.Hide();
        }
    }
}
