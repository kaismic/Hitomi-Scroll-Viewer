using CommunityToolkit.Mvvm.Input;
using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.Models;
using HitomiScrollViewerLib.ViewModels.SearchPageVMs;
using HitomiScrollViewerLib.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Foundation;
using static HitomiScrollViewerLib.Constants;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.ViewModels {
    public static class MainWindowVM {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(MainWindow).Name);

        public static event Action<LoadProgressReporterVM> ShowLoadProgressReporter;
        public static event Action HideLoadProgressReporter;

        public static event Func<ContentDialogModel, IAsyncOperation<ContentDialogResult>> RequestNotifyUser;
        public static event Action RequestHideCurrentNotification;

        public static event Action RequestMinimizeWindow;
        public static event Action RequestActivateWindow;

        public static event Action Initialised;
        public static bool IsInitialised { get; private set; } = false;

        public static void Init() {
            // TODO
            // if app upgraded from v2 -> v3:
            // 1. Migrate tag filter set - DONE
            // 2. Migrate galleries (bookmarks)
            // 3. Migrate images from roaming to local folder - DONE
            _ = Task.Run(() => {
                LoadProgressReporterVM vm = new() {
                    IsIndeterminate = true
                };
                ShowLoadProgressReporter.Invoke(vm);

                vm.SetText(LoadProgressReporterVM.LoadingStatus.InitialisingDatabase);
                bool dbCreatedFirstTime = HitomiContext.Main.Database.EnsureCreated();
                if (dbCreatedFirstTime) {
                    vm.IsIndeterminate = false;
                    vm.Value = 0;
                    vm.Maximum = HitomiContext.DATABASE_INIT_OP_NUM;
                    HitomiContext.Main.DatabaseInitProgressChanged += (_, value) => vm.Value = value;
                    HitomiContext.Main.ChangeToIndeterminateEvent += (_, _) => vm.IsIndeterminate = true;
                    HitomiContext.InitDatabase();
                }

                bool v2TagFilterExists = File.Exists(TAG_FILTERS_FILE_PATH_V2);
                // User upgraded from v2 to v3
                if (v2TagFilterExists) {
                    vm.IsIndeterminate = false;
                    vm.Value = 0;
                    vm.SetText(LoadProgressReporterVM.LoadingStatus.MigratingTFSs);
                    Dictionary<string, LegacyTagFilter> legacyTagFilters = (Dictionary<string, LegacyTagFilter>)JsonSerializer.Deserialize(
                        File.ReadAllText(TAG_FILTERS_FILE_PATH_V2),
                        typeof(Dictionary<string, LegacyTagFilter>),
                        TF_SERIALIZER_OPTIONS
                    );
                    vm.Maximum = legacyTagFilters.Count;
                    foreach (var pair in legacyTagFilters) {
                        HitomiContext.Main.TagFilters.AddRange(pair.Value.ToTagFilter(pair.Key));
                        vm.Value++;
                    }
                    HitomiContext.Main.SaveChanges();
                    File.Delete(TAG_FILTERS_FILE_PATH_V2);
                }

                // The user installed this app for the first time (which means there is no previous tf)
                // AND is starting the app for the first time
                if (!v2TagFilterExists && dbCreatedFirstTime) {
                    vm.IsIndeterminate = true;
                    vm.SetText(LoadProgressReporterVM.LoadingStatus.AddingExampleTFSs);
                    HitomiContext.Main.AddExampleTagFilters();
                }

                // move images folder in roaming folder to local
                if (Directory.Exists(IMAGE_DIR_V2)) {
                    vm.IsIndeterminate = true;
                    vm.SetText(LoadProgressReporterVM.LoadingStatus.MovingImageFolder);
                    Directory.Move(IMAGE_DIR_V2, IMAGE_DIR_V3);
                    // TODO convert image files from 0.webp 1.webp -> 000.webp 001.webp
                }

                // migrate existing galleries (p.k.a. bookmarks) from v2
                if (File.Exists(BOOKMARKS_FILE_PATH_V2)) {
                    vm.IsIndeterminate = false;
                    vm.Value = 0;
                    vm.SetText(LoadProgressReporterVM.LoadingStatus.MigratingGalleries);
                    List<OriginalGalleryInfo> originalGalleryInfos = (List<OriginalGalleryInfo>)JsonSerializer.Deserialize(
                        File.ReadAllText(BOOKMARKS_FILE_PATH_V2),
                        typeof(List<OriginalGalleryInfo>),
                        GALLERY_SERIALIZER_OPTIONS
                    );
                    vm.Maximum = originalGalleryInfos.Count;
                    foreach (var ogi in originalGalleryInfos) {
                        HitomiContext.Main.Galleries.Add(ogi.ToGallery());
                        vm.Value++;
                    }
                    HitomiContext.Main.SaveChanges();
                    File.Delete(BOOKMARKS_FILE_PATH_V2);
                }

                if (Directory.Exists(ROOT_DIR_V2)) {
                    Directory.Delete(ROOT_DIR_V2);
                }

                // TODO test if this is necessary
                vm.IsIndeterminate = false;
                vm.Value = 0;
                vm.Maximum = 6;
                vm.SetText(LoadProgressReporterVM.LoadingStatus.LoadingDatabase);
                HitomiContext.Main.Tags.Load();
                vm.Value++;
                HitomiContext.Main.GalleryLanguages.Load();
                vm.Value++;
                HitomiContext.Main.TagFilters.Load();
                vm.Value++;
                HitomiContext.Main.Galleries.Load();
                vm.Value++;
                HitomiContext.Main.GalleryTypes.Load();
                vm.Value++;
                HitomiContext.Main.UserSavedBrowseTags.Load();
                vm.Value++;

                vm.IsIndeterminate = true;
                vm.SetText(LoadProgressReporterVM.LoadingStatus.InitialisingApp);

                HideLoadProgressReporter.Invoke();

                IsInitialised = true;
                Initialised?.Invoke();
            });
        }

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
            if (DownloadManagerVM.Main.DownloadItemVMs.Count > 0) {
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
                HitomiContext.Main.Dispose();
            }
        }
    }
}
