using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Threading;

namespace Hitomi_Scroll_Viewer.SearchPageComponent {
    public sealed partial class DownloadingItem : Grid {
        private string _statusText = "";
        public DownloadingItem(string id) {
            InitializeComponent();

            BorderThickness = new(1);
            Background = new SolidColorBrush(Colors.);
            CornerRadius = new(10);
            Padding = new(10);
            ColumnDefinitions.Add(new() { Width = new GridLength(3, GridUnitType.Star) });
            ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
            ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
            RowDefinitions.Add(new());
            RowDefinitions.Add(new());
            ColumnSpacing = 4;

            TextBlock desc = new() {
                Text = id
            };
            SetRow(desc, 0);
            SetColumn(desc, 0);
            Children.Add(desc);

            ProgressBar progressBar = new() {
                Background = new SolidColorBrush(Colors.Transparent)
            };
            SetRow(progressBar, 1);
            SetColumn(progressBar, 0);
            Children.Add(progressBar);

            Button pauseResumeBtn = new() {
                Content = new TextBlock() {
                    Text = "Pause",
                    TextWrapping = TextWrapping.Wrap
                }
            };
            SetRow(pauseResumeBtn, 0);
            SetColumn(pauseResumeBtn, 1);
            SetRowSpan(pauseResumeBtn, 2);
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
            SetRowSpan(deleteBtn, 2);
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
