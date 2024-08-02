﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Hitomi_Scroll_Viewer.Entities
{
    internal class TagFilter
    {
        public long Id { get; set; }
        public static readonly string[] CATEGORIES = [
            "language", "female", "male", "artist", "character", "group", "series", "type", "tag"
        ];
        public static readonly Dictionary<string, int> CATEGORY_INDEX_MAP = 
            CATEGORIES
            .Select((category, i) => new KeyValuePair<string, int>(category, i))
            .ToDictionary();

        [MaxLength(9)]
        public string Category { get; set; }
        public IEnumerable<string> Tags { get; set; }
    }
}