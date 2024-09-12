﻿using System.Collections.Generic;
using System.Linq;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Entities {
    internal class LegacyTagFilter {
        private static readonly Dictionary<string, TagCategory> INV_CATEGORY_PROP_KEY_DICT = new() {
            { "tag", TagCategory.Tag },
            { "male", TagCategory.Male },
            { "female", TagCategory.Female },
            { "artist", TagCategory.Artist },
            { "group", TagCategory.Group },
            { "character", TagCategory.Character },
            { "series", TagCategory.Series }
        };
        private static readonly string[] TAGS_WITH_UNDERSCORES = [
            "bna_v5",
            "dai_quoc",
            "dude_dude_dude!",
            "fisticuffs_club",
            "h_artistlife",
            "h_earth",
            "jeith_h3ntai",
            "kan_suizoku_kan",
            "mashi ~yugure] feito rogu ㊸_ edo guda ♀](fate/grand order",
            "sōshoku tora _ sō shoku tora",
            "tokyo_tsunamushi_land"
        ];
        private static readonly string[] TAGS_WITH_UNDERSCORES_SPACES_REPLACED =
            TAGS_WITH_UNDERSCORES.Select(tag => tag.Replace(' ', '_')).ToArray();

        public Dictionary<string, IEnumerable<string>> IncludeTags { get; set; }
        public Dictionary<string, IEnumerable<string>> ExcludeTags { get; set; }


        private static void ConvertTagValues(List<string> tagValues, List<Tag> tags, TagCategory category) {
            tags.AddRange(
                tagValues.Select(
                    tagValue => {
                        // V2 tags values were stored by replacing space (' ') characters with underscores ('_')
                        // but V3 stores the tag values in original value without the replacement.
                        Tag tag = Tag.GetTag(tagValue.Replace('_', ' '), category);
                        if (tag != null) {
                            return tag;
                        }
                        // It is very unlikely to reach this point and reaching this point means that the tag contains underscores
                        // And only 11 tags contain underscores which are in TAGS_WITH_UNDERSCORES
                        // so just iterate through it and check
                        for (int i = 0; i < TAGS_WITH_UNDERSCORES_SPACES_REPLACED.Length; i++) {
                            if (TAGS_WITH_UNDERSCORES_SPACES_REPLACED[i] == tagValue) {
                                return Tag.GetTag(TAGS_WITH_UNDERSCORES[i], category);
                            }
                        }
                        // At this point, it means the user had created and stored a custom tag value in V2
                        // so just create a tag with that value and return it
                        return tag ??= Tag.CreateTag(tagValue, category);
                    }
                )
            );
        }

        internal List<TagFilter> ToTagFilter(string name) {
            List<TagFilter> result = [];

            bool hasAnyTags = false;
            List<Tag> includeTFSTags = [];
            foreach (var kvp in IncludeTags) {
                if (kvp.Key == "language" || kvp.Key == "type") {
                    continue;
                }
                List<string> tagValues = kvp.Value.ToList();
                hasAnyTags |= tagValues.Count != 0;
                ConvertTagValues(tagValues, includeTFSTags, INV_CATEGORY_PROP_KEY_DICT[kvp.Key]);
            }
            if (hasAnyTags) {
                result.Add(
                    new TagFilter() {
                        Name = name + " - " + TEXT_INCLUDE,
                        Tags = includeTFSTags
                    }
                );
            }

            hasAnyTags = false;
            List<Tag> excludeTFSTags = [];
            foreach (var kvp in IncludeTags) {
                if (kvp.Key == "language" || kvp.Key == "type") {
                    continue;
                }
                List<string> tagValues = kvp.Value.ToList();
                hasAnyTags |= tagValues.Count != 0;
                ConvertTagValues(tagValues, excludeTFSTags, INV_CATEGORY_PROP_KEY_DICT[kvp.Key]);
            }
            if (hasAnyTags) {
                result.Add(
                    new TagFilter() {
                        Name = name + " - " + TEXT_INCLUDE,
                        Tags = excludeTFSTags
                    }
                );
            }

            return result;
        }
    }

}