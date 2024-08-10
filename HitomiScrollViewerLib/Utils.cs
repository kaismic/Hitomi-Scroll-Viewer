using HitomiScrollViewerLib.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

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

        public static readonly string TAG_FILTER_SETS_DATABASE_NAME_V3 = "tag_filter_sets.db";
        public static readonly string TAG_FILTER_SETS_DATABASE_PATH_V3 = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, TAG_FILTER_SETS_DATABASE_NAME_V3);
        public static readonly string GALLERIES_DATABASE_NAME_V3 = "galleries.db";
        public static readonly string GALLERIES_DATABASE_PATH_V3 = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, GALLERIES_DATABASE_NAME_V3);

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
    }
}