using System;
using System.IO;
using System.Text.Json;
using Windows.Storage;

namespace HitomiScrollViewerLib {
    public static class Constants {
        public const string IMAGE_DIR_NAME = "images";
        public const string ROOT_DIR_NAME_V2 = "HSV";
        public static readonly string ROOT_DIR_V2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ROOT_DIR_NAME_V2);
        public static readonly string NON_VIRTUAL_ROOT_DIR_V2 = Path.Combine(ApplicationData.Current.LocalCacheFolder.Path, new DirectoryInfo(ROOT_DIR_V2).Name, ROOT_DIR_NAME_V2);
        public static readonly string IMAGE_DIR_V2 = Path.Combine(ROOT_DIR_V2, IMAGE_DIR_NAME);
        public static readonly string NON_VIRTUAL_IMAGE_DIR_V2 = Path.Combine(NON_VIRTUAL_ROOT_DIR_V2, IMAGE_DIR_NAME);

        public const string TAG_FILTERS_FILE_NAME_V2 = "tag_filters.json";
        public static readonly string TAG_FILTERS_FILE_PATH_V2 = Path.Combine(ROOT_DIR_V2, TAG_FILTERS_FILE_NAME_V2);
        public const string BOOKMARKS_FILE_NAME_V2 = "bookmarks.json";
        public static readonly string BOOKMARKS_FILE_PATH_V2 = Path.Combine(ROOT_DIR_V2, BOOKMARKS_FILE_NAME_V2);


        public static readonly string LOCAL_DIR_V3 = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        public static readonly string ROAMING_DIR_V3 = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        public static readonly string NON_VIRTUAL_LOCAL_DIR_V3 = Path.Combine(ApplicationData.Current.LocalCacheFolder.Path, new DirectoryInfo(LOCAL_DIR_V3).Name);
        public static readonly string IMAGE_DIR_V3 = Path.Combine(LOCAL_DIR_V3, IMAGE_DIR_NAME);
        public static readonly string NON_VIRTUAL_IMAGE_DIR_V3 = Path.Combine(NON_VIRTUAL_LOCAL_DIR_V3, IMAGE_DIR_NAME);

        public static readonly string MAIN_DATABASE_PATH_V3 = Path.Combine(ApplicationData.Current.LocalFolder.Path, "main.db");
        public static readonly string USER_DATABASE_PATH_V3 = Path.Combine(ApplicationData.Current.LocalFolder.Path, "user_data.db");

        public static readonly string TFS_SYNC_FILE_PATH = Path.Combine(ApplicationData.Current.LocalFolder.Path, "tag_filter_sets.json");
        public static readonly string GALLERIES_SYNC_FILE_PATH = Path.Combine(ApplicationData.Current.LocalFolder.Path, "galleries.json");

        public static readonly JsonSerializerOptions GALLERY_SERIALIZER_OPTIONS = new(JsonSerializerDefaults.Web) {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
        public static readonly JsonSerializerOptions TF_SERIALIZER_OPTIONS = new(JsonSerializerDefaults.Web);

        public const string GLYPH_REPEAT_ALL = "\xE8EE";
        public const string GLYPH_CANCEL = "\xE711";
        public const string GLYPH_PLAY = "\xE768";
        public const string GLYPH_PAUSE = "\xE769";
    }
}