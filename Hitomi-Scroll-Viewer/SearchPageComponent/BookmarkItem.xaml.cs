using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using static Hitomi_Scroll_Viewer.MainWindow;
using static Hitomi_Scroll_Viewer.SearchPage;

namespace Hitomi_Scroll_Viewer.SearchPageComponent {
    public sealed partial class BookmarkItem : StackPanel {
        public readonly Gallery bmGallery;
        private readonly Image[] _images;
        private readonly HyperlinkButton _hb;
        private readonly bool _imageNotFound = false;
        public BookmarkItem(Gallery newGallery, SearchPage sp) {
            InitializeComponent();

            bmGallery = newGallery;

            Orientation = Orientation.Vertical;
            BorderBrush = new SolidColorBrush(Colors.Black);
            BorderThickness = new Thickness(1);

            _hb = new() {
                Content = new TextBlock() {
                    Text = bmGallery.title + Environment.NewLine + bmGallery.id,
                    TextWrapping = TextWrapping.WrapWholeWords,
                    FontSize = 24,
                },
            };
            _hb.Click += sp.HandleBookmarkClick;
            Children.Add(_hb);

            Grid imageContainer = new();
            Children.Add(imageContainer);

            // add thumbnail images
            if (Directory.Exists(IMAGE_DIR + @"\" + bmGallery.id)) {
                _images = new Image[THUMBNAIL_IMG_NUM];
                for (int i = 0; i < THUMBNAIL_IMG_NUM; i++) {
                    imageContainer.ColumnDefinitions.Add(
                        new ColumnDefinition() {
                            Width = new GridLength(2, GridUnitType.Star)
                        }
                    );
                    int imgIdx = i * bmGallery.files.Count / THUMBNAIL_IMG_NUM;
                        _images[i] = new() {
                            Source = new BitmapImage(new(IMAGE_DIR + @"\" + bmGallery.id + @"\" + imgIdx + IMAGE_EXT)),
                            Width = THUMBNAIL_IMG_WIDTH,
                            Height = THUMBNAIL_IMG_WIDTH * bmGallery.files[i].height / bmGallery.files[i].width,
                            HorizontalAlignment = HorizontalAlignment.Center
                        };
                    Grid.SetColumn(_images[i], i);
                    imageContainer.Children.Add(_images[i]);
                }
            } else {
                _imageNotFound = true;
                _hb.IsEnabled = false;
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
            if (_imageNotFound) {
                return;
            }
            _hb.IsEnabled = enable;
        }
    }
}
