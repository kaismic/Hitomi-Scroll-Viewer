using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using Google;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Upload;
using HitomiScrollViewerLib.DAOs;
using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.DTOs;
using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.Models;
using HitomiScrollViewerLib.ViewModels.PageVMs;
using HitomiScrollViewerLib.Views.SearchPageViews;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static HitomiScrollViewerLib.Constants;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.ViewModels.SearchPageVMs {
    public partial class SyncContentDialogVM : DQObservableObject {
        private static readonly string SUBTREE_NAME = typeof(SyncContentDialog).Name;

        private readonly TagFilterDAO _tagFilterDAO;
        public SyncContentDialogVM(DriveService driveService, TagFilterDAO tagFilterDAO) {
            _tagFilterDAO = tagFilterDAO;
            DriveService = driveService;
            PropertyChanged += (object sender, PropertyChangedEventArgs e) => {
                if (e.PropertyName != nameof(IsPrimaryButtonEnabled) && e.PropertyName != nameof(IsEnabled)) {
                    IsPrimaryButtonEnabled = CanClickPrimaryButton();
                }
            };
        }

        public DriveService DriveService { get; }

        private bool _closeDialog = true;
        private bool _isSyncing = false;
        private CancellationTokenSource _cts;

        [ObservableProperty]
        private bool _isEnabled;
        [ObservableProperty]
        private bool _isPrimaryButtonEnabled;

        [ObservableProperty]
        private string _closeButtonText;
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
        private Visibility _border1Visibility;
        // Gallery sync options separator
        [ObservableProperty]
        private Visibility _border2Visibility;

        [ObservableProperty]
        private bool _isTFOptionChecked;
        partial void OnIsTFOptionCheckedChanged(bool value) {
            // if checked and option is fetch
            if (value && RadioButtons1SelectedIndex == 1) {
                FetchTFOptionsVisibility = Visibility.Visible;
            }
            // if above has problem use below
            //if (value) {
            //    FetchTFOptionsVisibility =
            //        RadioButtons1SelectedIndex == 0 ?
            //        Visibility.Collapsed :
            //        Visibility.Visible;
            //}
        }

        [ObservableProperty]
        private Visibility _tfCheckBoxVisibility;
        [ObservableProperty]
        private Visibility _fetchTFOptionsVisibility;
        [ObservableProperty]
        private Visibility _radioButtons3Visibility;


        [ObservableProperty]
        private bool _isGalleryOptionChecked;
        partial void OnIsGalleryOptionCheckedChanged(bool value) {
            if (value && RadioButtons1SelectedIndex == 1) {
                FetchGalleryOptionsVisibility = Visibility.Visible;
            }
            // if above has problem use below
            //if (value) {
            //    FetchGalleryOptionsVisibility =
            //        RadioButtons1SelectedIndex == 0 ?
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
        private int _radioButtons1SelectedIndex;
        partial void OnRadioButtons1SelectedIndexChanged(int value) {
            if (value == -1) {
                return;
            }
            TFInfoBarModel.IsOpen = false;
            GalleryInfoBarModel.IsOpen = false;
            IsTFOptionChecked = false;
            IsGalleryOptionChecked = false;
            Border1Visibility = Visibility.Visible;
            Border2Visibility = Visibility.Collapsed;
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
                    Border2Visibility = Visibility.Visible;
                    break;
                }
            }
        }

        // Tag Filter Fetch option 1 (Overwrite or Append)
        [ObservableProperty]
        private int _radioButtons2SelectedIndex;
        partial void OnRadioButtons2SelectedIndexChanged(int value) {
            switch (value) {
                // Overwrite option selected
                case 0: {
                    RadioButtons3Visibility = Visibility.Collapsed;
                    break;
                }
                // Append option selected
                case 1: {
                    RadioButtons3Visibility = Visibility.Visible;
                    break;
                }
            }
        }

        // Tag Filter Fetch option 2 (Keep duplicate or Replace duplicate)
        [ObservableProperty]
        private int _radioButtons3SelectedIndex;

        // Gallery Fetch option 1 (Download after fetching or not)
        [ObservableProperty]
        private int _radioButtons4SelectedIndex;

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
         * <exception cref="GoogleApiException"/>
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
            using HitomiContext context = new();
            string uploadFileContent;
            switch (userDataType) {
                case UserDataType.TagFilterSet:
                    IEnumerable<TagFilterSyncDTO> tfDTOs =
                        context.TagFilters.AsNoTracking()
                        .Include(tf => tf.Tags)
                        .Select(tf => tf.ToTagFilterSyncDTO());
                    uploadFileContent = JsonSerializer.Serialize(tfDTOs);
                    break;
                case UserDataType.Gallery:
                    IEnumerable<GallerySyncDTO> galleryDTOs =
                        context.Galleries.AsNoTracking()
                        .Include(g => g.Files)
                        .Include(g => g.Tags)
                        .Select(g => g.ToGallerySyncDTO());
                    uploadFileContent = JsonSerializer.Serialize(galleryDTOs);
                    break;
                default:
                    throw new InvalidOperationException($"Invalid {nameof(userDataType)}: {userDataType}");
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
                    ? GetCreateMediaUpload(DriveService, uploadFileContent, uploadFileName, MediaTypeNames.Application.Json)
                    : GetUpdateMediaUpload(DriveService, uploadFileContent, file.Id, MediaTypeNames.Application.Json);
                AttachProgressChangedEventHandler(mediaUpload, mediaUpload.ContentStream.Length);
                IUploadProgress uploadProgress = await mediaUpload.UploadAsync(_cts.Token);
                if (uploadProgress.Exception != null) {
                    throw uploadProgress.Exception;
                }

                SetInfoBarModel(
                    infoBarModel,
                    InfoBarSeverity.Success,
                    infoBarTitle,
                    "InfoBar_Upload_Success_Message".GetLocalized(SUBTREE_NAME)
                );
            } catch (TaskCanceledException) {
                SetInfoBarModel(
                    infoBarModel,
                    InfoBarSeverity.Informational,
                    infoBarTitle,
                    "InfoBar_Upload_Canceled_Message".GetLocalized(SUBTREE_NAME)
                );
            } catch (Exception e) {
                string message;
                if (e is GoogleApiException googleApiException && googleApiException.HttpStatusCode == System.Net.HttpStatusCode.Forbidden) {
                    message = "InfoBar_Error_Unauthorized_Message".GetLocalized(SUBTREE_NAME);
                } else {
                    message = "InfoBar_Error_Unknown_Message".GetLocalized(SUBTREE_NAME);
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

        public async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
            args.Cancel = true;
            _cts = new();
            _closeDialog = false;
            StartStopSync(true);

            Google.Apis.Drive.v3.Data.File tfssFile = null;
            Google.Apis.Drive.v3.Data.File galleriesFile = null;
            try {
                Google.Apis.Drive.v3.Data.FileList fileList = await GetListRequest(DriveService).ExecuteAsync(_cts.Token);
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
            if (RadioButtons1SelectedIndex == 0) {
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
                            "InfoBar_Error_FileNotUploaded_Message".GetLocalized(SUBTREE_NAME)
                        );
                    }
                    // file exists
                    else {
                        using HitomiContext context = new();
                        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                        try {
                            FilesResource.GetRequest request = DriveService.Files.Get(tfssFile.Id);
                            AttachProgressChangedEventHandler(request, tfssFile.Size.Value);
                            await DownloadAndWriteAsync(
                                request,
                                TFS_SYNC_FILE_PATH,
                                _cts.Token
                            );
                            string json = await File.ReadAllTextAsync(TFS_SYNC_FILE_PATH, _cts.Token);
                            IEnumerable<TagFilter> fetchedTagFilters =
                                JsonSerializer.Deserialize<IEnumerable<TagFilterSyncDTO>>(json)
                                .Select(dto => dto.ToTagFilter(context.Tags));
                            // Overwrite
                            if (RadioButtons2SelectedIndex == 0) {
                                _tagFilterDAO.RemoveRange(_tagFilterDAO.LocalTagFilters);
                                _tagFilterDAO.AddRange(fetchedTagFilters);
                            }
                            // Append
                            else {
                                foreach (TagFilter fetchedTF in fetchedTagFilters) {
                                    TagFilter localTF = context.TagFilters.AsNoTracking().FirstOrDefault(tf => tf.Name == fetchedTF.Name);
                                    // no duplicate name so just add
                                    if (localTF == null) {
                                        _tagFilterDAO.Add(fetchedTF);
                                    }
                                    // if replace option is selected, replace local tfs tags with duplicate name
                                    else if (RadioButtons3SelectedIndex == 0) {
                                        _tagFilterDAO.UpdateTags(localTF.Name, fetchedTF.Tags);
                                    }
                                }
                            }
                            SetInfoBarModel(
                                TFInfoBarModel,
                                InfoBarSeverity.Success,
                                TEXT_TAG_FILTERS,
                                "InfoBar_Fetch_Success_Message".GetLocalized(SUBTREE_NAME)
                            );
                        } catch (TaskCanceledException) {
                            SetInfoBarModel(
                                TFInfoBarModel,
                                InfoBarSeverity.Informational,
                                TEXT_TAG_FILTERS,
                                "InfoBar_Fetch_Canceled_Message".GetLocalized(SUBTREE_NAME)
                            );
                        } catch (Exception e) {
                            string message;
                            if (e is GoogleApiException googleApiException && googleApiException.HttpStatusCode == System.Net.HttpStatusCode.Forbidden) {
                                message = "InfoBar_Error_Unauthorized_Message".GetLocalized(SUBTREE_NAME);
                            } else {
                                message = "InfoBar_Error_Unknown_Message".GetLocalized(SUBTREE_NAME);
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
                            "InfoBar_Error_FileNotUploaded_Message".GetLocalized(SUBTREE_NAME)
                        );
                    }
                    // file exists
                    else {
                        // TODO

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
                        //    BookmarkSyncResultInfoBar.Message = "InfoBar_Fetch_Success_Message".GetLocalized(SUBTREE_NAME);
                        //} catch (TaskCanceledException) {
                        //    TagFilterSyncResultInfoBar.Severity = InfoBarSeverity.Informational;
                        //    TagFilterSyncResultInfoBar.Title = TEXT_GALLERIES;
                        //    TagFilterSyncResultInfoBar.Message = "InfoBar_Fetch_Canceled_Message".GetLocalized(SUBTREE_NAME);
                        //} catch (Exception e) {
                        //    BookmarkSyncResultInfoBar.Severity = InfoBarSeverity.Error;
                        //    BookmarkSyncResultInfoBar.Title = TEXT_ERROR;
                        //    if (e is GoogleApiException googleApiException && googleApiException.HttpStatusCode == System.Net.HttpStatusCode.Forbidden) {
                        //        BookmarkSyncResultInfoBar.Message = "InfoBar_Error_Unauthorized_Message".GetLocalized(SUBTREE_NAME);
                        //    } else {
                        //        BookmarkSyncResultInfoBar.Message = "InfoBar_Error_Unknown_Message".GetLocalized(SUBTREE_NAME);
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
            if (RadioButtons1SelectedIndex == -1) {
                return false;
            }
            // Upload option selected
            else if (RadioButtons1SelectedIndex == 0) {
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
                        RadioButtons2SelectedIndex != -1 &&
                        (RadioButtons2SelectedIndex == 0 || RadioButtons3SelectedIndex != -1);
                }
                if (IsGalleryOptionChecked) {
                    enable &= RadioButtons4SelectedIndex != -1;
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
