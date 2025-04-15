using HitomiScrollViewerData;

namespace HitomiScrollViewerWebApp {
    public static class Utils {
        public static string GetImageUrl(string baseUrl, int index, int fileExtIndex) =>
             $"{baseUrl}&index={index}&fileExt={Constants.IMAGE_FILE_EXTS[fileExtIndex]}";
    }
}
