using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.Foundation;
using Windows.UI;

namespace Hitomi_Scroll_Viewer.SearchPageComponent {
    public sealed partial class TagListControlButton : Button {
        public readonly ContentDialog confirmDialog;
        public TagListControlButton(string text, Color borderColor, bool confirmAction) {
            InitializeComponent();

            Content = new TextBlock() {
                Text = text,
                TextWrapping = TextWrapping.WrapWholeWords
            };

            BorderBrush = new SolidColorBrush(borderColor);

            if (confirmAction) {
                confirmDialog = new() {
                    IsPrimaryButtonEnabled = true,
                    PrimaryButtonText = "Yes",
                    CloseButtonText = "Cancel"
                };
                Click += ConfirmAction;
                Loaded += (object sender, RoutedEventArgs e) => {
                    confirmDialog.XamlRoot = XamlRoot;
                };
            }
        }

        public Func<bool> buttonClickFunc;

        public void SetDialog(string title, string text) {
            confirmDialog.Title = title;
            confirmDialog.Content = text;
        }

        public void SetAction(TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> action) {
            confirmDialog.PrimaryButtonClick += action;
        }

        public async void ConfirmAction(object sender, RoutedEventArgs e) {
            if (buttonClickFunc.Invoke()) {
                await confirmDialog.ShowAsync();
            }
        }
    }
}
