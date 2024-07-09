using System;
using System.IO;
using System.Text.Json;

namespace Hitomi_Scroll_Viewer {
    public static class Utils {
        public static readonly string ROOT_DIR_NAME = "HSV";
        public static readonly string ROOT_DIR = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ROOT_DIR_NAME);
        public static readonly string NON_VIRTUAL_ROOT_DIR = Path.Combine(Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path, new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)).Name, ROOT_DIR_NAME);
        public static readonly string IMAGE_DIR_NAME = "images";
        public static readonly string IMAGE_DIR = Path.Combine(ROOT_DIR, IMAGE_DIR_NAME);
        public static readonly string NON_VIRTUAL_IMAGE_DIR = Path.Combine(NON_VIRTUAL_ROOT_DIR, IMAGE_DIR_NAME);
        public static readonly string SETTINGS_PATH = Path.Combine(ROOT_DIR, "settings.json");
        public static readonly string LOGS_PATH = Path.Combine(ROOT_DIR, "logs.txt");

        public static readonly string BOOKMARKS_FILE_NAME = "bookmarks.json";
        public static readonly string BOOKMARKS_FILE_PATH = Path.Combine(ROOT_DIR, BOOKMARKS_FILE_NAME);
        public static readonly string TAG_FILTERS_FILE_NAME = "tag_filters.json";
        public static readonly string TAG_FILTERS_FILE_PATH = Path.Combine(ROOT_DIR, TAG_FILTERS_FILE_NAME);

        /*
         * apparently I can't just use Environment.NewLine as separator
         * because of this TextBox bug which somehow converts \r\n to \r and it's still not fixed...
         * https://github.com/microsoft/microsoft-ui-xaml/issues/1826
         * https://stackoverflow.com/questions/35138047/uwp-textbox-selectedtext-changes-r-n-to-r
        */
        public static readonly string[] NEW_LINE_SEPS = [Environment.NewLine, "\r"];
        public static readonly JsonSerializerOptions DEFAULT_SERIALIZER_OPTIONS = new() { IncludeFields = true, WriteIndented = true };
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

        public static void WriteObjectToJson(string path, object obj) {
            File.WriteAllText(path, JsonSerializer.Serialize(obj, DEFAULT_SERIALIZER_OPTIONS));
        }
    }
}