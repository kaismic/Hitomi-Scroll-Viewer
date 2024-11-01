using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Collections;

namespace HitomiScrollViewerLib.Entities {
    public class SortDirectionEntity {
        private static readonly string SUBTREE_NAME = typeof(SortDirection).Name;
        public int Id { get; private set; }
        public SortDirection SortDirection { get; init; }
        public string DisplayName => SortDirection.ToString().GetLocalized(SUBTREE_NAME);
    }
}
