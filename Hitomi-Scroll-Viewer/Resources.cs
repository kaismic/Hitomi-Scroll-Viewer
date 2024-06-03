using Microsoft.Windows.ApplicationModel.Resources;

namespace Hitomi_Scroll_Viewer {
    internal class Resources {
        internal static readonly ResourceMap MainResourceMap = new ResourceManager().MainResourceMap;
        private static readonly ResourceMap ResourceMap = MainResourceMap.GetSubtree("Resources");
        internal static readonly string DIALOG_TEXT_YES = ResourceMap.GetValue("DialogText_Yes").ValueAsString;
        internal static readonly string DIALOG_TEXT_CANCEL = ResourceMap.GetValue("DialogText_Cancel").ValueAsString;
    }
}
