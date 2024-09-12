﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Google;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Upload;
using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.Models.SearchPageModels;
using HitomiScrollViewerLib.Views.SearchPageViews;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static HitomiScrollViewerLib.SharedResources;
using static HitomiScrollViewerLib.Constants;

namespace HitomiScrollViewerLib.ViewModels.SearchPageVMs {
    public partial class SyncContentDialogVM(DriveService _driveService) : ObservableObject {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(SyncContentDialog).Name);

        private bool _closeDialog = true;
        private bool _isSyncing = false;
        private CancellationTokenSource _cts;

        [ObservableProperty]
        private bool _isEnabled;

        [ObservableProperty]
        private string _closeButtonText = TEXT_CLOSE;

        [ObservableProperty]
        private Visibility _progressBarVisibility;
        [ObservableProperty]
        private bool _isProgressBarIndeterminate;
        [ObservableProperty]
        private double _progressBarValue;
        [ObservableProperty]
        private double _progressBarMaximum;


        [ObservableProperty]
        private bool _isUploadWarningInfoBarOpen;

        // Tag filter and Gallery separator
        [ObservableProperty]
        private Visibility _border_1_Visibility;
        // Gallery sync options separator
        [ObservableProperty]
        private Visibility _border_2_Visibility;

        [ObservableProperty]
        private bool isTFOptionChecked;
        partial void OnIsTFOptionCheckedChanged(bool value) {
            // if checked and option is fetch
            if (value && RadioButtons_1_SelectedIndex == 1) {
                FetchTFOptionsVisibility = Visibility.Visible;
            }
            // if above has problem use below
            //if (value) {
            //    FetchTFOptionsVisibility =
            //        RadioButtons_1_SelectedIndex == 0 ?
            //        Visibility.Collapsed :
            //        Visibility.Visible;
            //}
        }
        [ObservableProperty]
        private Visibility _tfCheckBoxVisibility;
        [ObservableProperty]
        private Visibility _fetchTFOptionsVisibility;
        [ObservableProperty]
        private Visibility _radioButtons_3_Visibility;

        [ObservableProperty]
        private bool _isGalleryOptionChecked;
        partial void OnIsGalleryOptionCheckedChanged(bool value) {
            // if checked and option is fetch
            if (value && RadioButtons_1_SelectedIndex == 1) {
                FetchGalleryOptionsVisibility = Visibility.Visible;
            }
            // if above has problem use below
            //if (value) {
            //    FetchGalleryOptionsVisibility =
            //        RadioButtons_1_SelectedIndex == 0 ?
            //        Visibility.Collapsed :
            //        Visibility.Visible;
            //}
        }
        [ObservableProperty]
        private Visibility _galleryCheckBoxVisibility;
        [ObservableProperty]
        private Visibility _fetchGalleryOptionsVisibility;


        // Sync direction (Upload or Fetch)
        [ObservableProperty]
        private int _radioButtons_1_SelectedIndex;
        partial void OnRadioButtons_1_SelectedIndexChanged(int value) {
            if (value == -1) {
                return;
            }
            TFInfoBarModel.IsOpen = false;
            GalleryInfoBarModel.IsOpen = false;
            IsTFOptionChecked = false;
            IsGalleryOptionChecked = false;
            Border_1_Visibility = Visibility.Visible;
            Border_2_Visibility = Visibility.Collapsed;
            TfCheckBoxVisibility = Visibility.Visible;
            GalleryCheckBoxVisibility = Visibility.Visible;
            switch (value) {
                // Upload option selected
                case 0: {
                    IsUploadWarningInfoBarOpen = true;
                    FetchTFOptionsVisibility = Visibility.Collapsed;
                    FetchGalleryOptionsVisibility = Visibility.Collapsed;
                    break;
                }
                // Fetch option selected
                case 1: {
                    IsUploadWarningInfoBarOpen = false;
                    FetchTFOptionsVisibility = IsTFOptionChecked ? Visibility.Visible : Visibility.Collapsed;
                    FetchGalleryOptionsVisibility = IsGalleryOptionChecked ? Visibility.Visible : Visibility.Collapsed;
                    Border_2_Visibility = Visibility.Visible;
                    break;
                }
            }
        }
        // Tag Filter Fetch option 1 (Overwrite or Append)
        [ObservableProperty]
        private int _radioButtons_2_SelectedIndex;
        partial void OnRadioButtons_2_SelectedIndexChanged(int value) {
            switch (value) {
                // Overwrite option selected
                case 0: {
                    RadioButtons_3_Visibility = Visibility.Collapsed;
                    break;
                }
                // Append option selected
                case 1: {
                    RadioButtons_3_Visibility = Visibility.Visible;
                    break;
                }
            }
        }
        // Tag Filter Fetch option 2 (Keep duplicate or Replace duplicate)
        [ObservableProperty]
        private int _radioButtons_3_SelectedIndex;
        // Gallery Fetch option 1 (Download after fetching or not)
        [ObservableProperty]
        private int _radioButtons_4_SelectedIndex;

        public InfoBarModel TFInfoBarModel { get; } = new() { IsOpen = false };
        public InfoBarModel GalleryInfoBarModel { get; } = new() { IsOpen = false };

        public void ContentDialog_CloseButtonClick(ContentDialog _0, ContentDialogButtonClickEventArgs _1) {
            if (_isSyncing) {
                _cts.Cancel();
            } else {
                _closeDialog = true;
            }
        }

        public void ContentDialog_Closing(ContentDialog _0, ContentDialogClosingEventArgs args) {
            args.Cancel = !_closeDialog;
        }

        private void StartStopSync(bool start) {
            _isSyncing = start;
            IsEnabled = !start;
            if (start) {
                ProgressBarVisibility = Visibility.Visible;
                IsProgressBarIndeterminate = true;
                CloseButtonText = TEXT_CANCEL;
                TFInfoBarModel.IsOpen = false;
                GalleryInfoBarModel.IsOpen = false;
            } else {
                _cts.Dispose();
                ProgressBarVisibility = Visibility.Collapsed;
                CloseButtonText = TEXT_CLOSE;
            }
        }

        public enum UserDataType {
            TagFilterSet, Gallery
        }

        public static FilesResource.ListRequest GetListRequest(DriveService driveService) {
            FilesResource.ListRequest listRequest = driveService.Files.List();
            listRequest.Spaces = "appDataFolder";
            listRequest.Fields = "nextPageToken, files(id, name, size)";
            listRequest.PageSize = 8;
            return listRequest;
        }

        public static FilesResource.CreateMediaUpload GetCreateMediaUpload(
            DriveService driveService,
            string content,
            string fileName,
            string contentType
        ) {
            Google.Apis.Drive.v3.Data.File fileMetaData = new() {
                Name = fileName,
                Parents = ["appDataFolder"]
            };
            return driveService.Files.Create(
                fileMetaData,
                new MemoryStream(Encoding.UTF8.GetBytes(content)),
                contentType
            );
        }

        public static FilesResource.UpdateMediaUpload GetUpdateMediaUpload(
            DriveService driveService,
            string content,
            string fileId,
            string contentType
        ) {
            return driveService.Files.Update(
                new(),
                fileId,
                new MemoryStream(Encoding.UTF8.GetBytes(content)),
                contentType
            );
        }

        /**
         * <returns>The file content from Google Drive<c>string</c>.</returns>
         * <exception cref="Exception"/>
         * <exception cref="TaskCanceledException"/>
         * <exception cref="Google.GoogleApiException"/>
         */
        public static async Task DownloadAndWriteAsync(
            FilesResource.GetRequest request,
            string filePath,
            CancellationToken ct
        ) {
            using FileStream fileStream = new(filePath, FileMode.Create, FileAccess.Write) {
                Position = 0
            };
            IDownloadProgress result = await request.DownloadAsync(fileStream, ct);
            if (result.Exception != null) {
                throw result.Exception;
            }
        }

        private async Task StartUploadAsync(UserDataType userDataType, Google.Apis.Drive.v3.Data.File file) {
            string uploadFileName = userDataType switch {
                UserDataType.TagFilterSet => Path.GetFileName(TFS_SYNC_FILE_PATH),
                UserDataType.Gallery => Path.GetFileName(GALLERIES_SYNC_FILE_PATH),
                _ => throw new InvalidOperationException($"Invalid {nameof(userDataType)}: {userDataType}")
            };
            string uploadFileContent = userDataType switch {
                UserDataType.TagFilterSet => JsonSerializer.Serialize(HitomiContext.Main.TagFilters, TF_SERIALIZER_OPTIONS),
                UserDataType.Gallery => JsonSerializer.Serialize(HitomiContext.Main.Galleries, TF_SERIALIZER_OPTIONS),
                _ => throw new InvalidOperationException($"Invalid {nameof(userDataType)}: {userDataType}")
            };
            InfoBarModel infoBarModel = userDataType switch {
                UserDataType.TagFilterSet => TFInfoBarModel,
                UserDataType.Gallery => GalleryInfoBarModel,
                _ => throw new InvalidOperationException($"Invalid {nameof(userDataType)}: {userDataType}")
            };
            string infoBarTitle = userDataType switch {
                UserDataType.TagFilterSet => TEXT_TAG_FILTERS,
                UserDataType.Gallery => TEXT_GALLERIES,
                _ => throw new InvalidOperationException($"Invalid {nameof(userDataType)}: {userDataType}")
            };

            try {
                ResumableUpload mediaUpload =
                    file == null
                    ? GetCreateMediaUpload(_driveService, uploadFileContent, uploadFileName, MediaTypeNames.Application.Json)
                    : GetUpdateMediaUpload(_driveService, uploadFileContent, file.Id, MediaTypeNames.Application.Json);
                AttachProgressChangedEventHandler(mediaUpload, mediaUpload.ContentStream.Length);
                IUploadProgress uploadProgress = await mediaUpload.UploadAsync(_cts.Token);
                if (uploadProgress.Exception != null) {
                    throw uploadProgress.Exception;
                }

                SetInfoBarModel(
                    infoBarModel,
                    InfoBarSeverity.Success,
                    infoBarTitle,
                    _resourceMap.GetValue("InfoBar_Upload_Success_Message").ValueAsString
                );
            } catch (TaskCanceledException) {
                SetInfoBarModel(
                    infoBarModel,
                    InfoBarSeverity.Informational,
                    infoBarTitle,
                    _resourceMap.GetValue("InfoBar_Upload_Canceled_Message").ValueAsString
                );
            } catch (Exception e) {
                string message;
                if (e is GoogleApiException googleApiException && googleApiException.HttpStatusCode == System.Net.HttpStatusCode.Forbidden) {
                    message = _resourceMap.GetValue("InfoBar_Error_Unauthorized_Message").ValueAsString;
                } else {
                    message = _resourceMap.GetValue("InfoBar_Error_Unknown_Message").ValueAsString;
                }
                SetInfoBarModel(
                    infoBarModel,
                    InfoBarSeverity.Error,
                    TEXT_ERROR,
                    message
                );
            } finally {
                infoBarModel.IsOpen = true;
            }
        }

        private static void SetInfoBarModel(InfoBarModel infoBarModel, InfoBarSeverity severity, string title, string message) {
            infoBarModel.Severity = severity;
            infoBarModel.Title = title;
            infoBarModel.Message = message;
        }

        public ICommand PrimaryButtonCommand => new RelayCommand(HandlePrimaryButtonClick, CanClickPrimaryButton);

        private async void HandlePrimaryButtonClick() {
            _cts = new();
            _closeDialog = false;
            StartStopSync(true);

            Google.Apis.Drive.v3.Data.File tfssFile = null;
            Google.Apis.Drive.v3.Data.File galleriesFile = null;
            try {
                Google.Apis.Drive.v3.Data.FileList fileList = await GetListRequest(_driveService).ExecuteAsync(_cts.Token);
                if (fileList != null) {
                    foreach (var file in fileList.Files) {
                        if (file.Name == Path.GetFileName(TFS_SYNC_FILE_PATH)) {
                            tfssFile = file;
                        } else if (file.Name == Path.GetFileName(GALLERIES_SYNC_FILE_PATH)) {
                            galleriesFile = file;
                        }
                    }
                }
            } catch (Exception) { }

            // Upload
            if (RadioButtons_1_SelectedIndex == 0) {
                // Upload tag filter sets
                if (IsTFOptionChecked) {
                    await StartUploadAsync(UserDataType.TagFilterSet, tfssFile);
                }
                // Upload galleries
                if (IsGalleryOptionChecked) {
                    await StartUploadAsync(UserDataType.Gallery, galleriesFile);
                }
            }
            // Fetch
            else {
                // Fetch tag filter sets
                if (IsTFOptionChecked) {
                    // file is not uploaded yet
                    if (tfssFile == null) {
                        SetInfoBarModel(
                            TFInfoBarModel,
                            InfoBarSeverity.Error,
                            TEXT_TAG_FILTERS,
                            _resourceMap.GetValue("InfoBar_Error_FileNotUploaded_Message").ValueAsString
                        );
                    }
                    // file exists
                    else {
                        try {
                            FilesResource.GetRequest request = _driveService.Files.Get(tfssFile.Id);
                            AttachProgressChangedEventHandler(request, (long)tfssFile.Size);
                            await DownloadAndWriteAsync(
                                request,
                                TFS_SYNC_FILE_PATH,
                                _cts.Token
                            );
                            string fetchedFileContent = await File.ReadAllTextAsync(TFS_SYNC_FILE_PATH, _cts.Token);
                            IEnumerable<TagFilter> fetchedTFSs =
                                JsonSerializer
                                .Deserialize<IEnumerable<TagFilter>>
                                (fetchedFileContent, TF_SERIALIZER_OPTIONS);
                            // Overwrite
                            if (RadioButtons_2_SelectedIndex == 0) {
                                HitomiContext.Main.TagFilters.RemoveRange(HitomiContext.Main.TagFilters);
                                HitomiContext.Main.TagFilters.AddRange(fetchedTFSs);
                                HitomiContext.Main.SaveChanges();
                            }
                            // Append
                            else {
                                foreach (TagFilter fetchedTFS in fetchedTFSs) {
                                    TagFilter existingTFS = HitomiContext.Main.TagFilters.FirstOrDefault(tfs => tfs.Name == fetchedTFS.Name);
                                    // no duplicate name so just add
                                    if (existingTFS == null) {
                                        HitomiContext.Main.TagFilters.Add(fetchedTFS);
                                    }
                                    // if replace option is selected, replace local tfs tags with duplicate name
                                    else if (RadioButtons_3_SelectedIndex == 0) {
                                        existingTFS.Tags = fetchedTFS.Tags;
                                    }
                                }
                                HitomiContext.Main.SaveChanges();
                            }
                            SetInfoBarModel(
                                TFInfoBarModel,
                                InfoBarSeverity.Success,
                                TEXT_TAG_FILTERS,
                                _resourceMap.GetValue("InfoBar_Fetch_Success_Message").ValueAsString
                            );
                        } catch (TaskCanceledException) {
                            SetInfoBarModel(
                                TFInfoBarModel,
                                InfoBarSeverity.Informational,
                                TEXT_TAG_FILTERS,
                                _resourceMap.GetValue("InfoBar_Fetch_Canceled_Message").ValueAsString
                            );
                        } catch (Exception e) {
                            string message;
                            if (e is GoogleApiException googleApiException && googleApiException.HttpStatusCode == System.Net.HttpStatusCode.Forbidden) {
                                message = _resourceMap.GetValue("InfoBar_Error_Unauthorized_Message").ValueAsString;
                            } else {
                                message = _resourceMap.GetValue("InfoBar_Error_Unknown_Message").ValueAsString;
                            }
                            SetInfoBarModel(
                                TFInfoBarModel,
                                InfoBarSeverity.Error,
                                TEXT_ERROR,
                                message
                            );
                        }
                    }
                    TFInfoBarModel.IsOpen = true;
                }
                // Fetch bookmarks
                if (IsGalleryOptionChecked) {
                    // file is not uploaded yet
                    if (tfssFile == null) {
                        SetInfoBarModel(
                            TFInfoBarModel,
                            InfoBarSeverity.Error,
                            TEXT_TAG_FILTERS,
                            _resourceMap.GetValue("InfoBar_Error_FileNotUploaded_Message").ValueAsString
                        );
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
                    GalleryInfoBarModel.IsOpen = true;
                }
            }
            StartStopSync(false);
        }

        private bool CanClickPrimaryButton() {
            // Nothing selected
            if (RadioButtons_1_SelectedIndex == -1) {
                return false;
            }
            // Upload option selected
            else if (RadioButtons_1_SelectedIndex == 0) {
                return IsTFOptionChecked || IsGalleryOptionChecked;
            }
            // Fetch option selected
            else {
                if (!IsTFOptionChecked && !IsGalleryOptionChecked) {
                    return false;
                }
                bool enable = true;
                if (IsTFOptionChecked) {
                    enable &=
                        RadioButtons_2_SelectedIndex != -1 &&
                        (RadioButtons_2_SelectedIndex == 0 || RadioButtons_3_SelectedIndex != -1);
                }
                if (IsGalleryOptionChecked) {
                    enable &= RadioButtons_4_SelectedIndex != -1;
                }
                return enable;
            }
        }

        private void AttachProgressChangedEventHandler(object request, long totalByteSize) {
            // if totalByteSize is less than 5MB than make SyncProgressBar indeterminate because
            // ProgressChanged event only fires at the start and completed status because the file
            // size is so small
            if (totalByteSize < 5_000_000) {
                IsProgressBarIndeterminate = true;
                return;
            }
            IsProgressBarIndeterminate = false;
            ProgressBarValue = 0;
            ProgressBarMaximum = totalByteSize;
            if (request is ResumableUpload uploadRequest) {
                uploadRequest.ProgressChanged += (progress) => ProgressBarValue = progress.BytesSent;
            } else if (request is FilesResource.GetRequest getRequest) {
                getRequest.MediaDownloader.ProgressChanged += (progress) => ProgressBarValue = progress.BytesDownloaded;
            }
        }
    }
}