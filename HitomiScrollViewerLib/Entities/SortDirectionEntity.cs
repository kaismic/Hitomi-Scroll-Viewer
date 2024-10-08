using CommunityToolkit.WinUI.Collections;
using HitomiScrollViewerLib.DbContexts;
using Microsoft.Windows.ApplicationModel.Resources;
using System.ComponentModel.DataAnnotations;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Entities {
    public class SortDirectionEntity {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(SortDirection).Name);
        public static readonly SortDirectionEntity[] SORT_DIRECTIONS = GetSortDirectionEntities();

        private static SortDirectionEntity[] GetSortDirectionEntities() {
            using HitomiContext context = new();
            return [.. context.SortDirections];
        }

        [Key]
        public SortDirection SortDirection { get; init; }
        public string DisplayName => _resourceMap.GetValue(SortDirection.ToString()).ValueAsString;
    }
}
