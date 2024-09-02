using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HitomiScrollViewerLib.ViewModels.SearchPageVMs;
using HitomiScrollViewerLib.Views;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using static HitomiScrollViewerLib.SharedResources;
using static HitomiScrollViewerLib.Utils;

namespace HitomiScrollViewerLib.ViewModels.PageVMs {
    public partial class SearchPageVM : ObservableObject {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(Views.PageViews.SearchPage).Name);
        private static readonly Range GALLERY_ID_LENGTH_RANGE = 6..7;

        public ObservableCollection<SearchLinkItemVM> SearchLinkItemVMs { get; } = [];
        public TagFilterSetEditorVM TagFilterSetEditorVM { get; } = new();
        public SyncManagerVM SyncManagerVM { get; } = new();
        public DownloadManagerVM DownloadManagerVM { get; } = new();
        private readonly IEnumerable<IAppWindowClosingHandler> _appWindowClosingHandlers;

        [ObservableProperty]
        private string _downloadInputText;

        public SearchPageVM() {
            _appWindowClosingHandlers = [DownloadManagerVM, TagFilterSetEditorVM];
            DownloadButtonCommand = new RelayCommand(
                ExecuteDownloadButtonCommand,
                () => DownloadInputText.Length != 0
            );
        }

        public void HandleAppWindowClosing(AppWindowClosingEventArgs args) {
            foreach (IAppWindowClosingHandler hanlder in _appWindowClosingHandlers) {
                hanlder.HandleAppWindowClosing(args);
                if (args.Cancel) {
                    return;
                }
            }
        }


        public void HyperlinkCreateButton_Clicked(object _0, RoutedEventArgs _1) {
            SearchLinkItemVM vm = TagFilterSetEditorVM.GetSearchLinkItemVM();
            if (vm != null) {
                // copy link to clipboard
                vm.DeleteCommand.ExecuteRequested += (XamlUICommand sender, ExecuteRequestedEventArgs args) => {
                    SearchLinkItemVMs.Remove((SearchLinkItemVM)args.Parameter);
                };
                SearchLinkItemVMs.Add(vm);
                DataPackage dataPackage = new() {
                    RequestedOperation = DataPackageOperation.Copy
                };
                dataPackage.SetText(vm.SearchLink);
                Clipboard.SetContent(dataPackage);
            }
        }

        public ICommand DownloadButtonCommand { get; }

        private void ExecuteDownloadButtonCommand() {
            string idPattern = @"\d{" + GALLERY_ID_LENGTH_RANGE.Start + "," + GALLERY_ID_LENGTH_RANGE.End + "}";
            string[] urlOrIds = DownloadInputText.Split(NEW_LINE_SEPS, DEFAULT_STR_SPLIT_OPTIONS);
            if (urlOrIds.Length == 0) {
                MainWindow.CurrMW.NotifyUser(_resourceMap.GetValue("Notification_DownloadInputTextBox_Empty_Title").ValueAsString, "");
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
                MainWindow.CurrMW.NotifyUser(
                    _resourceMap.GetValue("Notification_DownloadInputTextBox_Invalid_Title").ValueAsString,
                    ""
                );
                return;
            }
            DownloadInputText = "";

            foreach (int id in extractedIds) {
                DownloadManagerVM.TryDownload(id);
            }
        }

        public Size PageSize { get; set; }

        private const int POPUP_MSG_DISPLAY_DURATION = 5000;
        private const int POPUP_MSG_MAX_DISPLAY_NUM = 3;
        public ObservableCollection<InfoBarVM> PopupMsgInfoBarVMs { get; } = [];
        public void AddPopupMsgInfoBarVM(string message) {
            InfoBarVM vm = new() {
                Message = message,
                Width = PageSize.Width / 4,
            };
            vm.CloseButtonCommand = new RelayCommand(() => PopupMsgInfoBarVMs.Remove(vm));
            PopupMsgInfoBarVMs.Add(vm);
            if (PopupMsgInfoBarVMs.Count > POPUP_MSG_MAX_DISPLAY_NUM) {
                PopupMsgInfoBarVMs.RemoveAt(0);
            }
            Task.Run(
                async () => {
                    await Task.Delay(POPUP_MSG_DISPLAY_DURATION);
                    PopupMsgInfoBarVMs.Remove(vm);
                }
            );
        }
    }
}
