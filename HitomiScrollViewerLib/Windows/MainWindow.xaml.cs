using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Pages;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Windows {
    public sealed partial class MainWindow : Window {
        private static MainWindow _currMW;
        public static MainWindow CurrMW {
            get => _currMW ??= new MainWindow();
        }
        public static SearchPage SearchPage { get; private set; }
        public static BrowsePage BrowsePage { get; private set; }
        public static ViewPage ViewPage { get; private set; }

        private readonly IAppWindowClosingHandler[] _appWindowClosingHandlers;

        private MainWindow() {
            InitializeComponent();
            SearchPage = new();
            BrowsePage = new();
            ViewPage = new();
            _appWindowClosingHandlers = [SearchPage, ViewPage];
            AppWindow.Closing += AppWindow_Closing;
            ((OverlappedPresenter)AppWindow.Presenter).Maximize();
            Title = APP_DISPLAY_NAME;
        }

        private void SelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args) {
            int currSelectedIdx = sender.Items.IndexOf(sender.SelectedItem);
            RootFrame.Content = currSelectedIdx switch {
                0 => SearchPage,
                1 => BrowsePage,
                2 => ViewPage,
                _ => throw new InvalidOperationException($"{currSelectedIdx} is an invalid Page index.")
            };

            //var slideNavigationTransitionEffect = currSelectedIdx - previousSelectedIndex > 0 ? SlideNavigationTransitionEffect.FromRight : SlideNavigationTransitionEffect.FromLeft;
            //RootFrame.Navigate(,);
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
            foreach (IAppWindowClosingHandler handler in _appWindowClosingHandlers) {
                handler.HandleAppWindowClosing(args);
                if (args.Cancel) {
                    return;
                }
            }
            HitomiContext.Main.Dispose();
        }
    }
}
