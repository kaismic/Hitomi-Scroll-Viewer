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
    public sealed partial class BookmarkItem : StackPanel {
        public readonly Gallery gallery;
        private readonly Image[] _images;
        private readonly HyperlinkButton _hb;
        public BookmarkItem(Gallery newGallery, SearchPage sp) {
            InitializeComponent();

            gallery = newGallery;

            Orientation = Orientation.Vertical;
            BorderBrush = new SolidColorBrush(Colors.Black);
            BorderThickness = new Thickness(1);

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
            _hb.Click += sp.HandleBookmarkClick;
            Children.Add(_hb);

            Grid imageContainer = new();
            Children.Add(imageContainer);

            // add thumbnail images
            _images = new Image[THUMBNAIL_IMG_NUM];
            for (int i = 0; i < THUMBNAIL_IMG_NUM; i++) {
                imageContainer.ColumnDefinitions.Add(
                    new ColumnDefinition() {
                        Width = new GridLength(2, GridUnitType.Star)
                    }
                );
                int imgIdx = i * gallery.files.Length / THUMBNAIL_IMG_NUM;
                    _images[i] = new() {
                        Source = new BitmapImage(new(IMAGE_DIR + @"\" + gallery.id + @"\" + imgIdx + IMAGE_EXT)),
                        Width = THUMBNAIL_IMG_WIDTH,
                        Height = THUMBNAIL_IMG_WIDTH * gallery.files[i].height / gallery.files[i].width,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                Grid.SetColumn(_images[i], i);
                imageContainer.Children.Add(_images[i]);
            }

            // add another ColumnDefinition for remove button
            imageContainer.ColumnDefinitions.Add(
                new ColumnDefinition() {
                    Width = new GridLength(1, GridUnitType.Star)
                }
            ); Button removeBtn = new() {
                Content = new TextBlock() {
                    Text = "Remove",
                    TextWrapping = TextWrapping.WrapWholeWords,
                },
                FontSize = 18,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(removeBtn, THUMBNAIL_IMG_NUM);
            removeBtn.Click += sp.RemoveBookmark;
            imageContainer.Children.Add(removeBtn);
        }

        public void EnableButton(bool enable) {
            _hb.IsEnabled = enable;
        }
    }
}
