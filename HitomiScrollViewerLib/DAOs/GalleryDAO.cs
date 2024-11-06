using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using System.Collections.Generic;
using System.IO;

namespace HitomiScrollViewerLib.DAOs {
    public class GalleryDAO {
        public static void RemoveRange(IEnumerable<Gallery> galleries) {
            using HitomiContext context = new();
            context.Galleries.RemoveRange(galleries);
            foreach (Gallery gallery in galleries) {
                if (Directory.Exists(gallery.ImageFilesDirectory)) {
                    Directory.Delete(gallery.ImageFilesDirectory, true);
                }
            }
            context.SaveChanges();
        }
    }
}
