using HitomiScrollViewerLib.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.ApplicationModel.Resources;
using System.IO;
using static HitomiScrollViewerLib.SharedResources;
using static HitomiScrollViewerLib.Utils;

namespace HitomiScrollViewerLib.Controls.SearchPageComponents {
    public sealed partial class BookmarkItem : Grid {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree("BookmarkItem");
        private static readonly string NOTIFICATION_ALREADY_DOWNLOADING = _resourceMap.GetValue("Notification_AlreadyDownloading").ValueAsString;

        private static readonly string TEXT_ARTIST = _resourceMap.GetValue("Text_Artist").ValueAsString;
        private static readonly int THUMBNAIL_IMG_HEIGHT = 256;
        public readonly Gallery gallery;
        private readonly ItemsChangeObservableCollection<Image> _thumbnailImages = [];
        private readonly string _imageDir;
        public bool IsDownloading;

        public BookmarkItem(Gallery newGallery, bool initIsDownloading) {
            InitializeComponent();

            gallery = newGallery;
            _imageDir = Path.Combine(IMAGE_DIR_V2, gallery.Id.ToString());
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

            //TitleTextBlock.Text = gallery.Title;
            //string artistsText = string.Join(", ", gallery.Artists);
            //ArtistTextBlock.Text = artistsText == null ? TEXT_ARTIST + ": N/A" : TEXT_ARTIST + ": " + artistsText;
            //IdTextBlock.Text = "ID: " + gallery.Id;

            //ImageContainerWrapper.Click += (_, _) => MainWindow.ViewPage.LoadGallery(gallery);
            //RemoveBtn.Click += (_, _) => MainWindow.SearchPage.RemoveBookmark(this);
            //DownloadBtn.Click += (_, _) => {
            //    if (!MainWindow.SearchPage.TryDownload(gallery.id, this)) {
            //        MainWindow.NotifyUser(NOTIFICATION_ALREADY_DOWNLOADING, "");
            //    }
            //};
            //MoveUpBtn.Click += (_, _) => MainWindow.SearchPage.SwapBookmarks(this, BookmarkSwapDirection.Up);
            //MoveDownBtn.Click += (_, _) => MainWindow.SearchPage.SwapBookmarks(this, BookmarkSwapDirection.Down);
        }

        private void CreateThumbnailImages() {
            //// Determine the number of thumbnail images which fit into ImageContainerWrapper.ActualWidth
            //double totalWidthSum = 0;
            //double imageWrapperWidth = ImageContainerWrapper.ActualWidth;
            //for (int i = 0; i < gallery.files.Length; i++) {
            //    double width = THUMBNAIL_IMG_HEIGHT * gallery.files[i].width / gallery.files[i].height;
            //    totalWidthSum += width;
            //    if (i > 0) {
            //        totalWidthSum += ImageContainer.Spacing;
            //    }
            //    if (totalWidthSum > imageWrapperWidth) {
            //        break;
            //    }
            //    _thumbnailImages.Add(new() {
            //        Height = THUMBNAIL_IMG_HEIGHT,
            //        Width = width
            //    });
            //}
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