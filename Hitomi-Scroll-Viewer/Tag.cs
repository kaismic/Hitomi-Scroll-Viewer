using System.Collections.Generic;

namespace Hitomi_Scroll_Viewer {
    public class Tag {
        public static readonly string[] CATEGORIES = {
            "language", "female", "male", "artist", "character", "group", "series", "type", "tag"
        };

        public Dictionary<string, string[]> includeTags = new();
        public Dictionary<string, string[]> excludeTags = new();
        public Tag() {
            foreach (string tag in CATEGORIES) {
                includeTags[tag] = System.Array.Empty<string>();
                excludeTags[tag] = System.Array.Empty<string>();
            }
        }
    }
}

/*
 {
    tag1: {
            includeTagTypes: {
                language: [sdr ,wf ,4 ga, fr ,apr fp]
                female;.
                male;
                artist;...
                character;
                series;...
                type;
                tag;.....


                },
            excludeTagTypes: {
                language: [sdr ,wf ,4 ga, fr ,apr fp]
                female;...
                male;
                artist;
                character;
                series;
                type;
                tag;.......
                }
           },
    tag2: {
            includeTagTypes: {
                language: [sdr ,wf ,4 ga, fr ,apr fp]
                female;.
                male;
                artist;...
                character;
                series;...
                type;
                tag.....
                },
            excludeTagTypes: {
                language: [sdr ,wf ,4 ga, fr ,apr fp]
                female;...
                male;
                artist;
                character;
                series;
                type;
                tag;.......
                }
           }
    }  
 */

