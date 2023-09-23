using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using Windows.UI.Text;
using static Hitomi_Scroll_Viewer.SearchPage;
using static Hitomi_Scroll_Viewer.Utils;

namespace Hitomi_Scroll_Viewer.SearchPageComponent {
    public sealed partial class BookmarkItem : Grid {
        public readonly Gallery gallery;
        private readonly Grid _imageContainer;
        private readonly Image[] _images;
        private readonly HyperlinkButton _hb;
        private readonly Button _removeBtn;
        public BookmarkItem(Gallery newGallery, SearchPage sp) {
            InitializeComponent();

            gallery = newGallery;

            Margin = new(8);
            BorderBrush = new SolidColorBrush(Colors.Black);
            BorderThickness = new Thickness(1);

            ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
            ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Auto) });
            RowDefinitions.Add(new());
            RowDefinitions.Add(new());

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

            _hb = new() {
                Content = hbText,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            SetRow(_hb, 0);
            SetColumn(_hb, 0);
            Children.Add(_hb);
            _hb.Click += HandleBookmarkClick;

            _imageContainer = new();
            SetRow(_imageContainer, 1);
            SetColumn(_imageContainer, 0);
            SetColumnSpan(_imageContainer, 2);
            Children.Add(_imageContainer);

            // add thumbnail images
            _images = new Image[THUMBNAIL_IMG_NUM];
            string imageDir = Path.Combine(IMAGE_DIR, gallery.id);
            bool dirExists = Directory.Exists(imageDir);
            for (int i = 0; i < THUMBNAIL_IMG_NUM; i++) {
                _imageContainer.ColumnDefinitions.Add(new());
                int idx = i * gallery.files.Length / THUMBNAIL_IMG_NUM;
                _images[i] = new() {
                    MaxHeight = THUMBNAIL_IMG_MAXHEIGHT,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                if (dirExists) {
                    string[] file = Directory.GetFiles(imageDir, idx.ToString() + ".*");
                    if (file.Length > 0) {
                        _images[i].Source = new BitmapImage(new(file[0]));
                    }
                }
                SetColumn(_images[i], i);
                _imageContainer.Children.Add(_images[i]);
            }

            // add remove button
            _removeBtn = new() {
                Content = new SymbolIcon(Symbol.Delete),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top
            };
            SetRow(_removeBtn, 0);
            SetColumn(_removeBtn, 1);
            Children.Add(_removeBtn);
            _removeBtn.Click += sp.RemoveBookmark;
        }

        private void HandleBookmarkClick(object _0, RoutedEventArgs _1) {
            LoadBookmark(gallery);
        }

        public void ReloadImages() {
            string imageDir = Path.Combine(IMAGE_DIR, gallery.id);
            for (int i = 0; i < THUMBNAIL_IMG_NUM; i++) {
                int idx = i * gallery.files.Length / THUMBNAIL_IMG_NUM;
                string[] file = Directory.GetFiles(imageDir, idx.ToString() + ".*");
                if (file.Length > 0) {
                    _images[i].Source = new BitmapImage(new(file[0]));
                }
            }
        }

        public void EnableHyperlinkButton(bool enable) {
            _hb.IsEnabled = enable;
        }

        public void EnableRemoveButton(bool enable) {
            _removeBtn.IsEnabled = enable;
        }
    }
}
