using Microsoft.UI.Windowing;
using System.Collections.Generic;

namespace HitomiScrollViewerLib {
    public interface IAppWindowClosingHandler {
        void HandleAppWindowClosing(AppWindowClosingEventArgs args);
    }
}
