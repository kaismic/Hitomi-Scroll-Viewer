using Microsoft.UI.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HitomiScrollViewerLib.Views.PageViews.ViewPage;

namespace HitomiScrollViewerLib.ViewModels.PageVMs {
    public class ViewPageVM : IAppWindowClosingHandler {
        private static ViewPageVM _main;
        public static ViewPageVM Main => _main ??= new();

        public void HandleAppWindowClosing(AppWindowClosingEventArgs args) {
            //// TODO save settings indivisually when they are changed
            //ToggleAutoScroll(false);
            //_settings.Values[SCROLL_DIRECTION_SETTING_KEY] = (int)_scrollDirection;
            //_settings.Values[VIEW_DIRECTION_SETTING_KEY] = (int)_viewDirection;
            //_settings.Values[AUTO_SCROLL_INTERVAL_SETTING_KEY] = _autoScrollInterval;
            //_settings.Values[IS_LOOPING_SETTING_KEY] = LoopBtn.IsChecked;
            //_settings.Values[USE_PAGE_FLIP_EFFECT_SETTING_KEY] = ImageFlipView.UseTouchAnimationsForAllNavigation;
        }
    }
}
