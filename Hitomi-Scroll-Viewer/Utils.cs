using System;
using System.IO;
using System.Text.Json;

namespace Hitomi_Scroll_Viewer {
    public static class Utils {
        public static readonly string ROOT_DIR = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HSV");
        public static readonly string IMAGE_DIR = Path.Combine(ROOT_DIR, "images");
        public static readonly string SETTINGS_PATH = Path.Combine(ROOT_DIR, "settings.json");
        public static readonly string LOGS_PATH = Path.Combine(ROOT_DIR, "logs.txt");
        /*
         * apparently I can't just use Environment.NewLine as separator
         * because of this TextBox bug which somehow converts \r\n to \r and it's still not fixed...
         * https://github.com/microsoft/microsoft-ui-xaml/issues/1826
         * https://stackoverflow.com/questions/35138047/uwp-textbox-selectedtext-changes-r-n-to-r
        */
        public static readonly string[] NEW_LINE_SEPS = [Environment.NewLine, "\r"];
        public static readonly JsonSerializerOptions serializerOptions = new() { IncludeFields = true, WriteIndented = true };
        public static readonly StringSplitOptions STR_SPLIT_OPTION = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;

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
            File.WriteAllText(path, JsonSerializer.Serialize(obj, serializerOptions));
        }
    }
}