using Microsoft.Windows.ApplicationModel.Resources;

namespace HitomiScrollViewerLib {
    public class SharedResources {
        public static readonly ResourceMap MainResourceMap = new ResourceManager().MainResourceMap;
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(SharedResources).Name);

        public static readonly string APP_DISPLAY_NAME = _resourceMap.GetValue("AppDisplayName").ValueAsString;
        
        public static readonly string TEXT_YES = _resourceMap.GetValue("Text_Yes").ValueAsString;
        public static readonly string TEXT_NO = _resourceMap.GetValue("Text_No").ValueAsString;
        public static readonly string TEXT_CANCEL = _resourceMap.GetValue("Text_Cancel").ValueAsString;
        public static readonly string TEXT_EXIT = _resourceMap.GetValue("Text_Exit").ValueAsString;
        public static readonly string TEXT_CLOSE = _resourceMap.GetValue("Text_Close").ValueAsString;
        public static readonly string TEXT_ERROR = _resourceMap.GetValue("Text_Error").ValueAsString;
        public static readonly string TEXT_TAG_FILTERS = _resourceMap.GetValue("Text_TagFilters").ValueAsString;
        public static readonly string TEXT_GALLERIES = _resourceMap.GetValue("Text_Galleries").ValueAsString;
        public static readonly string TEXT_PAGE = _resourceMap.GetValue("Text_Page").ValueAsString;
        public static readonly string TEXT_INCLUDE = _resourceMap.GetValue("Text_Include").ValueAsString;
        public static readonly string TEXT_EXCLUDE = _resourceMap.GetValue("Text_Exclude").ValueAsString;
    }
}
