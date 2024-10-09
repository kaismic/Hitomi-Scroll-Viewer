using HitomiScrollViewerLib.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using static HitomiScrollViewerLib.Constants;

namespace HitomiScrollViewerLib.DbContexts {
    public class HitomiContext : DbContext {
        public DbSet<TagFilter> TagFilters { get; set; }
        public DbSet<Gallery> Galleries { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<GalleryLanguage> GalleryLanguages { get; set; }
        public DbSet<GalleryTypeEntity> GalleryTypes { get; set; }
        public DbSet<QueryConfiguration> QueryConfigurations { get; set; }
        public DbSet<SortDirectionEntity> SortDirections { get; set; }
        public DbSet<GallerySortEntity> GallerySorts { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            // db file storage location = Windows.Storage.ApplicationData.Current.LocalFolder.Path
            optionsBuilder
                .UseSqlite($"Data Source={MAIN_DATABASE_PATH_V3}")
                .EnableSensitiveDataLogging();
        }

        /// <returns><see cref="Tag"/> or <c>null</c></returns>
        public Tag GetTag(string value, TagCategory category) {
            string formattedValue = value.ToLower();
            return Tags
                .FirstOrDefault(tag =>
                    tag.Value == formattedValue &&
                    tag.Category == category
                );
        }
    }
}