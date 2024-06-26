using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.ApplicationModel.Resources;
using System.IO;
using static Hitomi_Scroll_Viewer.Resources;
using static Hitomi_Scroll_Viewer.MainWindowComponent.SearchPage;
using static Hitomi_Scroll_Viewer.Utils;

namespace Hitomi_Scroll_Viewer.MainWindowComponent.SearchPageComponent {
    public sealed partial class BookmarkItem : Grid {
        private static readonly ResourceMap ResourceMap = MainResourceMap.GetSubtree("BookmarkItem");
        private static readonly string TEXT_ARTIST = ResourceMap.GetValue("Text_Artist").ValueAsString;
        private static readonly int THUMBNAIL_IMG_HEIGHT = 256;
        public readonly Gallery gallery;
        private readonly ItemsChangeObservableCollection<Image> _thumbnailImages = [];
        private readonly string _imageDir;
        public bool IsDownloading;

        public BookmarkItem(Gallery newGallery, bool initIsDownloading) {
            InitializeComponent();

            gallery = newGallery;
            _imageDir = Path.Combine(IMAGE_DIR, gallery.id);
            IsDownloading = initIsDownloading;

            Loaded += InitThumbnailImagesOnLoad;
            void InitThumbnailImagesOnLoad(object _0, RoutedEventArgs _1) {
                Loaded -= InitThumbnailImagesOnLoad;
                CreateThumbnailImages();
                if (!Directory.Exists(_imageDir)) {
                    return;
                }
                if (IsDownloading) {
                    EnableRemoveBtn(false);
                }
                for (int i = 0; i < _thumbnailImages.Count; i++) {
                    string[] files = Directory.GetFiles(_imageDir, i.ToString() + ".*");
                    if (files.Length > 0) {
                        _thumbnailImages[i].Source = new BitmapImage(new(files[0])) {
                            DecodePixelHeight = THUMBNAIL_IMG_HEIGHT
                        };
                    }
                }
                _thumbnailImages.NotifyItemChange();
            }

            TitleTextBlock.Text = gallery.title;
            string artistsText = gallery.GetArtists();
            ArtistTextBlock.Text = artistsText == null ? TEXT_ARTIST + ": N/A" : TEXT_ARTIST + ": " + artistsText;
            IdTextBlock.Text = "ID: " + gallery.id;

            ImageContainerWrapper.Click += (_, _) => MainWindow.ImageWatchingPage.LoadGallery(gallery);
            RemoveBtn.Click += (_, _) => MainWindow.SearchPage.DeleteBookmark(this);
            MoveUpBtn.Click += (_, _) => MainWindow.SearchPage.SwapBookmarks(this, BookmarkSwapDirection.Up);
            MoveDownBtn.Click += (_, _) => MainWindow.SearchPage.SwapBookmarks(this, BookmarkSwapDirection.Down);
        }

        private void CreateThumbnailImages() {
            // Determine the number of thumbnail images which fit into ImageContainerWrapper.ActualWidth
            double totalWidthSum = 0;
            double imageWrapperWidth = ImageContainerWrapper.ActualWidth;
            for (int i = 0; i < gallery.files.Length; i++) {
                double width = THUMBNAIL_IMG_HEIGHT * gallery.files[i].width / gallery.files[i].height;
                totalWidthSum += width;
                if (i > 0) {
                    totalWidthSum += ImageContainer.Spacing;
                }
                if (totalWidthSum > imageWrapperWidth) {
                    break;
                }
                _thumbnailImages.Add(new() {
                    Height = THUMBNAIL_IMG_HEIGHT,
                    Width = width
                });
            }
        }

        public void UpdateSingleImage(int i) {
            if (i >= _thumbnailImages.Count) {
                return;
            }
            string[] files = Directory.GetFiles(_imageDir, i.ToString() + ".*");
            if (files.Length > 0 && _thumbnailImages[i].Source == null) {
                _thumbnailImages[i].Source = new BitmapImage(new(files[0])) {
                    DecodePixelHeight = THUMBNAIL_IMG_HEIGHT
                };
                _thumbnailImages.NotifyItemChange();
            }
        }

        public void EnableRemoveBtn(bool enable) {
            RemoveBtn.IsEnabled = enable;
        }

        public void EnableBookmarkClick(bool enable) {
            ImageContainerWrapper.IsEnabled = enable;
            ImageContainerWrapper.Opacity = enable ? 1 : 0.25;
        }
    }
}