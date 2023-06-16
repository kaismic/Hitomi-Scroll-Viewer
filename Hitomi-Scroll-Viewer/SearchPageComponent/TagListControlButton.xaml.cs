using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.Foundation;
using Windows.UI;

namespace Hitomi_Scroll_Viewer.SearchPageComponent {
    public sealed partial class TagListControlButton : Button {
        private readonly ContentDialog _confirmDialog;
        public TagListControlButton(string text, Color borderColor, bool confirmAction) {
            InitializeComponent();

            Content = new TextBlock() {
                Text = text,
                TextWrapping = TextWrapping.WrapWholeWords
            };

            BorderBrush = new SolidColorBrush(borderColor);

            if (confirmAction) {
                _confirmDialog = new() {
                    IsPrimaryButtonEnabled = true,
                    PrimaryButtonText = "Yes",
                    CloseButtonText = "Cancel"
                };
                Click += ConfirmAction;
                Loaded += (object sender, RoutedEventArgs e) => {
                    _confirmDialog.XamlRoot = XamlRoot;
                };
            }
        }

        public Func<bool> buttonClickFunc;

        public void SetDialog(string title, string text) {
            _confirmDialog.Title = title;
            _confirmDialog.Content = text;
        }

        public void SetAction(TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> action) {
            _confirmDialog.PrimaryButtonClick += action;
        }

        public async void ConfirmAction(object sender, RoutedEventArgs e) {
            if (buttonClickFunc.Invoke()) {
                await _confirmDialog.ShowAsync();
            }
        }
    }
}
