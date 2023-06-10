using System.Collections.Generic;

namespace Hitomi_Scroll_Viewer {
    public class Gallery {
        public string type = "";
        public string language_localname = "";
        public string videofilename = "";
        public string language = "";
        public string id = "";
        public List<ImageInfo> files = new();
        public List<int> related = new();
        public string title = "";
        public string japanese_title = "";
        public List<Dictionary<string, string>> groups = new();
        public string language_url = "";
        public List<int> scene_indexes = new();
        public List<Dictionary<string, string>> artists = new();
        public List<Dictionary<string, string>> languages = new();
        public object video = null;
        public List<Dictionary<string, string>> characters = new();
        public string date = "";
        public List<Dictionary<string, string>> parodys = new();
        public List<Dictionary<string, object>> tags = new();

        public Gallery() {

        }
    }

    public class ImageInfo {
        public string name = "";
        public int height = 0;
        public int width = 0;
        public int haswebp = 1;
        public int hasavif = 1;
        public string hash = "";

        public ImageInfo() {

        }
    }
}
