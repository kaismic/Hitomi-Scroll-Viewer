using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HitomiScrollViewerLib.ViewModels.SearchPageVMs;
using HitomiScrollViewerLib.Views.PageViews;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.ViewModels.PageVMs {
    public partial class SearchPageVM : DQObservableObject {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(SearchPage).Name);
        private static readonly Range GALLERY_ID_LENGTH_RANGE = 6..7;

        private static SearchPageVM _main;
        public static SearchPageVM Main => _main ??= new() ;

        public TagFilterEditorVM TagFilterEditorVM { get; } = new();
        public ObservableCollection<SearchFilterVM> SearchFilterVMs { get; } = [];
        public DownloadManagerVM DownloadManagerVM { get; } = DownloadManagerVM.Main;
        public SyncManagerVM SyncManagerVM { get; } = new();

        [ObservableProperty]
        private string _downloadInputText = "";

        private SearchPageVM() {
            HyperlinkCreateButtonCommand = new RelayCommand(
                HyperlinkCreateButton_Clicked,
                () => TagFilterEditorVM.AnyFilterSelected
            );
            DownloadButtonCommand = new RelayCommand(
                ExecuteDownloadButtonCommand,
                () => DownloadInputText.Length != 0
            );
        }

        public ICommand HyperlinkCreateButtonCommand { get; }

        public void HyperlinkCreateButton_Clicked() {
            SearchFilterVM vm = TagFilterEditorVM.GetSearchFilterVM();
            if (vm != null) {
                // copy link to clipboard
                vm.DeleteCommand.Command = new RelayCommand<SearchFilterVM>((arg) => {
                    SearchFilterVMs.Remove(arg);
                });
                SearchFilterVMs.Add(vm);
                DataPackage dataPackage = new() {
                    RequestedOperation = DataPackageOperation.Copy
                };
                dataPackage.SetText(vm.SearchLink);
                Clipboard.SetContent(dataPackage);
            }
        }

        public ICommand DownloadButtonCommand { get; }

        /*
         * Environment.NewLine cannot be used alone as TextBox.Text separator
         * because of this TextBox bug which somehow converts \r\n to \r and it's still not fixed...
         * https://github.com/microsoft/microsoft-ui-xaml/issues/1826
         * https://stackoverflow.com/questions/35138047/uwp-textbox-selectedtext-changes-r-n-to-r
        */
        public static readonly string[] NEW_LINE_SEPS = [Environment.NewLine, "\r"];
        private void ExecuteDownloadButtonCommand() {
            string idPattern = @"\d{" + GALLERY_ID_LENGTH_RANGE.Start + "," + GALLERY_ID_LENGTH_RANGE.End + "}";
            string[] urlOrIds = DownloadInputText.Split(NEW_LINE_SEPS, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (urlOrIds.Length == 0) {
                _ = MainWindowVM.NotifyUser(new() { Title = _resourceMap.GetValue("Notification_DownloadInputTextBox_Empty_Title").ValueAsString });
                return;
            }
            List<int> extractedIds = [];
            foreach (string urlOrId in urlOrIds) {
                MatchCollection matches = Regex.Matches(urlOrId, idPattern);
                if (matches.Count > 0) {
                    extractedIds.Add(int.Parse(matches.Last().Value));
                }
            }
            if (extractedIds.Count == 0) {
                _ = MainWindowVM.NotifyUser(new() { Title = _resourceMap.GetValue("Notification_DownloadInputTextBox_Invalid_Title").ValueAsString });
                return;
            }
            DownloadInputText = "";

            foreach (int id in extractedIds) {
                DownloadManagerVM.TryDownload(id);
            }
        }
    }
}
