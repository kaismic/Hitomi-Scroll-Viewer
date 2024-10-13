using CommunityToolkit.WinUI;

namespace HitomiScrollViewerLib {
    public class SharedResources {
        private static readonly string SUBTREE_NAME = typeof(SharedResources).Name;

        public static readonly string APP_DISPLAY_NAME = "AppDisplayName".GetLocalized(SUBTREE_NAME);
        
        public static readonly string TEXT_YES = "Text_Yes".GetLocalized(SUBTREE_NAME);
        public static readonly string TEXT_NO = "Text_No".GetLocalized(SUBTREE_NAME);
        public static readonly string TEXT_CANCEL = "Text_Cancel".GetLocalized(SUBTREE_NAME);
        public static readonly string TEXT_EXIT = "Text_Exit".GetLocalized(SUBTREE_NAME);
        public static readonly string TEXT_CLOSE = "Text_Close".GetLocalized(SUBTREE_NAME);
        public static readonly string TEXT_ERROR = "Text_Error".GetLocalized(SUBTREE_NAME);
        public static readonly string TEXT_TAG_FILTERS = "Text_TagFilters".GetLocalized(SUBTREE_NAME);
        public static readonly string TEXT_GALLERIES = "Text_Galleries".GetLocalized(SUBTREE_NAME);
        public static readonly string TEXT_PAGE = "Text_Page".GetLocalized(SUBTREE_NAME);
        public static readonly string TEXT_INCLUDE = "Text_Include".GetLocalized(SUBTREE_NAME);
        public static readonly string TEXT_EXCLUDE = "Text_Exclude".GetLocalized(SUBTREE_NAME);
        public static readonly string TEXT_ALL = "Text_All".GetLocalized(SUBTREE_NAME);
        public static readonly string TEXT_LANGUAGE = "Text_Language".GetLocalized(SUBTREE_NAME);
        public static readonly string TEXT_TYPE = "Text_Type".GetLocalized(SUBTREE_NAME);
    }
}
