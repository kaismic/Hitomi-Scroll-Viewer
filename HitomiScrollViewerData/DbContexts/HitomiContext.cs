using HitomiScrollViewerData.Entities;
using Microsoft.EntityFrameworkCore;

namespace HitomiScrollViewerData.DbContexts {
    public class HitomiContext : DbContext {
        public static readonly string MAIN_DATABASE_PATH = "main.db";
        public HitomiContext() { }

        public DbSet<TagFilter> TagFilters { get; set; }
        public DbSet<Gallery> Galleries { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<GalleryLanguage> GalleryLanguages { get; set; }
        public DbSet<GalleryType> GalleryTypes { get; set; }
        public DbSet<QueryConfiguration> QueryConfigurations { get; set; }
        public DbSet<GallerySort> GallerySorts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            optionsBuilder
                .UseSqlite($"Data Source={MAIN_DATABASE_PATH}")
                .EnableSensitiveDataLogging();
        }
    }
}