using HitomiScrollViewerLib.Entities;
using Microsoft.EntityFrameworkCore;
using static HitomiScrollViewerLib.Utils;

namespace HitomiScrollViewerLib.DbContexts {
    internal class GalleryContext : DbContext {
        public DbSet<Gallery> Galleries { get; set; }

        private static GalleryContext _mainContext;
        public static GalleryContext MainContext {
            get => _mainContext ??= new GalleryContext();
        }

        private GalleryContext() : base() { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            // db file storage location = Windows.Storage.ApplicationData.Current.LocalFolder.Path
            optionsBuilder.UseSqlite($"Data Source={GALLERIES_DATABASE_NAME_V3}");
        }

        public void Init() {
            Database.EnsureCreated();
            Galleries.Load(); // TODO test if this is needed or where else I can put it
        }
    }
}
