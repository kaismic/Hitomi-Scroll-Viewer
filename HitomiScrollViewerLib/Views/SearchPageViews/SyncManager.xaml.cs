using HitomiScrollViewerLib.ViewModels.SearchPageVMs;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.Foundation;

namespace HitomiScrollViewerLib.Views.SearchPageViews {
    public sealed partial class SyncManager : Grid {
        public SyncManagerVM _viewModel;
        public SyncManagerVM ViewModel {
            get => _viewModel;
            set {
                _viewModel ??= value;
                _viewModel.ShowDialogRequested += ShowSyncContentDialog;
            }
        }

        public SyncManager() {
            InitializeComponent();
        }

        public async Task ShowSyncContentDialog(SyncContentDialogVM vm) {
            SyncContentDialog cd = new() {
                ViewModel = vm,
                XamlRoot = XamlRoot
            };
            await cd.ShowAsync();
        }
    }
}
