using HitomiScrollViewerLib.Models;
using HitomiScrollViewerLib.ViewModels;
using HitomiScrollViewerLib.ViewModels.PageVMs;
using HitomiScrollViewerLib.Views.PageViews;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;

namespace HitomiScrollViewerLib.Views {
    public sealed partial class MainWindow : Window {
        public static DispatcherQueue MainDispatcherQueue { get; private set; }
        private readonly LoadProgressReporter _reporter = new();

        public MainWindow() {
            InitializeComponent();

            AppWindow.Closing += AppWindow_Closing;
            AppWindow.SetIcon("Assets/favicon.ico");
            Title = AppInfo.Current.DisplayInfo.DisplayName;
            ((OverlappedPresenter)AppWindow.Presenter).Maximize();

            RootFrame.Loaded += RootFrame_Loaded;

            MainDispatcherQueue = DispatcherQueue;
        }

        private void RootFrame_Loaded(object sender, RoutedEventArgs e) {
            RootFrame.Loaded -= RootFrame_Loaded;
            
            MainWindowVM.RequestNotifyUser += NotifyUser;
            MainWindowVM.RequestHideCurrentNotification += () => { _currentNotification?.Hide(); };
            MainWindowVM.RequestMinimizeWindow += () => (AppWindow.Presenter as OverlappedPresenter).Minimize();
            MainWindowVM.RequestActivateWindow += Activate;

            AppInitializer.ShowLoadProgressReporter += (LoadProgressReporterVM e) => {
                DispatcherQueue.TryEnqueue(() => {
                    _reporter.XamlRoot = RootFrame.XamlRoot;
                    _reporter.ViewModel = e;
                    _ = _reporter.ShowAsync();
                });
            };
            AppInitializer.HideLoadProgressReporter += () => DispatcherQueue.TryEnqueue(_reporter.Hide);
            AppInitializer.Initialised += () => DispatcherQueue.TryEnqueue(() => {
                BrowsePageVM.Main.NavigateToViewPageRequested += () => {
                    MainSelectorBar.SelectedItem = MainSelectorBar.Items[2];
                };
                MainSelectorBar.IsEnabled = true;
                SelectorBar_SelectionChanged(MainSelectorBar, null);
            });
            _ = Task.Run(AppInitializer.StartAsync);
        }

        private void SelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs _1) {
            int currSelectedIdx = sender.Items.IndexOf(sender.SelectedItem);
            if (currSelectedIdx == -1) {
                currSelectedIdx = 0;
                sender.SelectedItem = sender.Items[currSelectedIdx];
            }
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
        }

        private ContentDialog _currentNotification;

        private IAsyncOperation<ContentDialogResult> NotifyUser(ContentDialogModel model) {
            _currentNotification = new() {
                DefaultButton = model.DefaultButton,
                Title = new TextBlock() {
                    TextWrapping = TextWrapping.WrapWholeWords,
                    Text = model.Title,
                    FontWeight = FontWeights.Bold
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
