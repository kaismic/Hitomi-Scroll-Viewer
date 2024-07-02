using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Hitomi_Scroll_Viewer.Resources;

namespace Hitomi_Scroll_Viewer.MainWindowComponent.SearchPageComponent.SyncManagerComponent {
    /**
     * single-select option: push and pull (upload and fetch?)
     * push:
     *      multi-select option: tag filter, bookmark - show warning: this will overwrite the currently uploaded tag filters/bookmarks
     * pull:
     *      multi-select option:
     *      tag filter:
     *          single-select option: Append to local tag filters, Overwrite local tag filters
     *              if (Append to local tag filters) is selected: 
     *                  single-select option: Replace duplicate named local tag filters with tag filters in cloud, keep duplicate named tag filters at local storage
     * 
     *      bookmark: do (CloudBookmark - LocalBookmark) enumberable and add them to bookmark
     *          single-select option: start downloading all bookmarks fetched from cloud, do not download
     */
    public sealed partial class SyncContentDialog : ContentDialog {
        public SyncContentDialog() {
            InitializeComponent();
            // this code is needed because of this bug https://github.com/microsoft/microsoft-ui-xaml/issues/424
            Resources["ContentDialogMaxWidth"] = double.MaxValue;
            PrimaryButtonText = "Sync";
            CloseButtonText = DIALOG_BUTTON_TEXT_CANCEL;
        }

        private void SyncMethodRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            RadioButtons radioButtons = sender as RadioButtons;
            if (radioButtons.SelectedIndex == -1) {
                return;
            }
            TagFilterOptionCheckBox.IsChecked = false;
            BookmarkOptionCheckBox.IsChecked = false;
            TogglePrimaryButton();
            Border0.Visibility = Visibility.Visible;
            TagFilterOptionCheckBox.Visibility = Visibility.Visible;
            BookmarkOptionCheckBox.Visibility = Visibility.Visible;
            switch (radioButtons.SelectedIndex) {
                // Upload option selected
                case 0: {
                    UploadWarningInfoBar.Visibility = Visibility.Visible;
                    FetchTagFilterOptionStackPanel.Visibility = Visibility.Collapsed;
                    FetchBookmarkOptionStackPanel.Visibility = Visibility.Collapsed;
                    break;
                }
                // Fetch option selected
                case 1: {
                    UploadWarningInfoBar.Visibility = Visibility.Collapsed;
                    FetchTagFilterOptionStackPanel.Visibility = (bool)TagFilterOptionCheckBox.IsChecked ? Visibility.Visible : Visibility.Collapsed;
                    FetchBookmarkOptionStackPanel.Visibility = (bool)BookmarkOptionCheckBox.IsChecked ? Visibility.Visible : Visibility.Collapsed;
                    break;
                }
            }
        }

        private void TagFilterOptionCheckBox_Checked(object _0, RoutedEventArgs _1) {
            TogglePrimaryButton();
            if (SyncMethodRadioButtons.SelectedIndex == 1) {
                FetchTagFilterOptionStackPanel.Visibility = Visibility.Visible;
            }
        }
        private void TagFilterOptionCheckBox_Unchecked(object _0, RoutedEventArgs _1) {
            TogglePrimaryButton();
            if (SyncMethodRadioButtons.SelectedIndex == 1) {
                FetchTagFilterOptionStackPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void BookmarkOptionCheckBox_Checked(object _0, RoutedEventArgs _1) {
            TogglePrimaryButton();
            if (SyncMethodRadioButtons.SelectedIndex == 1) {
                FetchBookmarkOptionStackPanel.Visibility = Visibility.Visible;
            }
        }
        private void BookmarkOptionCheckBox_Unchecked(object _0, RoutedEventArgs _1) {
            TogglePrimaryButton();
            if (SyncMethodRadioButtons.SelectedIndex == 1) {
                FetchBookmarkOptionStackPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void FetchTagFilterOption0_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            RadioButtons radioButtons = sender as RadioButtons;
            if (radioButtons.SelectedIndex == -1) {
                return;
            }
            TogglePrimaryButton();
            switch (radioButtons.SelectedIndex) {
                // Overwrite option selected
                case 0: {
                    (FetchTagFilterOption1.Parent as StackPanel).Visibility = Visibility.Collapsed;
                    break;
                }
                // Append option selected
                case 1: {
                    (FetchTagFilterOption1.Parent as StackPanel).Visibility = Visibility.Visible;
                    break;
                }
            }
        }

        private void FetchTagFilterOption1_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            RadioButtons radioButtons = sender as RadioButtons;
            if (radioButtons.SelectedIndex == -1) {
                return;
            }
            TogglePrimaryButton();
        }

        private void FetchBookmarkOption0_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            RadioButtons radioButtons = sender as RadioButtons;
            if (radioButtons.SelectedIndex == -1) {
                return;
            }
            TogglePrimaryButton();
        }

        private void TogglePrimaryButton() {
            // Upload option selected
            if (SyncMethodRadioButtons.SelectedIndex == 0) {
                IsPrimaryButtonEnabled = (bool)TagFilterOptionCheckBox.IsChecked || (bool)BookmarkOptionCheckBox.IsChecked;
            }
            // Fetch option selected
            else {
                if (!(bool)TagFilterOptionCheckBox.IsChecked && !(bool)BookmarkOptionCheckBox.IsChecked) {
                    IsPrimaryButtonEnabled = false;
                    return;
                }
                bool enable = true;
                if ((bool)TagFilterOptionCheckBox.IsChecked) {
                    int option0Idx = FetchTagFilterOption0.SelectedIndex;
                    int option1Idx = FetchTagFilterOption1.SelectedIndex;
                    enable &= option0Idx != -1 && (option0Idx == 0 || option1Idx != -1);
                }
                if ((bool)BookmarkOptionCheckBox.IsChecked) {
                    enable &= FetchBookmarkOption0.SelectedIndex != -1;
                }
                IsPrimaryButtonEnabled = enable;
            }
        }
    }
}
