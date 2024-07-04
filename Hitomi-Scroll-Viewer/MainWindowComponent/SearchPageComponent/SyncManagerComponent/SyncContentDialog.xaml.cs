using Google;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Upload;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;
using static Hitomi_Scroll_Viewer.MainWindowComponent.SearchPageComponent.SyncManagerComponent.SyncContentDialog.SyncOptions;
using static Hitomi_Scroll_Viewer.Resources;
using static Hitomi_Scroll_Viewer.Utils;

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
        internal struct SyncOptions {
            public SyncOptions() {}
            public bool IsUpload { get; internal set; }
            public enum SyncDataType {
                TagFilter, Bookmark
            }
            public HashSet<SyncDataType> SyncDataTypes { get; internal set; } = [];
            public bool FetchTagFilterOverwrite { get; internal set; }
            public bool FetchTagFilterReplaceDups { get; internal set; }
            public bool FetchBookmarkStartDownload { get; internal set; }
        }

        private bool _closeDialog = true;

        private readonly DriveService _driveService;

        public SyncContentDialog(DriveService driveService) {
            _driveService = driveService;
            InitializeComponent();
            // this code is needed because of this bug https://github.com/microsoft/microsoft-ui-xaml/issues/424
            Resources["ContentDialogMaxWidth"] = double.MaxValue;
            PrimaryButtonText = "Sync";
            CloseButtonText = DIALOG_BUTTON_TEXT_CANCEL;
        }

        private void ContentDialog_CloseButtonClick(ContentDialog _0, ContentDialogButtonClickEventArgs _1) {
            _closeDialog = true;
        }

        private static void DisableControlsRecursive(StackPanel parentStackPanel) {
            foreach (var child in parentStackPanel.Children) {
                if (child is Control control && control is not ProgressBar) {
                    control.IsEnabled = false;
                } else if (child is StackPanel childStackPanel) {
                    DisableControlsRecursive(childStackPanel);
                }
            }
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog _0, ContentDialogButtonClickEventArgs _1) {
            _closeDialog = false;
            IsEnabled = false;
            IsPrimaryButtonEnabled = false;
            DisableControlsRecursive(Content as StackPanel);
            SyncProgressBar.Visibility = Visibility.Visible;

            SyncOptions options = new() {
                IsUpload = SyncMethodRadioButtons.SelectedIndex == 0,
                FetchTagFilterOverwrite = FetchTagFilterOption0.SelectedIndex == 0,
                FetchTagFilterReplaceDups = FetchTagFilterOption1.SelectedIndex == 0,
                FetchBookmarkStartDownload = FetchBookmarkOption0.SelectedIndex == 0
            };
            if (TagFilterOptionCheckBox.IsChecked == true) options.SyncDataTypes.Add(SyncDataType.TagFilter);
            if (BookmarkOptionCheckBox.IsChecked == true) options.SyncDataTypes.Add(SyncDataType.Bookmark);
            
            if (SyncMethodRadioButtons.SelectedIndex == 0) {
                // Upload
                bool success = true;
                if (TagFilterOptionCheckBox.IsChecked == true) {
                    // Upload tag filters
                    success = await UploadFileAsync(TAG_FILTERS_FILE_NAME, TAG_FILTERS_FILE_PATH, MediaTypeNames.Application.Json);
                }
                if (success && BookmarkOptionCheckBox.IsChecked == true) {
                    // Upload bookmarks
                    success = await UploadFileAsync(BOOKMARKS_FILE_NAME, BOOKMARKS_FILE_PATH, MediaTypeNames.Application.Json);
                }
                if (success) {
                    SyncProgressBar.Visibility = Visibility.Collapsed;
                    SyncErrorInfoBar.Severity = InfoBarSeverity.Success;
                    SyncErrorInfoBar.Title = "Upload completed";
                    SyncErrorInfoBar.Message = "Uploading has completed successfully.";
                    CloseButtonText = DIALOG_BUTTON_TEXT_CLOSE;
                    SyncErrorInfoBar.IsOpen = true;
                }
            } else {
                // Fetch
                // TODO

            }
        }

        private async Task<bool> UploadFileAsync(string fileName, string filePath, string contentType) {
            FileList fileList = await GetFileListAsync();
            string fileId = null;
            foreach (var file in fileList.Files) {
                if (file.Name == fileName) {
                    fileId = file.Id;
                }
            }
            using FileStream stream = new(filePath, FileMode.Open);
            Google.Apis.Drive.v3.Data.File fileMetaData = new();
            ResumableUpload request;
            if (fileId == null) {
                fileMetaData.Name = fileName;
                fileMetaData.Parents = ["appDataFolder"];
                request = _driveService.Files.Create(
                    fileMetaData,
                    stream,
                    contentType
                );
            } else {
                request = _driveService.Files.Update(
                    fileMetaData,
                    fileId,
                    stream,
                    contentType
                );
            }
            try {
                IUploadProgress result = await request.UploadAsync();
                if (result.Exception != null) {
                    throw result.Exception;
                }
            } catch (Exception e) {
                SyncProgressBar.Visibility = Visibility.Collapsed;
                SyncErrorInfoBar.Severity = InfoBarSeverity.Error;
                SyncErrorInfoBar.Title = "Error";
                if (e is GoogleApiException googleApiException && googleApiException.HttpStatusCode == System.Net.HttpStatusCode.Forbidden) {
                    SyncErrorInfoBar.Message = "This app is not authorized to access Google Drive."
                        + "\nPlease sign out, sign in again and allow access to Google Drive";
                } else {
                    SyncErrorInfoBar.Message = "An unknown error has occurred. Please try again after a few seconds.";
                }
                SyncErrorInfoBar.IsOpen = true;
                return false;
            } finally {
                IsEnabled = true;
            }
            return true;
        }

        private async Task<FileList> GetFileListAsync() {
            FilesResource.ListRequest listRequest = _driveService.Files.List();
            listRequest.Spaces = "appDataFolder";
            listRequest.Fields = "nextPageToken, files(id, name)";
            listRequest.PageSize = 4;
            return await listRequest.ExecuteAsync();
        }


        /**
         * <returns>The file <see cref="MemoryStream"/> if the file is found successfully, otherwise, <c>null</c>.</returns>
         */
        private async Task<MemoryStream> FetchFile(string fileName) {
            Trace.WriteLine($"Searching for file {fileName}...");
            FileList filstList = await GetFileListAsync();
            foreach (Google.Apis.Drive.v3.Data.File file in filstList.Files) {
                // Prints the list of 10 file names.
                if (file.Name == fileName) {
                    Trace.WriteLine($"Found file {fileName}: id = {file.Id}");
                    FilesResource.GetRequest getRequest = _driveService.Files.Get(file.Id);
                    MemoryStream stream = new();
                    getRequest.Download(stream);
                    return stream;
                }
            }
            Trace.WriteLine($"File not found: {fileName}");
            return null;
        }

        private void ContentDialog_Closing(ContentDialog _0, ContentDialogClosingEventArgs args) {
            args.Cancel = !_closeDialog;
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
                    UploadWarningInfoBar.IsOpen = true;
                    FetchTagFilterOptionStackPanel.Visibility = Visibility.Collapsed;
                    FetchBookmarkOptionStackPanel.Visibility = Visibility.Collapsed;
                    break;
                }
                // Fetch option selected
                case 1: {
                    UploadWarningInfoBar.IsOpen = false;
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
