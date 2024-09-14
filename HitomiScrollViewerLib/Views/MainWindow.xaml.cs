using HitomiScrollViewerLib.Models;
using HitomiScrollViewerLib.ViewModels;
using HitomiScrollViewerLib.Views.PageViews;
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

            MainWindowVM.ShowLoadProgressReporter += (LoadProgressReporterVM e) => {
                _reporter = new() {
                    XamlRoot = RootFrame.XamlRoot,
                    ViewModel = e
                };
                _ = _reporter.ShowAsync();
            };
            MainWindowVM.HideLoadProgressReporter += _reporter.Hide;
            MainWindowVM.RequestNotifyUser += NotifyUser;
            MainWindowVM.Init();
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

        private async void NotifyUser(ContentDialogModel model) {
            ContentDialog cd = new() {
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
            model.InvokeClosedEvent(await cd.ShowAsync());
        }

        private void AppWindow_Closing(AppWindow _, AppWindowClosingEventArgs args) {
            MainWindowVM.Main.HandleAppWindowClosing(args);
        }

        private void RootFrame_SizeChanged(object _0, SizeChangedEventArgs e) {
            PopupInfoBarItemsRepeater.Margin = new(0, 0, 0, e.NewSize.Height / 16);
            PopupInfoBarItemsRepeater.Width = e.NewSize.Width / 4;
        }
    }
}
