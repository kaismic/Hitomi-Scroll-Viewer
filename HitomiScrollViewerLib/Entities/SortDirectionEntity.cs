using CommunityToolkit.WinUI.Collections;
using HitomiScrollViewerLib.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Entities {
    public class SortDirectionEntity {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(SortDirection).Name);

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
        public string DisplayName => _resourceMap.GetValue(SortDirection.ToString()).ValueAsString;
        public HashSet<GallerySortEntity> GallerySorts { get; } = [];
    }
}
