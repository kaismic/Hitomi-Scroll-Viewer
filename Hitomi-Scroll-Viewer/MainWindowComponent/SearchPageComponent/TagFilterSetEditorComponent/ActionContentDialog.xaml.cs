using Hitomi_Scroll_Viewer.DbContexts;
using Hitomi_Scroll_Viewer.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using static Hitomi_Scroll_Viewer.Resources;

namespace Hitomi_Scroll_Viewer.MainWindowComponent.SearchPageComponent.TagFilterSetEditorComponent {
    internal sealed partial class ActionContentDialog : ContentDialog {
        internal enum Action {
            Create, Rename, Delete
        }
        private string _oldName;

        public ActionContentDialog(TagFilterSetContext tagFilterSetContext) {
            InitializeComponent();
            DefaultButton = ContentDialogButton.Primary;
            CloseButtonText = TEXT_CANCEL;
            LengthDisplayTextBlock.Text = $"0/{TagFilterSet.TAG_FILTER_SET_NAME_MAX_LEN}";
            InputTextBox.MaxLength = TagFilterSet.TAG_FILTER_SET_NAME_MAX_LEN;

            InputTextBox.TextChanged += (_, _) => {
                LengthDisplayTextBlock.Text = $"{InputTextBox.Text.Length}/{TagFilterSet.TAG_FILTER_SET_NAME_MAX_LEN}";
                IsPrimaryButtonEnabled = InputTextBox.Text.Length != 0;
                ErrorMsgTextBlock.Text = "";
            };
            PrimaryButtonClick += (ContentDialog _, ContentDialogButtonClickEventArgs args) => {
                // Delete
                if (ContentGrid.Visibility == Visibility.Collapsed) {
                    return;
                }
                string newName = InputTextBox.Text;
                // Rename or Create
                if (_oldName == newName) {
                    ErrorMsgTextBlock.Text = "Cannot rename to the same name."; // TODO
                    args.Cancel = true;
                    return;
                }
                // Create
                if (tagFilterSetContext.TagFilterSets.Any(tagFilterSet => tagFilterSet.Name == newName)) {
                    ErrorMsgTextBlock.Text = $"\"{newName}\" already exists."; // TODO
                    args.Cancel = true;
                    return;
                }
            };
        }

        internal void SetDialogType(Action action, string title, string primaryButtonText, string oldName = null) {
            ErrorMsgTextBlock.Text = "";
            TitleTextBlock.Text = title;
            PrimaryButtonText = primaryButtonText;
            _oldName = oldName;
            switch (action) {
                case Action.Create:
                    IsPrimaryButtonEnabled = false;
                    ContentGrid.Visibility = Visibility.Visible;
                    InputTextBox.Text = "";
                    break;
                case Action.Rename:
                    IsPrimaryButtonEnabled = true;
                    ContentGrid.Visibility = Visibility.Visible;
                    ArgumentNullException.ThrowIfNull(oldName);
                    InputTextBox.Text = oldName;
                    InputTextBox.SelectAll();
                    break;
                case Action.Delete:
                    IsPrimaryButtonEnabled = true;
                    ContentGrid.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        internal string GetText() {
            return InputTextBox.Text;
        }
    }
}
