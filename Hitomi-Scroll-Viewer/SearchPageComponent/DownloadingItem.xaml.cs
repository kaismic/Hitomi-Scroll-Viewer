using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Hitomi_Scroll_Viewer.SearchPageComponent {
    public sealed partial class DownloadingItem : Grid {
        private string _statusText = "";
        public DownloadingItem(string id) {
            InitializeComponent();

            ColumnDefinitions.Add(new() { Width = new GridLength(7, GridUnitType.Star) });
            ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
            RowDefinitions.Add(new());
            RowDefinitions.Add(new());

            TextBlock desc = new() {
                Text = id
            };
            SetRow(desc, 0);
            SetColumn(desc, 0);
            Children.Add(desc);

            ProgressBar progressBar = new();
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
