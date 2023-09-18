using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Threading;

namespace Hitomi_Scroll_Viewer.SearchPageComponent {
    public sealed partial class DownloadingItem : Grid {
        private TextBlock _statusText;
        public DownloadingItem(string id) {
            InitializeComponent();

            BorderThickness = new(1);
            Background = new SolidColorBrush(Colors.LightBlue);
            CornerRadius = new(10);
            Padding = new(10);
            ColumnDefinitions.Add(new() { Width = new GridLength(3, GridUnitType.Star) });
            ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
            ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
            RowDefinitions.Add(new());
            RowDefinitions.Add(new());
            RowDefinitions.Add(new());
            ColumnSpacing = 8;
            RowSpacing = 4;

            TextBlock galleryIdText = new() {
                Text = id
            };
            SetRow(galleryIdText, 0);
            SetColumn(galleryIdText, 0);
            Children.Add(galleryIdText);

            ProgressBar progressBar = new() {
                Background = new SolidColorBrush(Colors.Transparent),
                Value = 100
            };
            SetRow(progressBar, 1);
            SetColumn(progressBar, 0);
            Children.Add(progressBar);

            _statusText = new() {
                Text = "Getting Gallery Info..."
            };
            SetRow(_statusText, 2);
            SetColumn(_statusText, 0);
            Children.Add(_statusText);

            Button pauseResumeBtn = new() {
                Content = new TextBlock() {
                    Text = "Pause",
                    TextWrapping = TextWrapping.Wrap
                }
            };
            SetRow(pauseResumeBtn, 0);
            SetColumn(pauseResumeBtn, 1);
            SetRowSpan(pauseResumeBtn, 3);
            Children.Add(pauseResumeBtn);
            pauseResumeBtn.Click += PauseResume;

            Button deleteBtn = new() {
                Content = new TextBlock() {
                    Text = "Delete",
                    TextWrapping = TextWrapping.Wrap
                }
            };
            SetRow(deleteBtn, 0);
            SetColumn(deleteBtn, 2);
            SetRowSpan(deleteBtn, 3);
            Children.Add(deleteBtn);
            deleteBtn.Click += Delete;


            CancellationTokenSource cts = new();
            CancellationToken ct = cts.Token;



        }

        private void PauseResume(object _0, RoutedEventArgs _1) {

        }

        private void Delete(object _0, RoutedEventArgs _1) {

        }
    }
}
