using Microsoft.Windows.ApplicationModel.Resources;

namespace Hitomi_Scroll_Viewer {
    internal class Resources {
        private static readonly ResourceMap ResourceManager = new ResourceManager().MainResourceMap.GetSubtree("Resources");
        internal static readonly string DIALOG_TEXT_YES = ResourceManager.GetValue("DialogText_Yes").ValueAsString;
        internal static readonly string DIALOG_TEXT_CANCEL = ResourceManager.GetValue("DialogText_Cancel").ValueAsString;
    }
}
