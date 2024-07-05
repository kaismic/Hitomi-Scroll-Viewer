using Google;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Upload;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;
using System.Threading.Tasks;
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
        private bool _closeDialog = true;
        private readonly DriveService _driveService;

        public SyncContentDialog(DriveService driveService) {
            _driveService = driveService;
            InitializeComponent();
            // this code is needed because of this bug https://github.com/microsoft/microsoft-ui-xaml/issues/424
            Resources["ContentDialogMaxWidth"] = double.MaxValue;
            PrimaryButtonText = "Sync";
            CloseButtonText = DIALOG_BUTTON_TEXT_CLOSE;
        }

        private void ContentDialog_CloseButtonClick(ContentDialog _0, ContentDialogButtonClickEventArgs _1) {
            _closeDialog = true;
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog _0, ContentDialogButtonClickEventArgs _1) {
            _closeDialog = false;
            IsEnabled = false;
            SyncProgressBar.Visibility = Visibility.Visible;

            FilesResource.ListRequest listRequest = _driveService.Files.List();
            listRequest.Spaces = "appDataFolder";
            listRequest.Fields = "nextPageToken, files(id, name)";
            listRequest.PageSize = 2;
            FileList fileList = await listRequest.ExecuteAsync();

            // TODO
            // handle situation correctly
            // Upload: 1. upload success, 2. exception
            // Fetch: 1. fetch success, 2. file(s) not yet uploaded, 3. exception

            // Upload
            if (SyncMethodRadioButtons.SelectedIndex == 0) {
                bool success = true;
                // Upload tag filters
                if (TagFilterOptionCheckBox.IsChecked == true) {
                    success = await UploadFileAsync(fileList, TAG_FILTERS_FILE_NAME, TAG_FILTERS_FILE_PATH, MediaTypeNames.Application.Json);
                }
                // Upload bookmarks
                if (success && BookmarkOptionCheckBox.IsChecked == true) {
                    success = await UploadFileAsync(fileList, BOOKMARKS_FILE_NAME, BOOKMARKS_FILE_PATH, MediaTypeNames.Application.Json);
                }
                if (success) {
                    SyncProgressBar.Visibility = Visibility.Collapsed;
                    SyncErrorInfoBar.Severity = InfoBarSeverity.Success;
                    SyncErrorInfoBar.Title = "Upload completed";
                    SyncErrorInfoBar.Message = "Uploading has completed successfully.";
                    SyncErrorInfoBar.IsOpen = true;
                }
            } else {
                // Fetch
                bool success = true;
                // Fetch tag filters
                if (TagFilterOptionCheckBox.IsChecked == true) {
                    string fetchedFileData = await FetchFile(fileList, TAG_FILTERS_FILE_NAME);
                    if (fetchedFileData != null) {
                        Dictionary<string, TagFilter> fetchedTagFilterDict = (Dictionary<string, TagFilter>)JsonSerializer.Deserialize(
                            fetchedFileData,
                            typeof(Dictionary<string, TagFilter>),
                            DEFAULT_SERIALIZER_OPTIONS
                        );
                        // Overwrite tag filters
                        if (FetchTagFilterOption0.SelectedIndex == 0) {
                            MainWindow.SearchPage.TagFilterDict = fetchedTagFilterDict;
                        }
                        // Append tag filters
                        else {
                            // Replace locally stored tag filters with duplicate names from the cloud
                            if (FetchTagFilterOption1.SelectedIndex == 0) {
                                MainWindow.SearchPage.TagFilterDict =
                                    (Dictionary<string, TagFilter>)fetchedTagFilterDict
                                    .Concat(
                                        MainWindow.SearchPage.TagFilterDict.Where(
                                            pair => !fetchedTagFilterDict.ContainsKey(pair.Key)
                                        )
                                    );
                            }
                            // Keep locally stored tag filters with duplicate names
                            else {
                                MainWindow.SearchPage.TagFilterDict =
                                    (Dictionary<string, TagFilter>)MainWindow.SearchPage.TagFilterDict
                                    .Concat(
                                        fetchedTagFilterDict.Where(
                                            pair => !MainWindow.SearchPage.TagFilterDict.ContainsKey(pair.Key)
                                        )
                                    );
                            }
                        }
                        MainWindow.SearchPage.WriteTagFilterDict();
                    } else {
                        success = false;
                    }
                }
                // Fetch bookmarks
                if (success && BookmarkOptionCheckBox.IsChecked == true) {
                    string fetchedFileData = await FetchFile(fileList, BOOKMARKS_FILE_NAME);
                    if (fetchedFileData != null) {
                        IEnumerable<Gallery> fetchedBookmarkGalleries = (IEnumerable<Gallery>)JsonSerializer.Deserialize(
                            fetchedFileData,
                            typeof(IEnumerable<Gallery>),
                            DEFAULT_SERIALIZER_OPTIONS
                        );
                        // append fetched galleries to existing bookmark if they are not already in the bookmark
                        IEnumerable<Gallery> localBookmarkGalleries = SearchPage.BookmarkItems.Select(item => item.gallery);
                        IEnumerable<Gallery> appendingGalleries = fetchedBookmarkGalleries.ExceptBy(
                            localBookmarkGalleries.Select(gallery => gallery.id),
                            gallery => gallery.id
                        );
                        List<BookmarkItem> appendedBookmarkItems = [];
                        foreach (var gallery in appendingGalleries) {
                            appendedBookmarkItems.Add(MainWindow.SearchPage.AddBookmark(gallery));
                        }
                        // start downloading all appended galleries if the corresponding option is checked
                        if (FetchBookmarkOption0.SelectedIndex == 0) {
                            foreach (var gallery in appendingGalleries) {
                                MainWindow.SearchPage.TryDownload(gallery.id);
                            }  
                        }
                    } else {
                        success = false;
                    }
                }
                if (success) {
                    SyncProgressBar.Visibility = Visibility.Collapsed;
                    SyncErrorInfoBar.Severity = InfoBarSeverity.Success;
                    SyncErrorInfoBar.Title = "Fetch completed";
                    SyncErrorInfoBar.Message = "Fetching has completed successfully.";
                    SyncErrorInfoBar.IsOpen = true;
                }
            }
            IsEnabled = true;
        }

        private async Task<bool> UploadFileAsync(FileList fileList, string fileName, string filePath, string contentType) {
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
            }
            return true;
        }

        /**
         * <returns>The file content <c>string</c> if the file is fetched and read successfully, otherwise, <c>null</c>.</returns>
         */
        private async Task<string> FetchFile(FileList fileList, string fileName) {
            Trace.WriteLine($"Searching for file {fileName}...");
            foreach (var file in fileList.Files) {
                if (file.Name == fileName) {
                    Trace.WriteLine($"Found file {fileName}, id = {file.Id}");
                    MemoryStream stream = new();
                    FilesResource.GetRequest getRequest = _driveService.Files.Get(file.Id);
                    try {
                        IDownloadProgress result = await getRequest.DownloadAsync(stream);
                        if (result.Exception != null) {
                            throw result.Exception;
                        }
                        stream.Position = 0;
                        using StreamReader reader = new(stream);
                        return await reader.ReadToEndAsync();
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
                        return null;
                    }
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
