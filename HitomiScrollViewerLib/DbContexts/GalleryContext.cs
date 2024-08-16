using HitomiScrollViewerLib.Entities;
using Microsoft.EntityFrameworkCore;
using System.IO;
using static HitomiScrollViewerLib.Utils;

namespace HitomiScrollViewerLib.DbContexts {
    public class GalleryContext(string dbFileName) : DbContext {
        public DbSet<Gallery> Galleries { get; set; }

        private static GalleryContext _main;
        public static GalleryContext Main {
            get => _main ??= new GalleryContext(Path.GetFileName(GALLERIES_MAIN_DATABASE_PATH_V3));
            set => _main = value;
        }

        private readonly string _dbFileName = dbFileName;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            // db file storage location = Windows.Storage.ApplicationData.Current.LocalFolder.Path
            optionsBuilder.UseSqlite($"Data Source={_dbFileName};Pooling=False");
        }
    }
}
