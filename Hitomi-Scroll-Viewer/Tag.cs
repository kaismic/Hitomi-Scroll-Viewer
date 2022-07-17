using System.Collections.Generic;

namespace Hitomi_Scroll_Viewer {
    internal class Tag {
        public Dictionary<string, string[]> includeTagTypes = new();
        public Dictionary<string, string[]> excludeTagTypes = new();
        public Tag() {

        }
        public Tag(string[] tagTypes) {
            foreach (string tag in tagTypes) {
                includeTagTypes[tag] = new string[] { "" };
                excludeTagTypes[tag] = new string[] { "" };
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

