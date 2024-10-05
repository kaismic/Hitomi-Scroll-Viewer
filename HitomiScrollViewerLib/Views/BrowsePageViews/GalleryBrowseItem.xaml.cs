using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.ViewModels.BrowsePageVMs;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Windows.UI;

namespace HitomiScrollViewerLib.Views.BrowsePageViews {
    public sealed partial class GalleryBrowseItem : UserControl {
        private SolidColorBrush TitleBackgroundBrush { get; set; }
        private SolidColorBrush SubtitleBackgroundBrush { get; set; }
        private SolidColorBrush TextForegroundBrush { get; set; }

        private GalleryBrowseItemVM _viewModel;
        public GalleryBrowseItemVM ViewModel {
            get => _viewModel;
            set {
                if (_viewModel != null) {
                    return;
                }
                _viewModel = value;
                string baseColorKey = value.Gallery.GalleryType.GalleryType.ToString() + "Color";
                string[] colorKeys = Enumerable.Range(0, 3).Select(i => baseColorKey + i).ToArray();
                bool isLightTheme = RequestedTheme == ElementTheme.Light;
                TitleBackgroundBrush = new((Color)Resources[colorKeys[1]]);
                SubtitleBackgroundBrush = new((Color)Resources[colorKeys[isLightTheme ? 2 : 0]]);
                TextForegroundBrush = new((Color)Resources[colorKeys[isLightTheme ? 0 : 2]]);
            }
        }

        public GalleryBrowseItem() {
            InitializeComponent();
            Loaded += GalleryBrowseItem_Loaded;

            for (int i = 0; i < MainGrid.Children.Count; i++) {
                MainGrid.RowDefinitions.Add(new() { Height = GridLength.Auto });
                Grid.SetRow(MainGrid.Children[i] as FrameworkElement, i);
            }
            for (int i = 0; i < SubtitleGrid.Children.Count; i++) {
                SubtitleGrid.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
                Grid.SetColumn(SubtitleGrid.Children[i] as FrameworkElement, i);
                if (SubtitleGrid.Children[i] is TextBlock tb) {
                    tb.IsTextSelectionEnabled = true;
                }
            }
        }

        private readonly List<string> _imageFilePaths = [];
        public const int MAX_THUMBNAIL_IMAGE_COUNT = 8;

        private void GalleryBrowseItem_Loaded(object _0, RoutedEventArgs _1) {
            Loaded -= GalleryBrowseItem_Loaded;
            // add thumbnail images
            Debug.WriteLine("GalleryBrowseItem_Loaded ActualWidth = " + ActualWidth);
            double remainingWidth = ActualWidth;
            HitomiContext.Main.Galleries.Where(g => g.Id == ViewModel.Gallery.Id).Include(g => g.Files).Load();
            foreach (ImageInfo imageInfo in ViewModel.Gallery.Files.OrderBy(f => f.Index)) {
                if (remainingWidth <= 0 || _imageFilePaths.Count >= 8) {
                    break;
                }
                _imageFilePaths.Add(imageInfo.ImageFilePath);
                remainingWidth = ActualWidth - ThumbnailImagePanel.ActualWidth;
                Debug.WriteLine("remainingWidth = " + remainingWidth);
                Debug.WriteLine("_imageFilePaths.Count = " + _imageFilePaths.Count);
            }
            ThumbnailImagePanel.ItemsSource = _imageFilePaths;
        }
    }
}
