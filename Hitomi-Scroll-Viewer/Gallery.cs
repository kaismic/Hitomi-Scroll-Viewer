using System.Collections.Generic;

namespace Hitomi_Scroll_Viewer {
    public class Gallery {
        public string type;
        public string language_localname;
        public string videofilename;
        public string language;
        public string id;
        public ImageInfo[] files;
        public int[] related;
        public string title;
        public string japanese_title;
        public Dictionary<string, string>[] groups;
        public string language_url;
        public int[] scene_indexes;
        public Dictionary<string, string>[] artists;
        public Dictionary<string, string>[] languages;
        public object video;
        public Dictionary<string, string>[] characters;
        public string date;
        public Dictionary<string, string>[] parodys;
        public Dictionary<string, object>[] tags;

        public Gallery() {}

        public string GetArtists() {
            if (artists == null) {
                return null;
            }
            string result = "";
            for (int i = 0; i < artists.Length; i++) {
                result += artists[i]["artist"] + ", ";
            }
            return result[..^", ".Length];
        }
    }

    public class ImageInfo {
        public string name;
        public int height;
        public int width;
        public int haswebp;
        public int hasavif;
        public string hash;

        public ImageInfo() {}
    }
}
