using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using Windows.UI.Text;
using static Hitomi_Scroll_Viewer.MainWindow;
using static Hitomi_Scroll_Viewer.SearchPage;

namespace Hitomi_Scroll_Viewer.SearchPageComponent {
    public sealed partial class BookmarkItem : Grid {
        public readonly Gallery gallery;
        private readonly Grid _imageContainer;
        private readonly Image[] _images;
        private readonly HyperlinkButton _hb;
        public BookmarkItem(Gallery newGallery, SearchPage sp) {
            InitializeComponent();

            gallery = newGallery;

            BorderBrush = new SolidColorBrush(Colors.Black);
            BorderThickness = new Thickness(1);

            RowDefinitions.Add(new());
            RowDefinitions.Add(new());
            ColumnDefinitions.Add(new() { Width = new GridLength(15, GridUnitType.Star) });
            ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });

            // add clickable hyperlink which loads bookmarked gallery
            TextBlock hbText = new() {
                TextWrapping = TextWrapping.WrapWholeWords,
                FontSize = 24,
            };
            string artists = gallery.GetArtists();
            if (artists == null) {
                hbText.Inlines.Add(new Run() { Text = gallery.title + Environment.NewLine });
                hbText.Inlines.Add(new Run() { Text = "N/A" + Environment.NewLine, FontStyle = FontStyle.Italic });
                hbText.Inlines.Add(new Run() { Text = gallery.id });
            } else {
                hbText.Text = gallery.title + Environment.NewLine + artists + Environment.NewLine + gallery.id;
            }

            _hb = new() { Content = hbText };
            SetRow(_hb, 0);
            SetColumn(_hb, 0);
            Children.Add(_hb);
            _hb.Click += HandleBookmarkClick;

            _imageContainer = new();
            SetRow(_imageContainer, 1);
            SetColumn(_imageContainer, 0);
            Children.Add(_imageContainer);

            // add thumbnail images
            _images = new Image[THUMBNAIL_IMG_NUM];
            for (int i = 0; i < THUMBNAIL_IMG_NUM; i++) {
                _imageContainer.ColumnDefinitions.Add(new());
                int imgIdx = i * gallery.files.Length / THUMBNAIL_IMG_NUM;
                _images[i] = new() {
                    Width = THUMBNAIL_IMG_WIDTH,
                    Height = THUMBNAIL_IMG_WIDTH * gallery.files[imgIdx].height / gallery.files[imgIdx].width,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                string path = Path.Combine(IMAGE_DIR, gallery.id, imgIdx.ToString()) + IMAGE_EXT;
                if (File.Exists(path)) {
                    _images[i].Source = new BitmapImage(new(path));
                }
                SetColumn(_images[i], i);
                _imageContainer.Children.Add(_images[i]);
            }

            // add remove button
            Button removeBtn = new() {
                Content = new TextBlock() {
                    Text = "Remove",
                    TextWrapping = TextWrapping.WrapWholeWords,
                },
                FontSize = 18,
            };
            SetRow(removeBtn, 0);
            SetRowSpan(removeBtn, 2);
            SetColumn(removeBtn, 1);
            Children.Add(removeBtn);
            removeBtn.Click += sp.RemoveBookmark;
        }

        private void HandleBookmarkClick(object _0, RoutedEventArgs _1) {
            LoadBookmark(gallery);
        }

        public void ReloadImages() {
            for (int i = 0; i < THUMBNAIL_IMG_NUM; i++) {
                int imgIdx = i * gallery.files.Length / THUMBNAIL_IMG_NUM;
                string path = Path.Combine(IMAGE_DIR, gallery.id, imgIdx.ToString()) + IMAGE_EXT;
                if (File.Exists(path)) {
                    _images[i].Source = new BitmapImage(new(path));
                }
            }
        }

        public void EnableButton(bool enable) {
            _hb.IsEnabled = enable;
        }
    }
}
