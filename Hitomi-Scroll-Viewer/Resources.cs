using Microsoft.Windows.ApplicationModel.Resources;

namespace Hitomi_Scroll_Viewer {
    internal class Resources {
        internal static readonly ResourceMap MainResourceMap = new ResourceManager().MainResourceMap;
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree("Resources");

        internal static readonly string APP_DISPLAY_NAME = _resourceMap.GetValue("AppDisplayName").ValueAsString;
        
        internal static readonly string TEXT_YES = _resourceMap.GetValue("Text_Yes").ValueAsString;
        internal static readonly string TEXT_NO = _resourceMap.GetValue("Text_No").ValueAsString;
        internal static readonly string TEXT_CANCEL = _resourceMap.GetValue("Text_Cancel").ValueAsString;
        internal static readonly string TEXT_EXIT = _resourceMap.GetValue("Text_Exit").ValueAsString;
        internal static readonly string TEXT_CLOSE = _resourceMap.GetValue("Text_Close").ValueAsString;
        internal static readonly string TEXT_ERROR = _resourceMap.GetValue("Text_Error").ValueAsString;
        internal static readonly string TEXT_TAG_FILTERS = _resourceMap.GetValue("Text_TagFilters").ValueAsString;
        internal static readonly string TEXT_BOOKMARKS = _resourceMap.GetValue("Text_Bookmarks").ValueAsString;
        internal static readonly string TEXT_PAGE = _resourceMap.GetValue("Text_Page").ValueAsString;

        internal static readonly string EXAMPLE_TAG_FILTER_NAME_1 = _resourceMap.GetValue("ExampleTagFilterSet_1").ValueAsString;
        internal static readonly string EXAMPLE_TAG_FILTER_NAME_2 = _resourceMap.GetValue("ExampleTagFilterSet_2").ValueAsString;
        internal static readonly string EXAMPLE_TAG_FILTER_NAME_3 = _resourceMap.GetValue("ExampleTagFilterSet_3").ValueAsString;
        internal static readonly string EXAMPLE_TAG_FILTER_NAME_4 = _resourceMap.GetValue("ExampleTagFilterSet_4").ValueAsString;
    }
}
