using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.ObjectModel;
using System.IO;
using static Hitomi_Scroll_Viewer.SearchPage;
using static Hitomi_Scroll_Viewer.Utils;

namespace Hitomi_Scroll_Viewer.SearchPageComponent {
    public sealed partial class BookmarkItem : Grid {
        private static readonly Thickness THUMBNAIL_IMG_MARGIN = new(8);
        public readonly Gallery gallery;
        private readonly ObservableCollection<Image> _thumbnailImages = [];
        private readonly string _imageDir;

        public BookmarkItem(Gallery newGallery, SearchPage sp) {
            InitializeComponent();
            Loaded += InsertThumbnailImages;

            gallery = newGallery;
            _imageDir = Path.Combine(IMAGE_DIR, gallery.id);

            TitleTextBlock.Text = gallery.title;
            string artistsText = gallery.GetArtists();
            if (artistsText == null) {
                ArtistTextBlock.Text = "Artist: N/A";
            } else {
                ArtistTextBlock.Text = "Artist: " + artistsText;
            }
            IdTextBlock.Text = "ID: " + gallery.id;

            foreach (TextBlock textblock in new[] { TitleTextBlock, ArtistTextBlock, IdTextBlock }) {
                textblock.TextWrapping = TextWrapping.WrapWholeWords;
                textblock.FontSize = 24;
                textblock.IsTextSelectionEnabled = true;
            }

            ImageContainerWrapper.Click += (_, _) => { LoadBookmark(gallery); };
            ImageContainer.ItemClick += (_, _) => { LoadBookmark(gallery); };
            RemoveBtn.Click += (_, _) => { sp.RemoveBookmark(this); };
            MoveUpBtn.Click += (_, _) => { sp.SwapBookmarks(this, BookmarkSwapDirection.Up); };
            MoveDownBtn.Click += (_, _) => { sp.SwapBookmarks(this, BookmarkSwapDirection.Down); };            
        }

        private void InsertThumbnailImages (object sender, RoutedEventArgs e) {
            Loaded -= InsertThumbnailImages;
            // add thumbnail images
            // Determine the number of thumbnail images which fits into ImageContainerWrapper.ActualWidth
            double widthSum = 0;
            double containerWidth = ImageContainerWrapper.ActualWidth;
            bool dirExists = Directory.Exists(_imageDir);
            for (int i = 0; i < gallery.files.Length; i++) {
                double width = gallery.files[i].width * THUMBNAIL_IMG_HEIGHT / gallery.files[i].height;
                widthSum += width + THUMBNAIL_IMG_MARGIN.Left + THUMBNAIL_IMG_MARGIN.Right;
                if (widthSum > containerWidth) {
                    break;
                }
                _thumbnailImages.Add(new() { Width = width, Height = THUMBNAIL_IMG_HEIGHT, Margin = THUMBNAIL_IMG_MARGIN });
                if (dirExists) {
                    string[] files = Directory.GetFiles(_imageDir, i.ToString() + ".*");
                    if (files.Length > 0) {
                        _thumbnailImages[i].Source = new BitmapImage(new(files[0]));
                    }
                }
            }
            ImageContainer.ItemsSource = _thumbnailImages;
        }

        public void ReloadImages() {
            if (!Directory.Exists(_imageDir)) {
                return;
            }
            for (int i = 0; i < _thumbnailImages.Count; i++) {
                string[] files = Directory.GetFiles(_imageDir, i.ToString() + ".*");
                if (files.Length > 0 && _thumbnailImages[i].Source == null) {
                    _thumbnailImages[i].Source = new BitmapImage(new(files[0]));
                }
            }
        }

        public void EnableBookmarkLoading(bool enable) {
            ImageContainerWrapper.IsEnabled = enable;
        }
    }
}