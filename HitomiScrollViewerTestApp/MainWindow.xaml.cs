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
using Windows.Foundation;
using Windows.Foundation.Collections;
using HitomiScrollViewerLib.Controls;
using HitomiScrollViewerLib.Controls.SearchPageComponents;
using System.Threading.Tasks;
using System.Diagnostics;

namespace HitomiScrollViewerTestApp {
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
            for (int i = 0; i < ButtonGrid.Children.Count; i++) {
                if (ButtonGrid.Children[i] is Button button) {
                    Grid.SetRow(button, i);
                }
            }
            Button1.Content = "show dialog";
            Button2.Content = "hide dialog";

            MainGrid.Loaded += MainGrid_Loaded;
            RootGrid.KeyDown += RootGrid_KeyDown;
        }

        private readonly MigrationProgressReporter reporter = new();

        private void RootGrid_KeyDown(object sender, KeyRoutedEventArgs e) {
            switch (e.Key) {
                case Windows.System.VirtualKey.D:
                    Trace.WriteLine(e.Key + " pressed");
                    reporter.Hide();
                    break;
            }
        }

        private void MainGrid_Loaded(object sender, RoutedEventArgs e) {
            MainGrid.Loaded -= MainGrid_Loaded;
            reporter.XamlRoot = MainGrid.XamlRoot;
        }


        private void Button1_Click(object sender, RoutedEventArgs e) {
            _ = Task.Run(async () => {
                int totalTime = 10;
                int delay = 1000;
                DispatcherQueue.TryEnqueue(() => {
                    reporter.SetProgressBarMaximum(totalTime);
                    reporter.SetStatusMessage("Counting from 1 to 10...");
                });
                for (int i = 0; i < totalTime; i++) {
                    await Task.Delay(delay);
                    DispatcherQueue.TryEnqueue(() => {
                        reporter.IncrementProgressBar();
                    });
                }
            });
            _ = reporter.ShowAsync();
        }

        private void Button2_Click(object sender, RoutedEventArgs e) {
            //reporter.Hide();
        }

        private void Button3_Click(object sender, RoutedEventArgs e) {

        }
    }
}
