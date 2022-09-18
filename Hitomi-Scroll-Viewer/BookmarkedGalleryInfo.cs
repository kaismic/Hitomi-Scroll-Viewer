using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Hitomi_Scroll_Viewer {
    public class BookmarkedGalleryInfo {
        [JsonInclude]
        public int Count { get; private set; } = 0;
        [JsonInclude]
        public List<string> Ids { get; private set; } = new();
        [JsonInclude]
        public List<string> Titles { get; private set; } = new();
        [JsonInclude]
        public List<double[]> ImgRatios { get; private set;  } = new();
        public BookmarkedGalleryInfo() {

        }

        public void RemoveBookmark(int idx) {
            Ids.RemoveAt(idx);
            Titles.RemoveAt(idx);
            ImgRatios.RemoveAt(idx);
            Count--;
        }

        public void AddBookmark(string id, string title, double[] imgRatio) {
            Ids.Add(id);
            Titles.Add(title);
            ImgRatios.Add(imgRatio);
            Count++;
        }

        public bool IdInList(string id) {
            return Ids.Contains(id);
        }
    }
}
