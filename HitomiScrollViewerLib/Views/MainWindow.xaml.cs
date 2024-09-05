using CommunityToolkit.WinUI;
using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.ViewModels.PageVMs;
using HitomiScrollViewerLib.ViewModels.SearchPageVMs;
using HitomiScrollViewerLib.Views.PageViews;
using HitomiScrollViewerLib.Views.SearchPageViews;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using static HitomiScrollViewerLib.SharedResources;
using static HitomiScrollViewerLib.Utils;

namespace HitomiScrollViewerLib.Views {
    public sealed partial class MainWindow : Window {
        private static MainWindow _currMW;
        public static MainWindow CurrMW {
            get => _currMW ??= new MainWindow();
        }
        public static SearchPageVM SearchPageVM { get; private set; }
        public static BrowsePageVM BrowsePageVM { get; private set; }
        public static ViewPageVM ViewPageVM { get; private set; }

        private MainWindow() {
            InitializeComponent();

            AppWindow.Closing += AppWindow_Closing;
            ((OverlappedPresenter)AppWindow.Presenter).Maximize();
            Title = APP_DISPLAY_NAME;
            // TODO
            // if app upgraded from v2 -> v3:
            // 1. Migrate tag filter set - DONE
            // 2. Migrate galleries (bookmarks)
            // 3. Migrate images from roaming to local folder - DONE

            _ = Task.Run(async () => {
                LoadProgressReporter reporter = new();
                LoadProgressReporterVM vm = reporter.ViewModel;
                vm.IsIndeterminate = true;
                await DispatcherQueue.EnqueueAsync(() => {
                    reporter.XamlRoot = RootFrame.XamlRoot;
                });
                _ = await DispatcherQueue.EnqueueAsync(reporter.ShowAsync);

                vm.SetText(LoadProgressReporterVM.LoadingStatus.InitialisingDatabase);
                bool dbCreatedFirstTime = HitomiContext.Main.Database.EnsureCreated();
                if (dbCreatedFirstTime) {
                    vm.IsIndeterminate = false;
                    vm.Value = 0;
                    vm.Maximum = HitomiContext.DATABASE_INIT_OP_NUM;
                    HitomiContext.Main.DatabaseInitProgressChanged += (_, value) => {
                        DispatcherQueue.TryEnqueue(() => vm.Value = value);
                    };
                    HitomiContext.Main.ChangeToIndeterminateEvent += (_, _) => {
                        DispatcherQueue.TryEnqueue(() => vm.IsIndeterminate = true);
                    };
                    HitomiContext.InitDatabase();
                }

                bool v2TagFilterExists = File.Exists(TAG_FILTERS_FILE_PATH_V2);
                // User upgraded from v2 to v3
                if (v2TagFilterExists) {
                    vm.IsIndeterminate = false;
                    vm.Value = 0;
                    vm.SetText(LoadProgressReporterVM.LoadingStatus.MigratingTFSs);
                    Dictionary<string, TagFilterV2> tagFilterV2 = (Dictionary<string, TagFilterV2>)JsonSerializer.Deserialize(
                        File.ReadAllText(TAG_FILTERS_FILE_PATH_V2),
                        typeof(Dictionary<string, TagFilterV2>),
                        TagFilterV2.DEFAULT_SERIALIZER_OPTIONS
                    );
                    vm.Maximum = tagFilterV2.Count;
                    foreach (var pair in tagFilterV2) {
                        HitomiContext.Main.AddRange(pair.Value.ToTagFilterSet(pair.Key));
                        vm.Value++;
                    }
                    HitomiContext.Main.SaveChanges();
                    File.Delete(TAG_FILTERS_FILE_PATH_V2);
                }

                // The user installed this app for the first time (which means there was no tf migration)
                // AND is starting the app for the first time
                if (!v2TagFilterExists && dbCreatedFirstTime) {
                    vm.IsIndeterminate = true;
                    vm.SetText(LoadProgressReporterVM.LoadingStatus.AddingExampleTFSs);
                    HitomiContext.Main.AddExampleTagFilterSets();
                }

                if (Directory.Exists(IMAGE_DIR_V2)) {
                    // move images folder in roaming folder to local
                    vm.IsIndeterminate = true;
                    vm.SetText(LoadProgressReporterVM.LoadingStatus.MovingImageFolder);
                    Directory.Move(IMAGE_DIR_V2, IMAGE_DIR_V3);
                }

                // TODO
                //if (File.Exists(BOOKMARKS_FILE_PATH_V2)) {
                //    await DispatcherQueue.EnqueueAsync(() => {
                //        reporter.SetProgressBarType(false);
                //        reporter.ResetValue();
                //        reporter.SetStatusMessage(LoadProgressReporter.LoadingStatus.MigratingGalleries);
                //    });

                //    await DispatcherQueue.EnqueueAsync(() => reporter.SetMaximum());
                //}

                vm.IsIndeterminate = false;
                vm.Value = 0;
                vm.Maximum = 4;
                vm.SetText(LoadProgressReporterVM.LoadingStatus.LoadingDatabase);
                HitomiContext.Main.Tags.Load();
                vm.Value++;
                HitomiContext.Main.GalleryLanguages.Load();
                vm.Value++;
                HitomiContext.Main.TagFilterSets.Load();
                vm.Value++;
                HitomiContext.Main.Galleries.Load();
                
                vm.IsIndeterminate = true;
                vm.SetText(LoadProgressReporterVM.LoadingStatus.InitialisingApp);
                SearchPageVM = new();
                BrowsePageVM = new();
                ViewPageVM = new();

                await DispatcherQueue.EnqueueAsync(reporter.Hide);
            });
        }

        private void SelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args) {
            int currSelectedIdx = sender.Items.IndexOf(sender.SelectedItem);
            //RootFrame.Content = currSelectedIdx switch {
            //    0 => SearchPageVM,
            //    1 => BrowsePageVM,
            //    2 => ViewPageVM,
            //    _ => throw new InvalidOperationException($"{currSelectedIdx} is an invalid Page index.")
            //};
            switch (currSelectedIdx) {
                case 0:
                    RootFrame.Navigate(typeof(SearchPage), SearchPageVM);
                    break;
                case 1:
                    RootFrame.Navigate(typeof(BrowsePage), BrowsePageVM);
                    break;
                case 2:
                    RootFrame.Navigate(typeof(ViewPage), ViewPageVM);
                    break;
                default:
                    throw new InvalidOperationException($"{currSelectedIdx} is an invalid Page index.");

            }

            //var slideNavigationTransitionEffect = currSelectedIdx - previousSelectedIndex > 0 ? SlideNavigationTransitionEffect.FromRight : SlideNavigationTransitionEffect.FromLeft;
            //previousSelectedIndex = currSelectedIdx;
        }


        private static readonly ContentDialog _notification = new() {
            CloseButtonText = TEXT_CLOSE,
            Title = new TextBlock() {
                TextWrapping = TextWrapping.WrapWholeWords
            },
            Content = new TextBlock() {
                TextWrapping = TextWrapping.WrapWholeWords
            }
        };

        public async void NotifyUser(string title, string content) {
            ((TextBlock)_notification.Title).Text = title;
            ((TextBlock)_notification.Content).Text = content;
            _notification.XamlRoot = RootFrame.XamlRoot;
            await _notification.ShowAsync();
        }

        private void AppWindow_Closing(AppWindow _, AppWindowClosingEventArgs args) {
            SearchPageVM.HandleAppWindowClosing(args);
            if (args.Cancel) {
                return;
            }
            HitomiContext.Main.Dispose();
        }
    }
}
