using CommunityToolkit.WinUI;
using Google;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Upload;
using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static HitomiScrollViewerLib.SharedResources;
using static HitomiScrollViewerLib.Utils;

namespace HitomiScrollViewerLib.Controls.SearchPageComponents
{
    public sealed partial class SyncContentDialog : ContentDialog {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree("SyncContentDialog");

        private bool _closeDialog = true;
        private readonly DriveService _driveService;
        private bool _isSyncing = false;
        private CancellationTokenSource _cts;

        public SyncContentDialog(DriveService driveService) {
            _driveService = driveService;
            InitializeComponent();
            CloseButtonText = TEXT_CLOSE;

            TagFilterOptionCheckBox.Content = TEXT_TAG_FILTER_SETS;
            BookmarkOptionCheckBox.Content = TEXT_GALLERIES;

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
                SyncProgressBar.IsIndeterminate = true;
                CloseButtonText = TEXT_CANCEL;
                TagFilterSyncResultInfoBar.IsOpen = false;
                BookmarkSyncResultInfoBar.IsOpen = false;
            } else {
                SyncProgressBar.Visibility = Visibility.Collapsed;
                CloseButtonText = TEXT_CLOSE;
            }
        }

        private async Task StartUploadAsync(UserDataType userDataType, Google.Apis.Drive.v3.Data.File file) {
            string uploadFileName = userDataType switch {
                UserDataType.TagFilterSet => Path.GetFileName(TFS_MAIN_DATABASE_PATH_V3),
                UserDataType.Gallery => Path.GetFileName(GALLERIES_MAIN_DATABASE_PATH_V3),
                _ => throw new InvalidOperationException($"Invalid {nameof(userDataType)}: {userDataType}")
            };
            string localSrcFilePath = userDataType switch {
                UserDataType.TagFilterSet => TFS_MAIN_DATABASE_PATH_V3,
                UserDataType.Gallery => GALLERIES_MAIN_DATABASE_PATH_V3,
                _ => throw new InvalidOperationException($"Invalid {nameof(userDataType)}: {userDataType}")
            };
            InfoBar infoBar = userDataType switch {
                UserDataType.TagFilterSet => TagFilterSyncResultInfoBar,
                UserDataType.Gallery => BookmarkSyncResultInfoBar,
                _ => throw new InvalidOperationException($"Invalid {nameof(userDataType)}: {userDataType}")
            };
            string infoBarTitle = userDataType switch {
                UserDataType.TagFilterSet => TEXT_TAG_FILTER_SETS,
                UserDataType.Gallery => TEXT_GALLERIES,
                _ => throw new InvalidOperationException($"Invalid {nameof(userDataType)}: {userDataType}")
            };

            try {
                switch (userDataType) {
                    case UserDataType.TagFilterSet: {
                        await TagFilterSetContext.Main.Database.CloseConnectionAsync();
                        break;
                    }
                    case UserDataType.Gallery: {
                        await GalleryContext.Main.Database.CloseConnectionAsync();
                        break;
                    }
                }

                using FileStream uploadStream = new(localSrcFilePath, FileMode.Open);
                ResumableUpload mediaUpload =
                    file == null
                    ? GetCreateMediaUpload(_driveService, uploadStream, uploadFileName, DB_MIME_TYPE)
                    : GetUpdateMediaUpload(_driveService, uploadStream, file.Id, DB_MIME_TYPE);
                AttachProgressChangedEventHandler(mediaUpload, mediaUpload.ContentStream.Length);
                IUploadProgress uploadProgress = await mediaUpload.UploadAsync(_cts.Token);
                if (uploadProgress.Exception != null) {
                    throw uploadProgress.Exception;
                }

                infoBar.Severity = InfoBarSeverity.Success;
                infoBar.Title = infoBarTitle;
                infoBar.Message = _resourceMap.GetValue("InfoBar_Upload_Success_Message").ValueAsString;
            } catch (TaskCanceledException) {
                infoBar.Severity = InfoBarSeverity.Informational;
                infoBar.Title = infoBarTitle;
                infoBar.Message = _resourceMap.GetValue("InfoBar_Upload_Canceled_Message").ValueAsString;
            } catch (Exception e) {
                infoBar.Severity = InfoBarSeverity.Error;
                infoBar.Title = TEXT_ERROR;
                if (e is GoogleApiException googleApiException && googleApiException.HttpStatusCode == System.Net.HttpStatusCode.Forbidden) {
                    infoBar.Message = _resourceMap.GetValue("InfoBar_Error_Unauthorized_Message").ValueAsString;
                } else {
                    infoBar.Message = _resourceMap.GetValue("InfoBar_Error_Unknown_Message").ValueAsString;
                }
            } finally {
                infoBar.IsOpen = true;
            }
        }

        private static void HandleFileNotUploaded(InfoBar infoBar, string infoBarTitle) {
            infoBar.Severity = InfoBarSeverity.Error;
            infoBar.Title = infoBarTitle;
            infoBar.Message = _resourceMap.GetValue("InfoBar_Error_FileNotUploaded_Message").ValueAsString;
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog _0, ContentDialogButtonClickEventArgs _1) {
            _cts = new();
            _closeDialog = false;
            StartStopSync(true);

            FilesResource.ListRequest listRequest = _driveService.Files.List();
            listRequest.Spaces = "appDataFolder";
            listRequest.Fields = "nextPageToken, files(id, name, size)";
            listRequest.PageSize = 8;
            Google.Apis.Drive.v3.Data.File tfssFile = null;
            Google.Apis.Drive.v3.Data.File galleriesFile = null;
            try {
                Google.Apis.Drive.v3.Data.FileList fileList = await listRequest.ExecuteAsync(_cts.Token);
                if (fileList != null) {
                    foreach (var file in fileList.Files) {
                        if (file.Name == Path.GetFileName(TFS_MAIN_DATABASE_PATH_V3)) {
                            tfssFile = file;
                        } else if (file.Name == Path.GetFileName(GALLERIES_MAIN_DATABASE_PATH_V3)) {
                            galleriesFile = file;
                        }
                    }
                }
            } catch (Exception) { }

            // Upload
            if (SyncDirectionRadioButtons.SelectedIndex == 0) {
                // Upload tag filter sets
                if (TagFilterOptionCheckBox.IsChecked == true) {
                    await StartUploadAsync(UserDataType.TagFilterSet, tfssFile);
                }
                // Upload galleries
                if (BookmarkOptionCheckBox.IsChecked == true) {
                    await StartUploadAsync(UserDataType.Gallery, galleriesFile);
                }
            }
            // Fetch
            else {
                // Fetch tag filter sets
                if (TagFilterOptionCheckBox.IsChecked == true) {
                    // file is not uploaded yet
                    if (tfssFile == null) {
                        HandleFileNotUploaded(TagFilterSyncResultInfoBar, TEXT_TAG_FILTER_SETS);
                    }
                    // file exists
                    else {
                        try {
                            FilesResource.GetRequest request = _driveService.Files.Get(tfssFile.Id);
                            AttachProgressChangedEventHandler(request, (long)tfssFile.Size);
                            // Overwrite tag filter sets
                            // This operation must be atomic and not be cancelled because
                            // it is disposing and creating a new TagFilterSetContext.Main
                            if (FetchTagFilterOption1.SelectedIndex == 0) {
                                await TagFilterSetContext.Main.DisposeAsync();
                                await DownloadAndWriteAsync(
                                    request,
                                    TFS_MAIN_DATABASE_PATH_V3,
                                    CancellationToken.None
                                );
                                TagFilterSetContext.Main = new(Path.GetFileName(TFS_MAIN_DATABASE_PATH_V3));
                                await TagFilterSetContext.Main.TagFilterSets.LoadAsync(CancellationToken.None);
                                await DispatcherQueue.EnqueueAsync(TagFilterSetEditor.Main.Init);
                            }
                            // Append tag filter sets
                            else {
                                await DownloadAndWriteAsync(
                                    request,
                                    TFS_TEMP_DATABASE_PATH_V3,
                                    _cts.Token
                                );
                                using TagFilterSetContext tempCtx = new(Path.GetFileName(TFS_TEMP_DATABASE_PATH_V3));
                                await tempCtx.TagFilterSets.LoadAsync(_cts.Token);
                                IEnumerable<string> fetchedTFSNames = tempCtx.TagFilterSets.Select(tfs => tfs.Name);
                                // Replace locally stored tfss with duplicate names from the cloud
                                foreach (string fetchedTFSName in fetchedTFSNames) {
                                    IQueryable<TagFilterSet> localDuplicateQuery = TagFilterSetContext.Main.TagFilterSets.Where(tfs => tfs.Name == fetchedTFSName);
                                    ICollection<TagFilterV3> fetchedTagFilters =
                                        tempCtx.TagFilterSets
                                        .Where(tfs => tfs.Name == fetchedTFSName)
                                        .Include(tfs => tfs.TagFilters)
                                        .First()
                                        .TagFilters
                                        .Select(tf => new TagFilterV3() { Category = tf.Category, Tags = tf.Tags})
                                        .ToList();
                                    // fetched tfs does not exist locally so just add
                                    if (!localDuplicateQuery.Any()) {
                                        await TagFilterSetContext.Main.TagFilterSets.AddAsync(
                                            new() {
                                                Name = fetchedTFSName,
                                                TagFilters = fetchedTagFilters
                                            },
                                            _cts.Token
                                        );
                                    }
                                    // replace local tfs with duplicate name
                                    else if (FetchTagFilterOption2.SelectedIndex == 0) {
                                        localDuplicateQuery.First().TagFilters = fetchedTagFilters;
                                    }
                                }
                                await TagFilterSetContext.Main.SaveChangesAsync(CancellationToken.None);
                                TagFilterSetEditor.Main.TagFilterSetComboBox.SelectedIndex = -1;
                            }
                            TagFilterSyncResultInfoBar.Severity = InfoBarSeverity.Success;
                            TagFilterSyncResultInfoBar.Title = TEXT_TAG_FILTER_SETS;
                            TagFilterSyncResultInfoBar.Message = _resourceMap.GetValue("InfoBar_Fetch_Success_Message").ValueAsString;
                        } catch (TaskCanceledException) {
                            TagFilterSyncResultInfoBar.Severity = InfoBarSeverity.Informational;
                            TagFilterSyncResultInfoBar.Title = TEXT_TAG_FILTER_SETS;
                            TagFilterSyncResultInfoBar.Message = _resourceMap.GetValue("InfoBar_Fetch_Canceled_Message").ValueAsString;
                        } catch (Exception e) {
                            TagFilterSyncResultInfoBar.Severity = InfoBarSeverity.Error;
                            TagFilterSyncResultInfoBar.Title = TEXT_ERROR;
                            if (e is GoogleApiException googleApiException && googleApiException.HttpStatusCode == System.Net.HttpStatusCode.Forbidden) {
                                TagFilterSyncResultInfoBar.Message = _resourceMap.GetValue("InfoBar_Error_Unauthorized_Message").ValueAsString;
                            } else {
                                TagFilterSyncResultInfoBar.Message = _resourceMap.GetValue("InfoBar_Error_Unknown_Message").ValueAsString;
                            }
                        }
                    }
                    TagFilterSyncResultInfoBar.IsOpen = true;
                }
                // Fetch bookmarks
                if (BookmarkOptionCheckBox.IsChecked == true) {
                    // file is not uploaded yet
                    if (tfssFile == null) {
                        HandleFileNotUploaded(BookmarkSyncResultInfoBar, TEXT_GALLERIES);
                    }
                    // file exists
                    else {
                        //try {
                        //    string fetchedBookmarksData = await GetFile(galleriesFile, _cts.Token);
                        //    IEnumerable<Gallery> fetchedBookmarkGalleries = (IEnumerable<Gallery>)JsonSerializer.Deserialize(
                        //        fetchedBookmarksData,
                        //        typeof(IEnumerable<Gallery>),
                        //        DEFAULT_SERIALIZER_OPTIONS
                        //    );
                        //    // append fetched galleries to existing bookmark if they are not already in the bookmark
                        //    IEnumerable<Gallery> localBookmarkGalleries = SearchPage.BookmarkItems.Select(item => item.gallery);
                        //    IEnumerable<Gallery> appendingGalleries = fetchedBookmarkGalleries.ExceptBy(
                        //        localBookmarkGalleries.Select(gallery => gallery.id),
                        //        gallery => gallery.id
                        //    );
                        //    foreach (var gallery in appendingGalleries) {
                        //        BookmarkItem appendedBookmarkItem = MainWindow.SearchPage.AddBookmark(gallery);
                        //        // start downloading all appended galleries if the corresponding option is checked
                        //        if (FetchBookmarkOption1.SelectedIndex == 0) {
                        //            MainWindow.SearchPage.TryDownload(gallery.id, appendedBookmarkItem);
                        //        }
                        //    }
                        //    BookmarkSyncResultInfoBar.Severity = InfoBarSeverity.Success;
                        //    BookmarkSyncResultInfoBar.Title = TEXT_GALLERIES;
                        //    BookmarkSyncResultInfoBar.Message = _resourceMap.GetValue("InfoBar_Fetch_Success_Message").ValueAsString;
                        //} catch (TaskCanceledException) {
                        //    TagFilterSyncResultInfoBar.Severity = InfoBarSeverity.Informational;
                        //    TagFilterSyncResultInfoBar.Title = TEXT_GALLERIES;
                        //    TagFilterSyncResultInfoBar.Message = _resourceMap.GetValue("InfoBar_Fetch_Canceled_Message").ValueAsString;
                        //} catch (Exception e) {
                        //    BookmarkSyncResultInfoBar.Severity = InfoBarSeverity.Error;
                        //    BookmarkSyncResultInfoBar.Title = TEXT_ERROR;
                        //    if (e is GoogleApiException googleApiException && googleApiException.HttpStatusCode == System.Net.HttpStatusCode.Forbidden) {
                        //        BookmarkSyncResultInfoBar.Message = _resourceMap.GetValue("InfoBar_Error_Unauthorized_Message").ValueAsString;
                        //    } else {
                        //        BookmarkSyncResultInfoBar.Message = _resourceMap.GetValue("InfoBar_Error_Unknown_Message").ValueAsString;
                        //    }
                        //}
                    }
                    BookmarkSyncResultInfoBar.IsOpen = true;
                }
            }
            StartStopSync(false);
        }

        private void AttachProgressChangedEventHandler(object request, long totalByteSize) {
            // if totalByteSize is less than 5MB than make SyncProgressBar indeterminate because
            // ProgressChanged event only fires at the start and completed status because the file
            // size is so small
            if (totalByteSize < 5_000_000) {
                SyncProgressBar.IsIndeterminate = true;
                return;
            }
            SyncProgressBar.IsIndeterminate = false;
            SyncProgressBar.Value = 0;
            SyncProgressBar.Maximum = totalByteSize;
            if (request is ResumableUpload uploadRequest) {
                uploadRequest.ProgressChanged += (IUploadProgress progress) => {
                    DispatcherQueue.TryEnqueue(
                        () => {
                            lock (SyncProgressBar) {
                                SyncProgressBar.Value = progress.BytesSent;
                            }
                        }
                    );
                };
            } else if (request is FilesResource.GetRequest getRequest) {
                getRequest.MediaDownloader.ProgressChanged += (IDownloadProgress progress) => {
                    DispatcherQueue.TryEnqueue(
                        () => {
                            lock (SyncProgressBar) {
                                SyncProgressBar.Value = progress.BytesDownloaded;
                            }
                        }
                    );
                };
            }
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
                    FetchTagFilterOptionStackPanel.Visibility = TagFilterOptionCheckBox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
                    FetchBookmarkOptionStackPanel.Visibility = BookmarkOptionCheckBox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
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
                IsPrimaryButtonEnabled = TagFilterOptionCheckBox.IsChecked == true || BookmarkOptionCheckBox.IsChecked == true;
            }
            // Fetch option selected
            else {
                if (TagFilterOptionCheckBox.IsChecked == false && BookmarkOptionCheckBox.IsChecked == false) {
                    IsPrimaryButtonEnabled = false;
                    return;
                }
                bool enable = true;
                if (TagFilterOptionCheckBox.IsChecked == true) {
                    int option1Idx = FetchTagFilterOption1.SelectedIndex;
                    int option2Idx = FetchTagFilterOption2.SelectedIndex;
                    enable &= option1Idx != -1 && (option1Idx == 0 || option2Idx != -1);
                }
                if (BookmarkOptionCheckBox.IsChecked == true) {
                    enable &= FetchBookmarkOption1.SelectedIndex != -1;
                }
                IsPrimaryButtonEnabled = enable;
            }
        }
    }
}
