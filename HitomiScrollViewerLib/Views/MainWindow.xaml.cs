using HitomiScrollViewerLib.Models;
using HitomiScrollViewerLib.ViewModels;
using HitomiScrollViewerLib.Views.PageViews;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Foundation;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Views {
    public sealed partial class MainWindow : Window {
        public static DispatcherQueue MainDispatcherQueue { get; private set; }
        private readonly LoadProgressReporter _reporter = new();

        public MainWindow() {
            InitializeComponent();

            AppWindow.Closing += AppWindow_Closing;
            ((OverlappedPresenter)AppWindow.Presenter).Maximize();
            Title = APP_DISPLAY_NAME;

            RootFrame.Loaded += RootFrame_Loaded;

            MainDispatcherQueue = DispatcherQueue;
        }

        // TODO implement DQObservableObject and revert most back to ObservableProperties

        private void RootFrame_Loaded(object sender, RoutedEventArgs e) {
            RootFrame.Loaded -= RootFrame_Loaded;
            MainWindowVM.ShowLoadProgressReporter += (LoadProgressReporterVM e) => {
                DispatcherQueue.TryEnqueue(() => {
                    _reporter.XamlRoot = RootFrame.XamlRoot;
                    _reporter.ViewModel = e;
                    _ = _reporter.ShowAsync();
                });
            };
            MainWindowVM.HideLoadProgressReporter += () => DispatcherQueue.TryEnqueue(_reporter.Hide);
            MainWindowVM.RequestNotifyUser += NotifyUser;
            MainWindowVM.RequestHideCurrentNotification += () => DispatcherQueue.TryEnqueue(_currentNotification.Hide);
            MainWindowVM.RequestMinimizeWindow += () => (AppWindow.Presenter as OverlappedPresenter).Minimize();
            MainWindowVM.RequestActivateWindow += Activate;
            MainWindowVM.Initialised += () => DispatcherQueue.TryEnqueue(() => SelectorBar_SelectionChanged(MainSelectorBar, null));
            MainWindowVM.Init();
        }

        private void SelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs _1) {
            if (!MainWindowVM.IsInitialised) {
                return;
            }
            int currSelectedIdx = sender.Items.IndexOf(sender.SelectedItem);
            if (currSelectedIdx == -1) {
                currSelectedIdx = 0;
                sender.SelectedItem = sender.Items[currSelectedIdx];
            }
            //RootFrame.Content = currSelectedIdx switch {
            //    0 => SearchPageVM,
            //    1 => BrowsePageVM,
            //    2 => ViewPageVM,
            //    _ => throw new InvalidOperationException($"{currSelectedIdx} is an invalid Page index.")
            //};
            switch (currSelectedIdx) {
                case 0:
                    RootFrame.Navigate(typeof(SearchPage));
                    break;
                case 1:
                    RootFrame.Navigate(typeof(BrowsePage));
                    break;
                case 2:
                    RootFrame.Navigate(typeof(ViewPage));
                    break;
                case 3:
                    RootFrame.Navigate(typeof(SettingsPage));
                    break;
                default:
                    throw new InvalidOperationException($"{currSelectedIdx} is an invalid Page index.");
            }

            //var slideNavigationTransitionEffect = currSelectedIdx - previousSelectedIndex > 0 ? SlideNavigationTransitionEffect.FromRight : SlideNavigationTransitionEffect.FromLeft;
            //previousSelectedIndex = currSelectedIdx;
        }

        private ContentDialog _currentNotification;

        private IAsyncOperation<ContentDialogResult> NotifyUser(ContentDialogModel model) {
            _currentNotification = new() {
                DefaultButton = model.DefaultButton,
                Title = new TextBlock() {
                    TextWrapping = TextWrapping.WrapWholeWords,
                    Text = model.Title
                },
                Content = new TextBlock() {
                    TextWrapping = TextWrapping.WrapWholeWords,
                    Text = model.Message
                },
                PrimaryButtonText = model.PrimaryButtonText,
                CloseButtonText = model.CloseButtonText,
                XamlRoot = RootFrame.XamlRoot
            };
            return _currentNotification.ShowAsync();
        }

        private void AppWindow_Closing(AppWindow _, AppWindowClosingEventArgs args) {
            MainWindowVM.HandleAppWindowClosing(args);
        }

        private void RootFrame_SizeChanged(object _0, SizeChangedEventArgs e) {
            PopupInfoBarItemsRepeater.Margin = new(0, 0, 0, e.NewSize.Height / 16);
            PopupInfoBarItemsRepeater.Width = e.NewSize.Width / 4;
        }
    }
}
