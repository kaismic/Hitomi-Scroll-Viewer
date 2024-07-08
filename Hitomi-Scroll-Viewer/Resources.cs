using Microsoft.Windows.ApplicationModel.Resources;

namespace Hitomi_Scroll_Viewer {
    internal class Resources {
        internal static readonly ResourceMap MainResourceMap = new ResourceManager().MainResourceMap;
        private static readonly ResourceMap ResourceMap = MainResourceMap.GetSubtree("Resources");

        internal static readonly string APP_DISPLAY_NAME = ResourceMap.GetValue("AppDisplayName").ValueAsString;
        
        internal static readonly string TEXT_YES = ResourceMap.GetValue("Text_Yes").ValueAsString;
        internal static readonly string TEXT_NO = ResourceMap.GetValue("Text_No").ValueAsString;
        internal static readonly string TEXT_CANCEL = ResourceMap.GetValue("Text_Cancel").ValueAsString;
        internal static readonly string TEXT_EXIT = ResourceMap.GetValue("Text_Exit").ValueAsString;
        internal static readonly string TEXT_CLOSE = ResourceMap.GetValue("Text_Close").ValueAsString;

        internal static readonly string EXAMPLE_TAG_FILTER_NAME_1 = ResourceMap.GetValue("ExampleTagFilterName_1").ValueAsString;
        internal static readonly string EXAMPLE_TAG_FILTER_NAME_2 = ResourceMap.GetValue("ExampleTagFilterName_2").ValueAsString;
        internal static readonly string EXAMPLE_TAG_FILTER_NAME_3 = ResourceMap.GetValue("ExampleTagFilterName_3").ValueAsString;
        internal static readonly string EXAMPLE_TAG_FILTER_NAME_4 = ResourceMap.GetValue("ExampleTagFilterName_4").ValueAsString;
    }
}
