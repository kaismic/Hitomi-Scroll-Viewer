using HitomiScrollViewerData.Entities;
using System.Text.RegularExpressions;

namespace HitomiScrollViewerAPI.Utils {
    public static partial class GalleryFileUtil {
        private const string ROOT_PATH = "Galleries";
        [GeneratedRegex(@"\d+")] private static partial Regex AllDigitRegex();

        public static IEnumerable<GalleryImage> GetMissingFiles(int galleryId, IEnumerable<GalleryImage> galleryImages) {
            string dir = Path.Combine(ROOT_PATH, galleryId.ToString());
            if (!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
                return galleryImages;
            }
            Regex reg = AllDigitRegex();
            HashSet<int> existingIndexes =
                [.. Directory.GetFiles(dir, "*.*")
                .Select(Path.GetFileName)
                .Cast<string>()
                .Select(f => f.Split('.')[0])
                .Where(f => reg.IsMatch(f))
                .Select(int.Parse)];
            return galleryImages.Where(gi => !existingIndexes.Contains(gi.Index));
        }

        public static string GetImagePath(Gallery gallery, GalleryImage galleryImage) {
            return Path.Combine(ROOT_PATH, gallery.Id.ToString(), GetFullFileName(gallery, galleryImage));
        }

        public static async Task WriteImageAsync(Gallery gallery, GalleryImage galleryImage, byte[] data) {
            string fullFileName = GetFullFileName(gallery, galleryImage);
            string dir = Path.Combine(ROOT_PATH, gallery.Id.ToString());
            Directory.CreateDirectory(dir);
            await File.WriteAllBytesAsync(Path.Combine(dir, fullFileName), data);
        }

        private static string GetFullFileName(Gallery gallery, GalleryImage galleryImage) {
            string format = "D" + Math.Floor(Math.Log10(gallery.GalleryImages.Count) + 1);
            string fileName = galleryImage.Index.ToString(format);
            return fileName + '.' + galleryImage.FileExt;
        }
    }
}
