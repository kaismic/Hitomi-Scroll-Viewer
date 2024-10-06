using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.ViewModels.BrowsePageVMs;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Windows.UI;

namespace HitomiScrollViewerLib.Views.BrowsePageViews {
    public sealed partial class GalleryBrowseItem : UserControl {
        private SolidColorBrush TitleBackgroundBrush {
            get => (SolidColorBrush)GetValue(TitleBackgroundBrushProperty);
            set => SetValue(TitleBackgroundBrushProperty, value);
        }
        public static readonly DependencyProperty TitleBackgroundBrushProperty =
            DependencyProperty.Register(
                nameof(TitleBackgroundBrush),
                typeof(SolidColorBrush),
                typeof(GalleryBrowseItem),
                null
            );

        private SolidColorBrush SubtitleBackgroundBrush {
            get => (SolidColorBrush)GetValue(SubtitleBackgroundBrushProperty);
            set => SetValue(SubtitleBackgroundBrushProperty, value);
        }
        public static readonly DependencyProperty SubtitleBackgroundBrushProperty =
            DependencyProperty.Register(
                nameof(SubtitleBackgroundBrush),
                typeof(SolidColorBrush),
                typeof(GalleryBrowseItem),
                null
            );

        private SolidColorBrush TextForegroundBrush {
            get => (SolidColorBrush)GetValue(TextForegroundBrushProperty);
            set => SetValue(TextForegroundBrushProperty, value);
        }
        public static readonly DependencyProperty TextForegroundBrushProperty =
            DependencyProperty.Register(
                nameof(TextForegroundBrush),
                typeof(SolidColorBrush),
                typeof(GalleryBrowseItem),
                null
            );


        public GalleryBrowseItemVM ViewModel {
            get => (GalleryBrowseItemVM)GetValue(ViewModelProperty);
            set {
                if (ViewModel == null) {
                    value.WidthChanged += (double width) => {
                        // return if initial images are not yet added
                        if (ThumbnailImagePanel.ItemsSource == null) {
                            return;
                        }
                        if (Monitor.TryEnter(_addingImageLock)) {
                            // add more images if there are empty spaces
                            try {
                                TryAddThumnailImages();
                            } finally {
                                Monitor.Exit(_addingImageLock);
                            }
                        }
                    };
                }
                SetValue(ViewModelProperty, value);

                string baseColorKey = value.Gallery.GalleryType.GalleryType.ToString() + "Color";
                string[] colorKeys = Enumerable.Range(0, 3).Select(i => baseColorKey + i).ToArray();
                bool isLightTheme = RequestedTheme == ElementTheme.Light;
                TitleBackgroundBrush = new((Color)Resources[colorKeys[1]]);
                SubtitleBackgroundBrush = new((Color)Resources[colorKeys[isLightTheme ? 2 : 0]]);
                TextForegroundBrush = new((Color)Resources[colorKeys[isLightTheme ? 0 : 2]]);
            }
        }
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(
                nameof(ViewModel),
                typeof(GalleryBrowseItemVM),
                typeof(GalleryBrowseItem),
                null
            );

        private readonly object _addingImageLock = new();

        public GalleryBrowseItem() {
            InitializeComponent();
            Loaded += GalleryBrowseItem_Loaded;

            for (int i = 0; i < SubtitleGrid.Children.Count; i++) {
                SubtitleGrid.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
                Grid.SetColumn(SubtitleGrid.Children[i] as FrameworkElement, i);
                if (SubtitleGrid.Children[i] is TextBlock tb) {
                    tb.IsTextSelectionEnabled = true;
                }
            }
        }

        private readonly ObservableCollection<string> _imageFilePaths = [];
        private const int MAX_THUMBNAIL_IMAGE_COUNT = 5;

        private void GalleryBrowseItem_Loaded(object _0, RoutedEventArgs _1) {
            Loaded -= GalleryBrowseItem_Loaded;
            // add initial thumbnail images
            HitomiContext.Main.Galleries.Where(g => g.Id == ViewModel.Gallery.Id).Include(g => g.Files).Load();
            TryAddThumnailImages();
            ThumbnailImagePanel.ItemsSource = _imageFilePaths;
        }

        private void TryAddThumnailImages() {
            double remainingWidth = ThumbnailImagePanelContainer.ActualWidth - ThumbnailImagePanel.ActualWidth;
            if (remainingWidth <= 0 || _imageFilePaths.Count >= MAX_THUMBNAIL_IMAGE_COUNT) {
                return;
            }
            foreach (ImageInfo imageInfo in ViewModel.Gallery.Files.OrderBy(f => f.Index).Skip(_imageFilePaths.Count)) {
                if (remainingWidth <= 0 || _imageFilePaths.Count >= MAX_THUMBNAIL_IMAGE_COUNT) {
                    return;
                }
                _imageFilePaths.Add(imageInfo.ImageFilePath);
                remainingWidth = ThumbnailImagePanelContainer.ActualWidth - ThumbnailImagePanel.ActualWidth;
            }
        }
    }
}
