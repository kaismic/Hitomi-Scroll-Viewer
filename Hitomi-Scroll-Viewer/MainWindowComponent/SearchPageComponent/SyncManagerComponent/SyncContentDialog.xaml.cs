using Google;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Upload;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static Hitomi_Scroll_Viewer.Resources;
using static Hitomi_Scroll_Viewer.Utils;

namespace Hitomi_Scroll_Viewer.MainWindowComponent.SearchPageComponent.SyncManagerComponent {
    public sealed partial class SyncContentDialog : ContentDialog {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree("SyncContentDialog");

        private bool _closeDialog = true;
        private readonly DriveService _driveService;
        private bool _isSyncing = false;
        private CancellationTokenSource _cts;

        public SyncContentDialog(DriveService driveService) {
            _driveService = driveService;
            InitializeComponent();
            // this code is needed because of this bug https://github.com/microsoft/microsoft-ui-xaml/issues/424
            Resources["ContentDialogMaxWidth"] = double.MaxValue;
            PrimaryButtonText = "Sync";
            CloseButtonText = TEXT_CLOSE;

            TagFilterOptionCheckBox.Content = TEXT_TAG_FILTERS;
            BookmarkOptionCheckBox.Content = TEXT_BOOKMARKS;

            FetchBookmarkOption1.Items.Add(new RadioButton() {
                Content = TEXT_YES
            });
            FetchBookmarkOption1.Items.Add(new RadioButton() {
                Content = TEXT_NO
            });
        }

        private void ContentDialog_CloseButtonClick(ContentDialog _0, ContentDialogButtonClickEventArgs _1) {
            if (_isSyncing) {
                _cts.Cancel();
            } else {
                _closeDialog = true;
            }
        }

        private static void EnableContentControls(StackPanel parentStackPanel, bool enable) {
            foreach (var child in parentStackPanel.Children) {
                if (child is Control control) {
                    control.IsEnabled = enable;
                } else if (child is StackPanel childStackPanel) {
                    EnableContentControls(childStackPanel, enable);
                }
            }
        }
        
        private void StartStopSync(bool start) {
            _isSyncing = start;
            IsPrimaryButtonEnabled = !start;
            EnableContentControls(Content as StackPanel, !start);
            if (start) {
                SyncProgressBar.Visibility = Visibility.Visible;
                CloseButtonText = TEXT_CANCEL;
                TagFilterSyncResultInfoBar.IsOpen = false;
                BookmarkSyncResultInfoBar.IsOpen = false;
            } else {
                SyncProgressBar.Visibility = Visibility.Collapsed;
                CloseButtonText = TEXT_CLOSE;
            }
        }

        private static readonly string INFOBAR_UPLOAD_SUCCESS_MESSAGE = _resourceMap.GetValue("InfoBar_Upload_Success_Message").ValueAsString;
        private static readonly string INFOBAR_UPLOAD_CANCELED_MESSAGE = _resourceMap.GetValue("InfoBar_Upload_Canceled_Message").ValueAsString;

        private static readonly string INFOBAR_FETCH_SUCCESS_MESSAGE = _resourceMap.GetValue("InfoBar_Fetch_Success_Message").ValueAsString;
        private static readonly string INFOBAR_FETCH_CANCELED_MESSAGE = _resourceMap.GetValue("InfoBar_Fetch_Canceled_Message").ValueAsString;

        private static readonly string INFOBAR_ERROR_UNAUTHORIZED_MESSAGE = _resourceMap.GetValue("InfoBar_Error_Unauthorized_Message").ValueAsString;
        private static readonly string INFOBAR_ERROR_UNKNOWN_MESSAGE = _resourceMap.GetValue("InfoBar_Unknown_Error_Message").ValueAsString;
        private static readonly string INFOBAR_ERROR_FILENOTFOUND_MESSAGE = _resourceMap.GetValue("InfoBar_Error_FileNotFound_Message").ValueAsString;

        private async void ContentDialog_PrimaryButtonClick(ContentDialog _0, ContentDialogButtonClickEventArgs _1) {
            _cts = new();
            _closeDialog = false;
            StartStopSync(true);

            FilesResource.ListRequest listRequest = _driveService.Files.List();
            listRequest.Spaces = "appDataFolder";
            listRequest.Fields = "nextPageToken, files(id, name)";
            listRequest.PageSize = 2;
            string tagFiltersFileId = null;
            string bookmarksFileId = null;
            try {
                FileList fileList = await listRequest.ExecuteAsync(_cts.Token);
                if (fileList != null) {
                    foreach (var file in fileList.Files) {
                        if (file.Name == TAG_FILTERS_FILE_NAME) {
                            tagFiltersFileId = file.Id;
                        } else if (file.Name == BOOKMARKS_FILE_NAME) {
                            bookmarksFileId = file.Id;
                        }
                    }
                }
            } catch (Exception) {}

            // Upload
            if (SyncDirectionRadioButtons.SelectedIndex == 0) {
                // Upload tag filters
                if (TagFilterOptionCheckBox.IsChecked == true) {
                    try {
                        if (tagFiltersFileId == null) {
                            await CreateFileAsync(TAG_FILTERS_FILE_NAME, TAG_FILTERS_FILE_PATH, MediaTypeNames.Application.Json, _cts.Token);
                        } else {
                            await UpdateFileAsync(tagFiltersFileId, TAG_FILTERS_FILE_PATH, MediaTypeNames.Application.Json, _cts.Token);
                        }
                        TagFilterSyncResultInfoBar.Severity = InfoBarSeverity.Success;
                        TagFilterSyncResultInfoBar.Title = TEXT_TAG_FILTERS;
                        TagFilterSyncResultInfoBar.Message = INFOBAR_UPLOAD_SUCCESS_MESSAGE;
                    } catch (TaskCanceledException) {
                        TagFilterSyncResultInfoBar.Severity = InfoBarSeverity.Informational;
                        TagFilterSyncResultInfoBar.Title = TEXT_TAG_FILTERS;
                        TagFilterSyncResultInfoBar.Message = INFOBAR_UPLOAD_CANCELED_MESSAGE;
                    } catch (Exception e) {
                        TagFilterSyncResultInfoBar.Severity = InfoBarSeverity.Error;
                        TagFilterSyncResultInfoBar.Title = TEXT_ERROR;
                        if (e is GoogleApiException googleApiException && googleApiException.HttpStatusCode == System.Net.HttpStatusCode.Forbidden) {
                            TagFilterSyncResultInfoBar.Message = INFOBAR_ERROR_UNAUTHORIZED_MESSAGE;
                        } else {
                            TagFilterSyncResultInfoBar.Message = INFOBAR_ERROR_UNKNOWN_MESSAGE;
                        }
                    } finally {
                        TagFilterSyncResultInfoBar.IsOpen = true;
                    }
                }
                // Upload bookmarks
                if (BookmarkOptionCheckBox.IsChecked == true) {
                    try {
                        if (bookmarksFileId == null) {
                            await CreateFileAsync(BOOKMARKS_FILE_NAME, BOOKMARKS_FILE_PATH, MediaTypeNames.Application.Json, _cts.Token);
                        } else {
                            await UpdateFileAsync(bookmarksFileId, BOOKMARKS_FILE_PATH, MediaTypeNames.Application.Json, _cts.Token);
                        }
                        BookmarkSyncResultInfoBar.Severity = InfoBarSeverity.Success;
                        BookmarkSyncResultInfoBar.Title = TEXT_BOOKMARKS;
                        BookmarkSyncResultInfoBar.Message = INFOBAR_UPLOAD_SUCCESS_MESSAGE;
                    } catch (TaskCanceledException) {
                        BookmarkSyncResultInfoBar.Severity = InfoBarSeverity.Informational;
                        BookmarkSyncResultInfoBar.Title = TEXT_BOOKMARKS;
                        BookmarkSyncResultInfoBar.Message = INFOBAR_UPLOAD_CANCELED_MESSAGE;
                    } catch (Exception e) {
                        BookmarkSyncResultInfoBar.Severity = InfoBarSeverity.Error;
                        BookmarkSyncResultInfoBar.Title = TEXT_ERROR;
                        if (e is GoogleApiException googleApiException && googleApiException.HttpStatusCode == System.Net.HttpStatusCode.Forbidden) {
                            BookmarkSyncResultInfoBar.Message = INFOBAR_ERROR_UNAUTHORIZED_MESSAGE;
                        } else {
                            BookmarkSyncResultInfoBar.Message = INFOBAR_ERROR_UNKNOWN_MESSAGE;
                        }
                    } finally {
                        BookmarkSyncResultInfoBar.IsOpen = true;
                    }
                }
            }
            // Fetch
            else {
                // Fetch tag filters
                if (TagFilterOptionCheckBox.IsChecked == true) {
                    // file is not uploaded yet
                    if (tagFiltersFileId == null) {
                        TagFilterSyncResultInfoBar.Severity = InfoBarSeverity.Error;
                        TagFilterSyncResultInfoBar.Title = TEXT_TAG_FILTERS;
                        TagFilterSyncResultInfoBar.Message = INFOBAR_ERROR_FILENOTFOUND_MESSAGE;
                    }
                    // file exists
                    else {
                        try {
                            string fetchedTagFiltersData = await GetFile(tagFiltersFileId, _cts.Token);
                            Dictionary<string, TagFilter> fetchedTagFilterDict = (Dictionary<string, TagFilter>)JsonSerializer.Deserialize(
                                fetchedTagFiltersData,
                                typeof(Dictionary<string, TagFilter>),
                                DEFAULT_SERIALIZER_OPTIONS
                            );
                            // Overwrite tag filters
                            if (FetchTagFilterOption1.SelectedIndex == 0) {
                                MainWindow.SearchPage.TagFilterDict = fetchedTagFilterDict;
                            }
                            // Append tag filters
                            else {
                                // Replace locally stored tag filters with duplicate names from the cloud
                                if (FetchTagFilterOption2.SelectedIndex == 0) {
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

                            TagFilterSyncResultInfoBar.Severity = InfoBarSeverity.Success;
                            TagFilterSyncResultInfoBar.Title = TEXT_TAG_FILTERS;
                            TagFilterSyncResultInfoBar.Message = INFOBAR_FETCH_SUCCESS_MESSAGE;
                        } catch (TaskCanceledException) {
                            TagFilterSyncResultInfoBar.Severity = InfoBarSeverity.Informational;
                            TagFilterSyncResultInfoBar.Title = TEXT_TAG_FILTERS;
                            TagFilterSyncResultInfoBar.Message = INFOBAR_FETCH_CANCELED_MESSAGE;
                        } catch (Exception e) {
                            TagFilterSyncResultInfoBar.Severity = InfoBarSeverity.Error;
                            TagFilterSyncResultInfoBar.Title = TEXT_ERROR;
                            if (e is GoogleApiException googleApiException && googleApiException.HttpStatusCode == System.Net.HttpStatusCode.Forbidden) {
                                TagFilterSyncResultInfoBar.Message = INFOBAR_ERROR_UNAUTHORIZED_MESSAGE;
                            } else {
                                TagFilterSyncResultInfoBar.Message = INFOBAR_ERROR_UNKNOWN_MESSAGE;
                            }
                        }
                    }
                    TagFilterSyncResultInfoBar.IsOpen = true;
                }
                // Fetch bookmarks
                if (BookmarkOptionCheckBox.IsChecked == true) {
                    // file is not uploaded yet
                    if (tagFiltersFileId == null) {
                        BookmarkSyncResultInfoBar.Severity = InfoBarSeverity.Error;
                        BookmarkSyncResultInfoBar.Title = TEXT_BOOKMARKS;
                        BookmarkSyncResultInfoBar.Message = INFOBAR_ERROR_FILENOTFOUND_MESSAGE;
                    }
                    // file exists
                    else {
                        try {
                            string fetchedBookmarksData = await GetFile(bookmarksFileId, _cts.Token);
                            IEnumerable<Gallery> fetchedBookmarkGalleries = (IEnumerable<Gallery>)JsonSerializer.Deserialize(
                                fetchedBookmarksData,
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
                            if (FetchBookmarkOption1.SelectedIndex == 0) {
                                foreach (var gallery in appendingGalleries) {
                                    MainWindow.SearchPage.TryDownload(gallery.id);
                                }
                            }

                            BookmarkSyncResultInfoBar.Severity = InfoBarSeverity.Success;
                            BookmarkSyncResultInfoBar.Title = TEXT_BOOKMARKS;
                            BookmarkSyncResultInfoBar.Message = INFOBAR_FETCH_SUCCESS_MESSAGE;
                        } catch (TaskCanceledException) {
                            TagFilterSyncResultInfoBar.Severity = InfoBarSeverity.Informational;
                            TagFilterSyncResultInfoBar.Title = TEXT_BOOKMARKS;
                            TagFilterSyncResultInfoBar.Message = INFOBAR_FETCH_CANCELED_MESSAGE;
                        } catch (Exception e) {
                            BookmarkSyncResultInfoBar.Severity = InfoBarSeverity.Error;
                            BookmarkSyncResultInfoBar.Title = TEXT_ERROR;
                            if (e is GoogleApiException googleApiException && googleApiException.HttpStatusCode == System.Net.HttpStatusCode.Forbidden) {
                                BookmarkSyncResultInfoBar.Message = INFOBAR_ERROR_UNAUTHORIZED_MESSAGE;
                            } else {
                                BookmarkSyncResultInfoBar.Message = INFOBAR_ERROR_UNKNOWN_MESSAGE;
                            }
                        }
                    }
                    BookmarkSyncResultInfoBar.IsOpen = true;
                }
            }
            StartStopSync(false);
        }

        /**
         * <exception cref="Exception"/>
         * <exception cref="TaskCanceledException"/>
         * <exception cref="GoogleApiException"/>
         */
        private async Task CreateFileAsync(string fileName, string filePath, string contentType, CancellationToken ct) {
            using FileStream stream = new(filePath, FileMode.Open);
            Google.Apis.Drive.v3.Data.File fileMetaData = new() {
                Name = fileName,
                Parents = ["appDataFolder"]
            };
            var request = _driveService.Files.Create(
                fileMetaData,
                stream,
                contentType
            );
            IUploadProgress result = await request.UploadAsync(ct);
            if (result.Exception != null) {
                throw result.Exception;
            }
        }

        /**
         * <exception cref="Exception"/>
         * <exception cref="TaskCanceledException"/>
         * <exception cref="GoogleApiException"/>
         */
        private async Task UpdateFileAsync(string fileId, string filePath, string contentType, CancellationToken ct) {
            using FileStream stream = new(filePath, FileMode.Open);
            Google.Apis.Drive.v3.Data.File fileMetaData = new();
            var request = _driveService.Files.Update(
                fileMetaData,
                fileId,
                stream,
                contentType
            );
            IUploadProgress result = await request.UploadAsync(ct);
            if (result.Exception != null) {
                throw result.Exception;
            }
        }

        /**
         * <returns>The file content <c>string</c>.</returns>
         * <exception cref="Exception"/>
         * <exception cref="TaskCanceledException"/>
         * <exception cref="GoogleApiException"/>
         */
        private async Task<string> GetFile(string fileId, CancellationToken ct) {
            MemoryStream stream = new();
            FilesResource.GetRequest getRequest = _driveService.Files.Get(fileId);
            IDownloadProgress result = await getRequest.DownloadAsync(stream, ct);
            if (result.Exception != null) {
                throw result.Exception;
            }
            stream.Position = 0;
            using StreamReader reader = new(stream);
            return await reader.ReadToEndAsync();
        }

        private void ContentDialog_Closing(ContentDialog _0, ContentDialogClosingEventArgs args) {
            args.Cancel = !_closeDialog;
        }

        private void SyncDirectionRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            RadioButtons radioButtons = sender as RadioButtons;
            if (radioButtons.SelectedIndex == -1) {
                return;
            }
            TagFilterSyncResultInfoBar.IsOpen = false;
            BookmarkSyncResultInfoBar.IsOpen = false;
            TagFilterOptionCheckBox.IsChecked = false;
            BookmarkOptionCheckBox.IsChecked = false;
            TogglePrimaryButton();
            Border0.Visibility = Visibility.Visible;
            Border1.Visibility = Visibility.Collapsed;
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
                    Border1.Visibility = Visibility.Visible;
                    break;
                }
            }
        }

        private void TagFilterOptionCheckBox_Checked(object _0, RoutedEventArgs _1) {
            TogglePrimaryButton();
            if (SyncDirectionRadioButtons.SelectedIndex == 1) {
                FetchTagFilterOptionStackPanel.Visibility = Visibility.Visible;
            }
        }
        private void TagFilterOptionCheckBox_Unchecked(object _0, RoutedEventArgs _1) {
            TogglePrimaryButton();
            if (SyncDirectionRadioButtons.SelectedIndex == 1) {
                FetchTagFilterOptionStackPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void BookmarkOptionCheckBox_Checked(object _0, RoutedEventArgs _1) {
            TogglePrimaryButton();
            if (SyncDirectionRadioButtons.SelectedIndex == 1) {
                FetchBookmarkOptionStackPanel.Visibility = Visibility.Visible;
            }
        }
        private void BookmarkOptionCheckBox_Unchecked(object _0, RoutedEventArgs _1) {
            TogglePrimaryButton();
            if (SyncDirectionRadioButtons.SelectedIndex == 1) {
                FetchBookmarkOptionStackPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void FetchTagFilterOption1_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            RadioButtons radioButtons = sender as RadioButtons;
            if (radioButtons.SelectedIndex == -1) {
                return;
            }
            TogglePrimaryButton();
            switch (radioButtons.SelectedIndex) {
                // Overwrite option selected
                case 0: {
                    (FetchTagFilterOption2.Parent as StackPanel).Visibility = Visibility.Collapsed;
                    break;
                }
                // Append option selected
                case 1: {
                    (FetchTagFilterOption2.Parent as StackPanel).Visibility = Visibility.Visible;
                    break;
                }
            }
        }

        private void FetchTagFilterOption2_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            RadioButtons radioButtons = sender as RadioButtons;
            if (radioButtons.SelectedIndex == -1) {
                return;
            }
            TogglePrimaryButton();
        }

        private void FetchBookmarkOption1_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            RadioButtons radioButtons = sender as RadioButtons;
            if (radioButtons.SelectedIndex == -1) {
                return;
            }
            TogglePrimaryButton();
        }

        private void TogglePrimaryButton() {
            // Upload option selected
            if (SyncDirectionRadioButtons.SelectedIndex == 0) {
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
                    int option1Idx = FetchTagFilterOption1.SelectedIndex;
                    int option2Idx = FetchTagFilterOption2.SelectedIndex;
                    enable &= option1Idx != -1 && (option1Idx == 0 || option2Idx != -1);
                }
                if ((bool)BookmarkOptionCheckBox.IsChecked) {
                    enable &= FetchBookmarkOption1.SelectedIndex != -1;
                }
                IsPrimaryButtonEnabled = enable;
            }
        }
    }
}