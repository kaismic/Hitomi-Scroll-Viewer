using Google.Apis.Drive.v3;
using Google.Apis.Upload;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using Google.Apis.Download;

namespace HitomiScrollViewerLib {
    public static class Utils {
        public static readonly string IMAGE_DIR_NAME = "images";
        public static readonly string ROOT_DIR_NAME_V2 = "HSV";
        public static readonly string ROOT_DIR_V2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ROOT_DIR_NAME_V2);
        public static readonly string NON_VIRTUAL_ROOT_DIR_V2 = Path.Combine(Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path, new DirectoryInfo(ROOT_DIR_V2).Name, ROOT_DIR_NAME_V2);
        public static readonly string IMAGE_DIR_V2 = Path.Combine(ROOT_DIR_V2, IMAGE_DIR_NAME);
        public static readonly string NON_VIRTUAL_IMAGE_DIR_V2 = Path.Combine(NON_VIRTUAL_ROOT_DIR_V2, IMAGE_DIR_NAME);
        public static readonly string LOGS_PATH_V2 = Path.Combine(ROOT_DIR_V2, "logs.txt");

        public static readonly string TAG_FILTERS_FILE_NAME_V2 = "tag_filters.json";
        public static readonly string TAG_FILTERS_FILE_PATH_V2 = Path.Combine(ROOT_DIR_V2, TAG_FILTERS_FILE_NAME_V2);
        public static readonly string BOOKMARKS_FILE_NAME_V2 = "bookmarks.json";
        public static readonly string BOOKMARKS_FILE_PATH_V2 = Path.Combine(ROOT_DIR_V2, BOOKMARKS_FILE_NAME_V2);


        public static readonly string ROOT_DIR_V3 = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        public static readonly string NON_VIRTUAL_ROOT_DIR_V3 = Path.Combine(Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path, new DirectoryInfo(ROOT_DIR_V3).Name);
        public static readonly string IMAGE_DIR_V3 = Path.Combine(ROOT_DIR_V3, IMAGE_DIR_NAME);
        public static readonly string NON_VIRTUAL_IMAGE_DIR_V3 = Path.Combine(NON_VIRTUAL_ROOT_DIR_V3, IMAGE_DIR_NAME);
        public static readonly string LOGS_PATH_V3 = Path.Combine(ROOT_DIR_V3, "logs.txt");

        public static readonly string TFS_MAIN_DATABASE_PATH_V3 = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "tag_filter_sets.db");
        public static readonly string TFS_TEMP_DATABASE_PATH_V3 = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "tag_filter_sets_temp.db");

        public static readonly string GALLERIES_MAIN_DATABASE_PATH_V3 = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "galleries.db");
        public static readonly string GALLERIES_TEMP_DATABASE_PATH_V3 = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "galleries_temp.db");

        public static readonly string DB_MIME_TYPE = "application/vnd.sqlite3";

        public enum UserDataType {
            TagFilterSet, Gallery
        }
        /*
         * apparently I can't just use Environment.NewLine as separator
         * because of this TextBox bug which somehow converts \r\n to \r and it's still not fixed...
         * https://github.com/microsoft/microsoft-ui-xaml/issues/1826
         * https://stackoverflow.com/questions/35138047/uwp-textbox-selectedtext-changes-r-n-to-r
        */
        public static readonly string[] NEW_LINE_SEPS = [Environment.NewLine, "\r"];
        public static readonly JsonSerializerOptions DEFAULT_SERIALIZER_OPTIONS = new(JsonSerializerDefaults.Web) {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
        public static readonly JsonSerializerOptions SERIALIZER_OPTIONS_V2 = new() {
            IncludeFields = true
        };
        public static readonly StringSplitOptions DEFAULT_STR_SPLIT_OPTIONS = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;

        public static string GetExceptionDetails(Exception e) {
            string output = "";
            string stacktrace = e.StackTrace ?? "";
            output += $"  {e.GetType().Name}: {e.Message}," + Environment.NewLine;
            while (e.InnerException != null) {
                e = e.InnerException;
                output += $"  {e.GetType().Name}: {e.Message}," + Environment.NewLine + ",";
            }
            output += stacktrace;

            return output;
        }

        public static FilesResource.CreateMediaUpload GetCreateMediaUpload(
            DriveService driveService,
            FileStream uploadStream,
            string fileName,
            string contentType
        ) {
            Google.Apis.Drive.v3.Data.File fileMetaData = new() {
                Name = fileName,
                Parents = ["appDataFolder"]
            };
            return driveService.Files.Create(
                fileMetaData,
                uploadStream,
                contentType
            );
        }

        public static FilesResource.UpdateMediaUpload GetUpdateMediaUpload(
            DriveService driveService,
            FileStream uploadStream,
            string fileId,
            string contentType
        ) {
            return driveService.Files.Update(
                new(),
                fileId,
                uploadStream,
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
    }
}