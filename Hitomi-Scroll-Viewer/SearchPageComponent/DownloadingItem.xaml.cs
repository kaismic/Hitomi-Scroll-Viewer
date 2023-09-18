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
            Background = new SolidColorBrush(Colors.LightBlue);
            CornerRadius = new(10);
            Padding = new(10);
            ColumnDefinitions.Add(new() { Width = new GridLength(3, GridUnitType.Star) });
            ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
            RowDefinitions.Add(new());
            RowDefinitions.Add(new());
            ColumnSpacing = 10;

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

            Button cancelBtn = new() {
                Content = new TextBlock() {
                    Text = "Cancel",
                    TextWrapping = TextWrapping.Wrap
                }
            };
            SetRow(cancelBtn, 0);
            SetRowSpan(cancelBtn, 2);
            SetColumn(cancelBtn, 1);
            Children.Add(cancelBtn);

            CancellationTokenSource cts = new();
            CancellationToken ct = cts.Token;
            cancelBtn.Click += (_0, _1) => { cts.Cancel(); };

        }
    }
}
