using HitomiScrollViewerData.Entities;
using System.Text.RegularExpressions;

namespace HitomiScrollViewerAPI.Utils {
    public static partial class GalleryFileUtil {
        private const string ROOT_PATH = "Galleries";
        [GeneratedRegex(@"\d+")] private static partial Regex AllDigitRegex();

        public static IEnumerable<GalleryImage> GetMissingImages(int galleryId, IEnumerable<GalleryImage> galleryImages) {
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
            Console.WriteLine($"{galleryId}: existing indexes: {string.Join(',', existingIndexes)}");
            return galleryImages.Where(gi => !existingIndexes.Contains(gi.Index));
        }

        public static string GetImagePath(Gallery gallery, GalleryImage galleryImage, string fileExt) {
            return Path.Combine(ROOT_PATH, gallery.Id.ToString(), GetFullFileName(gallery, galleryImage, fileExt));
        }

        public static async Task WriteImageAsync(Gallery gallery, GalleryImage galleryImage, byte[] data, string fileExt) {
            string fullFileName = GetFullFileName(gallery, galleryImage, fileExt);
            string dir = Path.Combine(ROOT_PATH, gallery.Id.ToString());
            Directory.CreateDirectory(dir);
            await File.WriteAllBytesAsync(Path.Combine(dir, fullFileName), data);
        }

        private static string GetFullFileName(Gallery gallery, GalleryImage galleryImage, string fileExt) {
            string format = "D" + Math.Floor(Math.Log10(gallery.GalleryImages.Count) + 1);
            string fileName = galleryImage.Index.ToString(format);
            return fileName + '.' + fileExt;
        }
    }
}
