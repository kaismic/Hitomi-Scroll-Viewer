using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Diagnostics;
using System.IO;
using static Hitomi_Scroll_Viewer.MainWindow;
using static Hitomi_Scroll_Viewer.SearchPage;

namespace Hitomi_Scroll_Viewer.SearchPageComponent {
    public sealed partial class BookmarkItem : StackPanel {
        public readonly Gallery bmGallery;
        private readonly Image[] _images;
        private readonly HyperlinkButton _hb;
        private bool _imageNotFound = false;
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

            _images = new Image[THUMBNAIL_IMG_NUM];
            for (int i = 0; i < THUMBNAIL_IMG_NUM; i++) {
                imageContainer.ColumnDefinitions.Add(
                    new ColumnDefinition() {
                        Width = new GridLength(2, GridUnitType.Star)
                    }
                );
                _images[i] = new() {
                    Source = null,
                    Width = THUMBNAIL_IMG_WIDTH,
                    Height = THUMBNAIL_IMG_WIDTH * bmGallery.files[i].height / bmGallery.files[i].width,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetColumn(_images[i], i);
                imageContainer.Children.Add(_images[i]);
            }
            SetImages();

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

        private async void SetImages() {
            string path = IMAGE_DIR + @"\" + bmGallery.id;
            try {
                for (int i = 0; i < THUMBNAIL_IMG_NUM; i++) {
                    int imgIdx = i * bmGallery.files.Count / THUMBNAIL_IMG_NUM;
                    _images[i].Source = await GetBitmapImage(await File.ReadAllBytesAsync(path + @"\" + imgIdx.ToString()));
                }
            }
            catch (DirectoryNotFoundException) {
                _imageNotFound = true;
                _hb.IsEnabled = false;
                Debug.WriteLine("Directory for " + bmGallery.id + " not found");
            }
        }

        public void EnableButton(bool enable) {
            if (_imageNotFound) {
                return;
            }
            _hb.IsEnabled = enable;
        }
    }
}
