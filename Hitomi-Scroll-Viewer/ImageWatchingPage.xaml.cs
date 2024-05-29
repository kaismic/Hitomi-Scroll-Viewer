using Hitomi_Scroll_Viewer.ImageWatchingPageComponent;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;
using static Hitomi_Scroll_Viewer.Utils;

namespace Hitomi_Scroll_Viewer {
    public sealed partial class ImageWatchingPage : Page {
        private static readonly string SCROLL_SPEED_TEXT = "Auto Scroll Speed";
        private static readonly string PAGE_TURN_DELAY_TEXT = "Auto Page Turn Delay";
        private static readonly (double min, double max) SCROLL_SPEED_RANGE = (0.001, 1);
        private static readonly (double min, double max) PAGE_TURN_DELAY_RANGE = (1, 10);
        private static readonly double SCROLL_SPEED_FREQ = 0.001;
        private static readonly double PAGE_TURN_DELAY_FREQ = 0.5;
        private static double _scrollSpeed;
        private static double _pageTurnDelay; // in seconds
        public bool IsAutoScrolling { private set; get; } = false;
        private bool _isLooping = true;
        public Gallery CurrLoadedGallery { get; set; }
        private readonly ObservableCollection<Image> _imageCollection = [];
        private readonly ObservableCollection<Image> _reverseImageCollection = [];
        private readonly ObservableCollection<GroupedImagePanel> _groupedImagePanels = [];

        private readonly FlipView _flipView = new() {
            FocusVisualPrimaryThickness = new(0)
        };
        private readonly ScrollViewer _scrollViewer = new() {
            ZoomMode = ZoomMode.Enabled
        };
        private readonly ItemsRepeater _scrollViewerItemsRepeater = new();
        private readonly StackLayout _scrollViewerStackLayout = new();

        private (double[] vertical, double[] horizontal) _scrollOffsetAccum;

        public enum ViewMode {
            Default,
            Scroll
        }
        private static ViewMode _viewMode;
        public enum ViewDirection {
            TopToBottom,
            LeftToRight,
            RightToLeft
        }
        private static ViewDirection _viewDirection;

        private static int _currPageIndex;
        private static int _numOfPages;

        private bool _isInAction = false;
        private bool _pageChangedBySystem = false;

        public ImageWatchingPage() {
            InitializeComponent();

            EnableControls(false);

            if (File.Exists(SETTINGS_PATH)) {
                Settings settings = (Settings)JsonSerializer.Deserialize(
                    File.ReadAllText(SETTINGS_PATH),
                    typeof(Settings),
                    serializerOptions
                );
                _viewMode = settings.viewMode;
                _viewDirection = settings.viewDirection;
                _scrollSpeed = settings.scrollSpeed;
                _numOfPages = settings.numOfPages;
                _pageTurnDelay = settings.pageTurnDelay;
                _isLooping = settings.isLooping;
            } else {
                _viewMode = ViewMode.Default;
                _viewDirection = ViewDirection.RightToLeft;
                _scrollSpeed = 0.05;
                _numOfPages = 1;
                _pageTurnDelay = 5;
                _isLooping = true;
            }
            SetScrollSpeedSlider();
            SetViewModeTextAndIcon();
            ViewDirectionSelector.SelectedIndex = (int)_viewDirection;
            NumOfPagesSelector.SelectedIndex = _numOfPages - 1;
            LoopBtn.IsChecked = _isLooping;

            _flipView.ItemsSource = _groupedImagePanels;
            _flipView.SelectionChanged += FlipView_SelectionChanged;
            _scrollViewer.Content = _scrollViewerItemsRepeater;
            _scrollViewer.ViewChanging += ScrollViewer_ViewChanging;
            _scrollViewerItemsRepeater.Layout = _scrollViewerStackLayout;

            PreviewKeyDown += ImageWatchingPage_PreviewKeyDown;

            TopCommandBar.PointerEntered += (_, _) => {
                TopCommandBar.Opacity = 1;
                TopCommandBar.IsOpen = true;
                PageNumDisplay.Visibility = Visibility.Visible;
            };
            TopCommandBar.Opening += (_, _) => {
                TopCommandBar.Opacity = 1;
                PageNumDisplay.Visibility = Visibility.Visible;
            };
            TopCommandBar.Closing += (_, _) => {
                TopCommandBar.Opacity = 0;
                PageNumDisplay.Visibility = Visibility.Collapsed;
            };
            GoBackBtn.Click += (_, _) => App.MainWindow.SwitchPage();
            AutoScrollBtn.Click += (_, _) => StartStopAutoScroll((bool)AutoScrollBtn.IsChecked);

            // remove _flipview navigation buttons
            void FlipView_Loaded(object sender, RoutedEventArgs e) {
                _flipView.Loaded -= FlipView_Loaded;
                Grid flipViewGrid = VisualTreeHelper.GetChild(_flipView, 0) as Grid;
                var children = flipViewGrid.Children;
                for (int i = children.Count - 1; i >= 0; i--) {
                    if (children[i] is Button) {
                        children.RemoveAt(i);
                    }
                }
            }
            _flipView.Loaded += FlipView_Loaded;
        }

        private void SetScrollSpeedSlider() {
            switch (_viewMode) {
                case ViewMode.Default:
                    // save pageTurnDelay value because ScrollSpeedSlider.Value resets when min max is set
                    double pageTurnDelay = _pageTurnDelay;
                    ScrollSpeedSlider.StepFrequency = PAGE_TURN_DELAY_FREQ;
                    ScrollSpeedSlider.TickFrequency = PAGE_TURN_DELAY_FREQ;
                    ScrollSpeedSlider.Minimum = PAGE_TURN_DELAY_RANGE.min;
                    ScrollSpeedSlider.Maximum = PAGE_TURN_DELAY_RANGE.max;
                    ScrollSpeedSlider.Header = PAGE_TURN_DELAY_TEXT;
                    ScrollSpeedSlider.Value = pageTurnDelay;
                    _pageTurnDelay = pageTurnDelay;
                    break;
                case ViewMode.Scroll:
                    // save scrollSpeed value because ScrollSpeedSlider.Value resets when min max is set
                    double scrollSpeed = _scrollSpeed;
                    ScrollSpeedSlider.StepFrequency = SCROLL_SPEED_FREQ;
                    ScrollSpeedSlider.TickFrequency = SCROLL_SPEED_FREQ;
                    ScrollSpeedSlider.Minimum = SCROLL_SPEED_RANGE.min;
                    ScrollSpeedSlider.Maximum = SCROLL_SPEED_RANGE.max;
                    ScrollSpeedSlider.Header = SCROLL_SPEED_TEXT;
                    ScrollSpeedSlider.Value = scrollSpeed;
                    _scrollSpeed = scrollSpeed;
                    break;
            }
        }

        private static readonly string GLYPH_CANCEL = "\xE711";

        private void ShowActionIndicator(Symbol? symbol, string glyph) {
            if (symbol == null) {
                ActionIndicatorSymbolIcon.Opacity = 0;
            } else {
                ActionIndicatorSymbolIcon.Symbol = symbol.Value;
                ActionIndicatorSymbolIcon.Opacity = 1;
            }
            if (glyph == null) {
                ActionIndicatorFontIcon.Opacity = 0;
            } else {
                ActionIndicatorFontIcon.Glyph = glyph;
                ActionIndicatorFontIcon.Opacity = 1;
            }
            FadeOutStoryboard.Begin();
        }

        private void ScrollViewer_ViewChanging(object _0, ScrollViewerViewChangingEventArgs e) {
            if (_viewMode == ViewMode.Scroll && !_isInAction && !_pageChangedBySystem) {
                SetCurrPageIndex(GetPageIndexFromScrollOffset());
            }
        }

        private void HandleLoopBtnClick(object _0, RoutedEventArgs _1) {
            if ((bool)LoopBtn.IsChecked) {
                ShowActionIndicator(Symbol.RepeatAll, null);
            } else {
                ShowActionIndicator(Symbol.RepeatAll, GLYPH_CANCEL);
            }
        }

        private void SetScrollMode() {
            switch (_viewDirection) {
                case ViewDirection.TopToBottom:
                    _scrollViewerStackLayout.Orientation = Orientation.Vertical;
                    _scrollViewer.HorizontalScrollMode = ScrollMode.Disabled;
                    _scrollViewer.VerticalScrollMode = ScrollMode.Enabled;
                    _scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    _scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                    break;
                case ViewDirection.LeftToRight or ViewDirection.RightToLeft:
                    _scrollViewerStackLayout.Orientation = Orientation.Horizontal;
                    _scrollViewer.HorizontalScrollMode = ScrollMode.Enabled;
                    _scrollViewer.VerticalScrollMode = ScrollMode.Disabled;
                    _scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
                    _scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    break;
            }
        }

        private async void ViewDirectionSelector_SelectionChanged(object _0, SelectionChangedEventArgs e) {
            if (e.RemovedItems.Count == 0) {
                return;
            }
            _viewDirection = (ViewDirection)ViewDirectionSelector.SelectedIndex;

            switch (_viewMode) {
                case ViewMode.Default:
                    await SetImageOrientationAndSize();
                    break;
                case ViewMode.Scroll:
                    RefreshLayout();
                    break;
            }
        }

        private async Task SetImageOrientationAndSize() {
            (double width, double height) viewportSize = (0, 0);
            foreach (var image in _imageCollection) {
                image.Width = double.NaN;
                image.Height = double.NaN;
                if (image.Source != null) {
                    (image.Source as BitmapImage).DecodePixelWidth = 0;
                    (image.Source as BitmapImage).DecodePixelHeight = 0;
                }
            }
            switch (_viewMode) {
                case ViewMode.Default:
                    while (viewportSize.width == 0 || viewportSize.height == 0) {
                        viewportSize = (_flipView.ActualWidth, _flipView.ActualHeight);
                        await Task.Delay(200);
                    }
                    foreach (var panel in _groupedImagePanels) {
                        panel.UpdateViewDirection(_viewDirection);
                        panel.SetImageSizes(_viewDirection, viewportSize);
                    }
                    break;
                case ViewMode.Scroll:
                    while (viewportSize.width == 0 || viewportSize.height == 0) {
                        viewportSize = (_scrollViewer.ActualWidth, _scrollViewer.ActualHeight);
                        await Task.Delay(200);
                    }
                    SetScrollMode();
                    if (_viewDirection == ViewDirection.RightToLeft) {
                        _scrollViewerItemsRepeater.ItemsSource = _reverseImageCollection;
                    } else {
                        _scrollViewerItemsRepeater.ItemsSource = _imageCollection;
                    }
                    double dimension = _viewDirection == ViewDirection.TopToBottom ? viewportSize.width : viewportSize.height;
                    switch (_viewDirection) {
                        case ViewDirection.TopToBottom:
                            _scrollOffsetAccum.vertical = new double[_imageCollection.Count];
                            for (int i = 0; i < _imageCollection.Count; i++) {
                                Image image = _imageCollection[i];
                                image.Width = dimension;
                                image.Height = dimension * CurrLoadedGallery.files[i].height / CurrLoadedGallery.files[i].width;
                                _scrollOffsetAccum.vertical[i] =  image.Height + (i == 0 ? 0 : _scrollOffsetAccum.vertical[i - 1]);
                                if (image.Source != null) {
                                    (image.Source as BitmapImage).DecodePixelWidth = (int)dimension;
                                }
                            }
                            break;
                        case ViewDirection.LeftToRight or ViewDirection.RightToLeft:
                            _scrollOffsetAccum.horizontal = new double[_imageCollection.Count];
                            for (int i = 0; i < _imageCollection.Count; i++) {
                                Image image = _imageCollection[i];
                                image.Width = dimension * CurrLoadedGallery.files[i].width / CurrLoadedGallery.files[i].height;
                                image.Height = dimension;
                                _scrollOffsetAccum.horizontal[i] = image.Width + (i == 0 ? 0 : _scrollOffsetAccum.horizontal[i - 1]);
                                if (image.Source != null) {
                                    (image.Source as BitmapImage).DecodePixelHeight = (int)dimension;
                                }
                            }
                            break;
                    }
                    break;
            }
        }

        private void FlipView_SelectionChanged(object _0, SelectionChangedEventArgs e) {
            if (e.RemovedItems.Count == 0 || _pageChangedBySystem || _flipView.SelectedItem == null || _isInAction) {
                return;
            }
            _pageChangedBySystem = true;
            SetCurrPageIndex(_flipView.SelectedIndex * _numOfPages);
            _pageChangedBySystem = false;
        }

        private void PageNavigator_SelectionChanged(object _0, SelectionChangedEventArgs e) {
            if (e.RemovedItems.Count == 0 || _pageChangedBySystem || PageNavigator.SelectedItem == null || _isInAction) {
                return;
            }
            int groupedPageIndex = PageNavigator.SelectedIndex / _numOfPages;
            int pageIndex = groupedPageIndex * _numOfPages;
            _pageChangedBySystem = true;
            switch (_viewMode) {
                case ViewMode.Default:
                    _flipView.SelectedIndex = groupedPageIndex;
                    break;
                case ViewMode.Scroll:
                    double offset = GetScrollOffsetFromPageIndex(pageIndex);
                    switch (_viewDirection) {
                        case ViewDirection.TopToBottom:
                            _scrollViewer.ScrollToVerticalOffset(offset);
                            break;
                        case ViewDirection.LeftToRight or ViewDirection.RightToLeft:
                            _scrollViewer.ScrollToVerticalOffset(offset);
                            break;
                    }
                    break;
            }
            SetCurrPageIndex(pageIndex);
            _pageChangedBySystem = false;
        }

        private void NumOfPagesSelector_SelectionChanged(object _0, SelectionChangedEventArgs e) {
            if (NumOfPagesSelector.SelectedItem == null) {
                return;
            }
            _numOfPages = (int)NumOfPagesSelector.SelectedItem;
            if (e.RemovedItems.Count == 0 || _viewMode == ViewMode.Scroll) {
                return;
            }
            if (StartStopAction(true)) {
                try {
                    RefreshLayout();
                } finally {
                    StartStopAction(false);
                }
            }
        }

        private CancellationTokenSource _autoScrollCts = new();
        public void StartStopAutoScroll(bool starting) {
            IsAutoScrolling = starting;
            AutoScrollBtn.IsChecked = starting;
            stopwatch.Reset();
            if (starting) {
                ShowActionIndicator(Symbol.Play, null);
                AutoScrollBtn.Icon = new SymbolIcon(Symbol.Pause);
                AutoScrollBtn.Label = "Stop Auto Page Turning / Scrolling";
                _autoScrollCts = new();
                Task.Run(() => ScrollAutomatically(_autoScrollCts.Token), _autoScrollCts.Token);
            }
            else {
                ShowActionIndicator(Symbol.Pause, null);
                _autoScrollCts.Cancel();
                AutoScrollBtn.Icon = new SymbolIcon(Symbol.Play);
                AutoScrollBtn.Label = "Start Auto Page Turning / Scrolling";
            }
        }

        private void SetCurrPageIndex(int idx) {
            bool alreadyIndicated = _pageChangedBySystem;
            _pageChangedBySystem = true;
            _currPageIndex = idx;
            PageNumText.Text = $"{idx + 1} of {CurrLoadedGallery.files.Length}";
            PageNavigator.SelectedIndex = idx;
            if (!alreadyIndicated) {
                _pageChangedBySystem = false;
            }
        }

        /**
         * <summary>Inserts images based on current page number</summary>
         */
        private async void RefreshLayout() {
            foreach (var panel in _groupedImagePanels) {
                panel.Children.Clear();
            }
            _scrollViewerItemsRepeater.ItemsSource = null;
            ImageContainer.Content = null;
            switch (_viewMode) {
                case ViewMode.Default:
                    _groupedImagePanels.Clear();
                    /*
                        example:
                        _images.Count = 22, _numOfPages = 4
                        22 / 4 = 5 r 2
                        -----------------
                        _groupedImagePanels = [0 ~ 3, 4 ~ 7, 8 ~ 11, 12 ~ 15, 16 ~ 19, 20 ~ 21]
                    */
                    for (int i = 0; i < _imageCollection.Count / _numOfPages; i++) {
                        Range range = (i * _numOfPages)..(Math.Min((i + 1) * _numOfPages, _imageCollection.Count));
                        _groupedImagePanels.Add(
                            new(_viewDirection, _imageCollection.Take(range), CurrLoadedGallery.files.Take(range))
                        );
                    }
                    _pageChangedBySystem = true;
                    int groupedPanelPageIndex = _currPageIndex / _numOfPages;
                    _flipView.SelectedIndex = groupedPanelPageIndex;
                    SetCurrPageIndex(groupedPanelPageIndex * _numOfPages);
                    _pageChangedBySystem = false;

                    ImageContainer.Content = _flipView;
                    await SetImageOrientationAndSize();
                    _flipView.Focus(FocusState.Programmatic);
                    break;
                case ViewMode.Scroll:
                    if (_viewDirection == ViewDirection.RightToLeft) {
                        _scrollViewerItemsRepeater.ItemsSource = _reverseImageCollection;
                    } else {
                        _scrollViewerItemsRepeater.ItemsSource = _imageCollection;
                    }

                    ImageContainer.Content = _scrollViewer;
                    await SetImageOrientationAndSize();

                    ScrollToPageIndex();
                    break;
            }
        }

        private void SetViewModeTextAndIcon() {
            switch (_viewMode) {
                case ViewMode.Default:
                    ViewModeBtn.Label = "View mode: Default";
                    ViewModeBtnIcon.Glyph = "\xF0E2";
                    break;
                case ViewMode.Scroll:
                    ViewModeBtn.Label = "View mode: Scroll";
                    ViewModeBtnIcon.Glyph = "\xECE7";
                    break;
            }
        }

        private void ViewModeBtn_Clicked(object _0, RoutedEventArgs _1) {
            if (StartStopAction(true)) {
                try {
                    _viewMode = _viewMode == ViewMode.Default ? ViewMode.Scroll : ViewMode.Default;
                    SetViewModeTextAndIcon();
                    SetScrollSpeedSlider();
                    RefreshLayout();
                } finally {
                    StartStopAction(false);
                }
            }
        }

        private double GetScrollOffsetFromPageIndex(int i) {
            double zoomFactor = _scrollViewer.ZoomFactor;
            return _viewDirection switch {
                ViewDirection.TopToBottom => i == 0 ? 0 : _scrollOffsetAccum.vertical[i - 1] * zoomFactor,
                ViewDirection.LeftToRight => i == 0 ? 0 : _scrollOffsetAccum.horizontal[i - 1] * zoomFactor,
                ViewDirection.RightToLeft =>
                    i == CurrLoadedGallery.files.Length - 1
                    ? 0
                    : _scrollOffsetAccum.horizontal[CurrLoadedGallery.files.Length - i - 2] * zoomFactor,
                _ => throw new NotImplementedException(),
            };
        }

        private int GetPageIndexFromScrollOffset() {
            double zoomFactor = _scrollViewer.ZoomFactor;
            double centerOffset;
            double[] scrollOffsetAccum;

            switch (_viewDirection) {
                case ViewDirection.TopToBottom:
                    centerOffset = _scrollViewer.VerticalOffset + _scrollViewer.ActualHeight / 2;
                    scrollOffsetAccum = _scrollOffsetAccum.vertical;
                    break;
                case ViewDirection.LeftToRight:
                    centerOffset = _scrollViewer.HorizontalOffset + _scrollViewer.ActualWidth / 2;
                    scrollOffsetAccum = _scrollOffsetAccum.horizontal;
                    break;
                case ViewDirection.RightToLeft:
                    centerOffset = _scrollViewer.ExtentWidth - (_scrollViewer.HorizontalOffset + _scrollViewer.ActualWidth / 2);
                    scrollOffsetAccum = _scrollOffsetAccum.horizontal;
                    break;
                default:
                    return CurrLoadedGallery.files.Length - 1;
            }

            for (int i = 0; i < scrollOffsetAccum.Length; i++) {
                if (centerOffset < scrollOffsetAccum[i] * zoomFactor) {
                    return i;
                }
            }

            return CurrLoadedGallery.files.Length - 1;
        }

        private double GetCurrScrollOffset() {
            return _viewDirection == ViewDirection.TopToBottom ? _scrollViewer.VerticalOffset : _scrollViewer.HorizontalOffset;
        }

        private async Task<double> GetMaxScrollOffset() {
            if (_viewDirection == ViewDirection.TopToBottom) {
                while (true) {
                    if (_scrollViewer.ExtentHeight != 0 && _scrollViewer.ActualHeight != 0) {
                        break;
                    } else {
                        await Task.Delay(200);
                    }
                }
            } else {
                while (true) {
                    if (_scrollViewer.ExtentWidth != 0 && _scrollViewer.ActualWidth != 0) {
                        break;
                    } else {
                        await Task.Delay(200);
                    }
                }
            }
            return _viewDirection == ViewDirection.TopToBottom
                ? _scrollViewer.ExtentHeight - _scrollViewer.ActualHeight
                : _scrollViewer.ExtentWidth - _scrollViewer.ActualWidth;
        }

        private async Task TryScrollToPageIndex() {
            double targetOffset = Math.Min(GetScrollOffsetFromPageIndex(_currPageIndex), await GetMaxScrollOffset());
            double lastOffset = GetCurrScrollOffset();
            int tryCount = 0;
            while (true) {
                if (_viewDirection == ViewDirection.TopToBottom) {
                    _scrollViewer.ScrollToVerticalOffset(targetOffset);
                } else {
                    _scrollViewer.ScrollToHorizontalOffset(targetOffset);
                }
                double currOffset = GetCurrScrollOffset();
                if (currOffset != lastOffset) {
                    tryCount = 0;
                }
                if (currOffset == targetOffset || tryCount > 2) {
                    break;
                }
                tryCount++;
                lastOffset = currOffset;
                await Task.Delay(200);
            }
        }

        private async void ScrollToPageIndex() {
            _scrollViewer.IsEnabled = false;
            try {
                if (_currPageIndex == 0) { // first page index
                    switch (_viewDirection) {
                        case ViewDirection.TopToBottom:
                            _scrollViewer.ScrollToVerticalOffset(0);
                            return;
                        case ViewDirection.LeftToRight:
                            _scrollViewer.ScrollToHorizontalOffset(0);
                            return;
                        case ViewDirection.RightToLeft:
                            await TryScrollToPageIndex();
                            return;
                    }
                } else if (_currPageIndex == CurrLoadedGallery.files.Length - 1) { // last page index
                    switch (_viewDirection) {
                        case ViewDirection.TopToBottom:
                            await TryScrollToPageIndex();
                            return;
                        case ViewDirection.LeftToRight:
                            await TryScrollToPageIndex();
                            return;
                        case ViewDirection.RightToLeft:
                            _scrollViewer.ScrollToHorizontalOffset(0);
                            return;
                    }
                } else {
                    await TryScrollToPageIndex();
                }
            } finally {
                _scrollViewer.IsEnabled = true;
            }
        }

        private void SetScrollSpeed(object slider, RangeBaseValueChangedEventArgs _0) {
            switch (_viewMode) {
                case ViewMode.Default:
                    _pageTurnDelay = ((Slider)slider).Value;
                    break;
                case ViewMode.Scroll:
                    _scrollSpeed = ((Slider)slider).Value;
                    break;
            }
        }

        public void ImageWatchingPage_PreviewKeyDown(object _, KeyRoutedEventArgs e) {
            if (CurrLoadedGallery == null) return;
            switch (e.Key) {
                case VirtualKey.L:
                    if ((bool)LoopBtn.IsChecked) {
                        LoopBtn.IsChecked = false;
                        ShowActionIndicator(Symbol.RepeatAll, GLYPH_CANCEL);
                    } else {
                        LoopBtn.IsChecked = true;
                        ShowActionIndicator(Symbol.RepeatAll, null);
                    }
                    break;
                case VirtualKey.Space:
                    if (!_isInAction) {
                        StartStopAutoScroll(!IsAutoScrolling);
                    }
                    break;
                case VirtualKey.V:
                    if (!_isInAction) ViewModeBtn_Clicked(null, null);
                    break;
                case VirtualKey.Left or VirtualKey.Right or VirtualKey.Down or VirtualKey.Up:
                    if (_viewMode == ViewMode.Default) {
                        if (_flipView.SelectedIndex == 0 && e.Key is VirtualKey.Left or VirtualKey.Up) {
                            _flipView.SelectedIndex = _flipView.Items.Count - 1;
                            e.Handled = true;
                        } else if (_flipView.SelectedIndex == _flipView.Items.Count - 1 && e.Key is VirtualKey.Right or VirtualKey.Down) {
                            _flipView.SelectedIndex = 0;
                            e.Handled = true;
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        public void Window_SizeChanged() {
            if (CurrLoadedGallery != null) {
                CheckAndUpdateImageSources();
            }
        }

        // for updating auto scrolling in sync with real time
        private static readonly Stopwatch stopwatch = new();

        private async void ScrollAutomatically(CancellationToken ct) {
            // TODO
            //while (IsAutoScrolling) {
            //    switch (_viewMode) {
            //        case ViewMode.Default:
            //            if (_currPage + 1 == _mw.CurrLoadedGallery.files.Length && !_isLooping) {
            //                DispatcherQueue.TryEnqueue(() => StartStopAutoScroll(false));
            //                return;
            //            }
            //            try {
            //                await Task.Delay((int)(_pageTurnDelay * 1000), ct);
            //            } catch (TaskCanceledException) {
            //                return;
            //            }
            //            if (IsAutoScrolling) {
            //                if (_currPage + 1 == _mw.CurrLoadedGallery.files.Length && !_isLooping) {
            //                    DispatcherQueue.TryEnqueue(() => StartStopAutoScroll(false));
            //                    return;
            //                }
            //                DispatcherQueue.TryEnqueue(() => { UpdatePageNum(_numOfPages); });
            //                DispatcherQueue.TryEnqueue(UpdateImages);
            //            }
            //            break;
            //        case ViewMode.Scroll:
            //            try {
            //                await Task.Delay(10, ct);
            //            } catch (TaskCanceledException) {
            //                return;
            //            }
            //            MainScrollViewer.DispatcherQueue.TryEnqueue(() => {
            //                bool isEndOfPage = _viewDirection switch {
            //                    ViewDirection.TopToBottom => MainScrollViewer.VerticalOffset == MainScrollViewer.ScrollableHeight,
            //                    ViewDirection.LeftToRight => MainScrollViewer.HorizontalOffset == MainScrollViewer.ScrollableWidth,
            //                    ViewDirection.RightToLeft => MainScrollViewer.HorizontalOffset == 0,
            //                    _ => throw new NotImplementedException("Unhandled ViewDirection Mode")
            //                };
            //                if (isEndOfPage) {
            //                    if (_isLooping) {
            //                        switch (_viewDirection) {
            //                            case ViewDirection.TopToBottom:
            //                                MainScrollViewer.ScrollToVerticalOffset(0);
            //                                break;
            //                            case ViewDirection.LeftToRight:
            //                                MainScrollViewer.ScrollToHorizontalOffset(0);
            //                                break;
            //                            case ViewDirection.RightToLeft:
            //                                MainScrollViewer.ScrollToHorizontalOffset(MainScrollViewer.ScrollableWidth);
            //                                break;
            //                        }
            //                    } else {
            //                        StartStopAutoScroll(false);
            //                        return;
            //                    }
            //                }
            //                else {
            //                    stopwatch.Stop();
            //                    switch (_viewDirection) {
            //                        case ViewDirection.TopToBottom:
            //                            MainScrollViewer.ScrollToVerticalOffset(MainScrollViewer.VerticalOffset + _scrollSpeed * stopwatch.ElapsedMilliseconds);
            //                            break;
            //                        case ViewDirection.LeftToRight:
            //                            MainScrollViewer.ScrollToHorizontalOffset(MainScrollViewer.HorizontalOffset + _scrollSpeed * stopwatch.ElapsedMilliseconds);
            //                            break;
            //                        case ViewDirection.RightToLeft:
            //                            MainScrollViewer.ScrollToHorizontalOffset(MainScrollViewer.HorizontalOffset - _scrollSpeed * stopwatch.ElapsedMilliseconds);
            //                            break;
            //                    }
            //                    stopwatch.Restart();
            //                }
            //            });
            //            break;
            //    }
            //}
        }

        private void EnableControls(bool enable) {
            RefreshBtn.IsEnabled = enable;
            ViewModeBtn.IsEnabled = enable;
            ScrollSpeedSlider.IsEnabled = enable;
            AutoScrollBtn.IsEnabled = enable;
            LoopBtn.IsEnabled = enable;
            ViewDirectionSelector.IsEnabled = enable;
            NumOfPagesSelector.IsEnabled = enable;
            PageNavigator.IsEnabled = enable;
            if (IsAutoScrolling) StartStopAutoScroll(false);
        }

        /**
         * <returns><c>true</c> if action is permitted, otherwise, <c>false</c></returns>
         */
        public bool StartStopAction(bool doOrFinishAction) {
            if (doOrFinishAction) {
                if (!_isInAction) {
                    _isInAction = true;
                    EnableControls(false);
                    SearchPage.BookmarkItems.ForEach(bmItem => bmItem.EnableBookmarkClick(false));
                    return true;
                }
                return false;
            }
            EnableControls(true);
            SearchPage.BookmarkItems.ForEach(bmItem => bmItem.EnableBookmarkClick(true));
            _isInAction = false;
            return true;
        }

        public void LoadGallery(Gallery gallery) {
            App.MainWindow.SwitchPage();
            if (CurrLoadedGallery != null && gallery.id == CurrLoadedGallery.id) {
                return;
            }
            if (StartStopAction(true)) {
                try {
                    string imageDir = Path.Combine(IMAGE_DIR, gallery.id);
                    if (!Directory.Exists(imageDir)) {
                        return;
                    }

                    CurrLoadedGallery = gallery;

                    _imageCollection.Clear();
                    _reverseImageCollection.Clear();
                    foreach (var panel in _groupedImagePanels) {
                        panel.Children.Clear();
                    }
                    _groupedImagePanels.Clear();

                    PageNavigator.ItemsSource = Enumerable.Range(1, gallery.files.Length + 1).ToList();
                    PageNavigator.SelectedIndex = 0;
                    SetCurrPageIndex(0);
                    for (int i = 0; i < gallery.files.Length; i++) {
                        Image image = new();
                        string[] files = Directory.GetFiles(imageDir, i.ToString() + ".*");
                        if (files.Length > 0) {
                            image.Source = new BitmapImage(new(files[0]));
                        }
                        _imageCollection.Add(image);
                        _reverseImageCollection.Insert(0, image);
                    }
                    RefreshLayout();
                } finally {
                    StartStopAction(false);
                }
            }
        }

        private async void CheckAndUpdateImageSources() {
            if (StartStopAction(true)) {
                try {
                    string imageDir = Path.Combine(IMAGE_DIR, CurrLoadedGallery.id);
                    if (!Directory.Exists(imageDir)) {
                        return;
                    }
                    for (int i = 0; i < _imageCollection.Count; i++) {
                        Image image = _imageCollection[i];
                        if (image.Source == null) {
                            string[] files = Directory.GetFiles(imageDir, i.ToString() + ".*");
                            if (files.Length > 0) {
                                image.Source = new BitmapImage(new(files[0]));
                            }
                        }
                    }
                    await SetImageOrientationAndSize();
                } finally {
                    StartStopAction(false);
                }
            }
        }

        private void RefreshBtn_Clicked(object _0, RoutedEventArgs _1) {
            ShowActionIndicator(Symbol.Refresh, null);
            CheckAndUpdateImageSources();
        }

        public Settings GetSettings() {
            return new(_viewMode, _viewDirection, _scrollSpeed, _numOfPages, _pageTurnDelay, _isLooping);
        }
    }
}
