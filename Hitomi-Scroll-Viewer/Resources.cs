using Microsoft.Windows.ApplicationModel.Resources;

namespace Hitomi_Scroll_Viewer {
    internal class Resources {
        internal static readonly ResourceMap MainResourceMap = new ResourceManager().MainResourceMap;
        private static readonly ResourceMap ResourceMap = MainResourceMap.GetSubtree("Resources");
        internal static readonly string DIALOG_BUTTON_TEXT_YES = ResourceMap.GetValue("DialogButtonText_Yes").ValueAsString;
        internal static readonly string DIALOG_BUTTON_TEXT_CANCEL = ResourceMap.GetValue("DialogButtonText_Cancel").ValueAsString;
        internal static readonly string DIALOG_BUTTON_TEXT_EXIT = ResourceMap.GetValue("DialogButtonText_Exit").ValueAsString;
    }
}
