using HitomiScrollViewerLib.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace HitomiScrollViewerLib.DTOs {
    public class TagFilterSyncDTO {
        public required string Name { get; set; }
        public required IEnumerable<int> TagIds { get; set; }

        public TagFilter ToTagFilter(IQueryable<Tag> tags) {
            return new() {
                Name = Name,
                Tags = [.. TagIds.Select(id => tags.First(tag => tag.Id == id))]
            };
        }
    }
}