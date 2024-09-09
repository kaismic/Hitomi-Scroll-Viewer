using HitomiScrollViewerLib.ViewModels;
using HitomiScrollViewerLib.ViewModels.SearchPageVMs;
using HitomiScrollViewerLib.Views.PageViews;
using HitomiScrollViewerLib.Views.SearchPageViews;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Views {
    public sealed partial class MainWindow : Window {
        private MainWindowVM ViewModel { get; } = MainWindowVM.Main;

        private LoadProgressReporter _reporter;

        public MainWindow() {
            InitializeComponent();

            AppWindow.Closing += AppWindow_Closing;
            ((OverlappedPresenter)AppWindow.Presenter).Maximize();
            Title = APP_DISPLAY_NAME;
            _notification.XamlRoot = RootFrame.XamlRoot;

            ViewModel.ShowLoadProgressReporter += (LoadProgressReporterVM e) => {
                _reporter = new() {
                    XamlRoot = RootFrame.XamlRoot,
                    ViewModel = e
                };
                _ = _reporter.ShowAsync();
            };
            ViewModel.HideLoadProgressReporter += _reporter.Hide;
            ViewModel.RequestNotifyUser += NotifyUser;
            ViewModel.Init();
        }

        private void SelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs _1) {
            int currSelectedIdx = sender.Items.IndexOf(sender.SelectedItem);
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

        private static void NotifyUser(NotificationEventArgs e) {
            ((TextBlock)_notification.Title).Text = e.Title;
            ((TextBlock)_notification.Content).Text = e.Message;
            _ = _notification.ShowAsync();
        }

        private void AppWindow_Closing(AppWindow _, AppWindowClosingEventArgs args) {
            MainWindowVM.Main.HandleAppWindowClosing(args);
        }

        private void RootFrame_SizeChanged(object _0, SizeChangedEventArgs e) {
            PopupInfoBarStackPanel.Margin = new(0, 0, 0, e.NewSize.Height / 16);
            PopupInfoBarStackPanel.Width = e.NewSize.Width / 4;
        }
    }
}
