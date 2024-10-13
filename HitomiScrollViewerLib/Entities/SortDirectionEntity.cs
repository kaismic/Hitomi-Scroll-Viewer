using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Collections;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HitomiScrollViewerLib.Entities {
    public class SortDirectionEntity {
        private static readonly string SUBTREE_NAME = typeof(SortDirection).Name;

        public override bool Equals(object obj) {
            return obj is SortDirectionEntity entity &&
                   SortDirection == entity.SortDirection;
        }

        public override int GetHashCode() {
            return HashCode.Combine(SortDirection);
        }

        public static bool operator == (SortDirectionEntity a, SortDirectionEntity b) {
            if (a is null) return b is null;
            return a.Equals(b);
        }
        public static bool operator != (SortDirectionEntity a, SortDirectionEntity b) {
            if (a is null) return b is not null;
            return !a.Equals(b);
        }

        [Key]
        public SortDirection SortDirection { get; init; }
        public string DisplayName => SortDirection.ToString().GetLocalized(SUBTREE_NAME);
        public HashSet<GallerySortEntity> GallerySorts { get; } = [];
    }
}
