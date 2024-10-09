using CommunityToolkit.Mvvm.Input;
using HitomiScrollViewerLib.Models;
using HitomiScrollViewerLib.ViewModels.PageVMs;
using HitomiScrollViewerLib.Views;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Foundation;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.ViewModels {
    public static class MainWindowVM {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(MainWindow).Name);

        public static event Func<ContentDialogModel, IAsyncOperation<ContentDialogResult>> RequestNotifyUser;
        public static event Action RequestHideCurrentNotification;

        public static event Action RequestMinimizeWindow;
        public static event Action RequestActivateWindow;

        public static void MinimizeWindow() {
            RequestMinimizeWindow?.Invoke();
        }

        public static void ActivateWindow() {
            RequestActivateWindow?.Invoke();
        }

        public static IAsyncOperation<ContentDialogResult> NotifyUser(ContentDialogModel model) {
            return RequestNotifyUser.Invoke(model);
        }

        public static void HideCurrentNotification() {
            RequestHideCurrentNotification.Invoke();
        }

        private const int POPUP_MSG_DISPLAY_DURATION = 5000;
        private const int POPUP_MSG_MAX_DISPLAY_NUM = 3;
        public static ObservableCollection<InfoBarModel> PopupMessages { get; } = [];
        public static void ShowPopup(string message) {
            InfoBarModel vm = new() {
                Message = message,
                CloseButtonCommand = new RelayCommand<InfoBarModel>((model) => PopupMessages.Remove(model))
            };
            PopupMessages.Add(vm);
            if (PopupMessages.Count > POPUP_MSG_MAX_DISPLAY_NUM) {
                PopupMessages.RemoveAt(0);
            }
            Task.Run(
                async () => {
                    await Task.Delay(POPUP_MSG_DISPLAY_DURATION);
                    MainWindow.MainDispatcherQueue.TryEnqueue(() => PopupMessages.Remove(vm));
                }
            );
        }

        public static async void HandleAppWindowClosing(AppWindowClosingEventArgs args) {
            if (SearchPageVM.Main.DownloadManagerVM.DownloadItemVMs.Count > 0) {
                ContentDialogModel cdModel = new() {
                    DefaultButton = ContentDialogButton.Close,
                    Title = _resourceMap.GetValue("Text_DownloadRemaining").ValueAsString,
                    PrimaryButtonText = TEXT_EXIT,
                    CloseButtonText = TEXT_CANCEL
                };
                ContentDialogResult cdr = await NotifyUser(cdModel);
                if (cdr == ContentDialogResult.None) {
                    args.Cancel = true;
                }
            }
            if (!args.Cancel) {
                if (SearchPageVM.Main.TagFilterEditorVM.IsTFAutoSaveEnabled) {
                    SearchPageVM.Main.TagFilterEditorVM.SaveTagFilter(SearchPageVM.Main.TagFilterEditorVM.SelectedTagFilter);
                }
                SearchPageVM.Main.Dispose();
                BrowsePageVM.Main.Dispose();
            }
        }
    }
}
