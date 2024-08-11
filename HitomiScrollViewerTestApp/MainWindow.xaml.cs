using HitomiScrollViewerLib.Controls.SearchPageComponents;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

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
                    button.VerticalAlignment = VerticalAlignment.Stretch;
                    button.HorizontalAlignment = HorizontalAlignment.Stretch;
                }
            }
            Button1.Content = "show dialog";

            MainGrid.Loaded += MainGrid_Loaded;
            RootGrid.KeyDown += RootGrid_KeyDown;
        }

        private readonly MigrationProgressReporter reporter = new();

        private void RootGrid_KeyDown(object sender, KeyRoutedEventArgs e) {
            Trace.WriteLine(e.Key + " pressed");
            switch (e.Key) {
                case Windows.System.VirtualKey.D:
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
                    reporter.ResetProgressBarValue();
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
            _ = reporter.ShowAsync(ContentDialogPlacement.InPlace);
        }

        private void Button2_Click(object sender, RoutedEventArgs e) {

        }

        private void Button3_Click(object sender, RoutedEventArgs e) {

        }
    }
}
