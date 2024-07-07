using Microsoft.Windows.ApplicationModel.Resources;

namespace Hitomi_Scroll_Viewer {
    internal class Resources {
        internal static readonly ResourceMap MainResourceMap = new ResourceManager().MainResourceMap;
        private static readonly ResourceMap ResourceMap = MainResourceMap.GetSubtree("Resources");

        internal static readonly string APP_DISPLAY_NAME = ResourceMap.GetValue("AppDisplayName").ValueAsString;
        
        internal static readonly string DIALOG_BUTTON_TEXT_YES = ResourceMap.GetValue("DialogButtonText_Yes").ValueAsString;
        internal static readonly string DIALOG_BUTTON_TEXT_CANCEL = ResourceMap.GetValue("DialogButtonText_Cancel").ValueAsString;
        internal static readonly string DIALOG_BUTTON_TEXT_EXIT = ResourceMap.GetValue("DialogButtonText_Exit").ValueAsString;
        internal static readonly string DIALOG_BUTTON_TEXT_CLOSE = ResourceMap.GetValue("DialogButtonText_Close").ValueAsString;

        internal static readonly string EXAMPLE_TAG_FILTER_NAME_1 = ResourceMap.GetValue("ExampleTagFilterName_1").ValueAsString;
        internal static readonly string EXAMPLE_TAG_FILTER_NAME_2 = ResourceMap.GetValue("ExampleTagFilterName_2").ValueAsString;
        internal static readonly string EXAMPLE_TAG_FILTER_NAME_3 = ResourceMap.GetValue("ExampleTagFilterName_3").ValueAsString;
        internal static readonly string EXAMPLE_TAG_FILTER_NAME_4 = ResourceMap.GetValue("ExampleTagFilterName_4").ValueAsString;
    }
}
