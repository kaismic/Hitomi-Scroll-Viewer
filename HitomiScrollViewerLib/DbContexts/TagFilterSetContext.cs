using HitomiScrollViewerLib.Entities;
using Microsoft.EntityFrameworkCore;
using static HitomiScrollViewerLib.Utils;

namespace HitomiScrollViewerLib.DbContexts {
    public class TagFilterSetContext : DbContext {
        public DbSet<TagFilterSet> TagFilterSets { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            // db file storage location = Windows.Storage.ApplicationData.Current.LocalFolder.Path
            optionsBuilder
                .UseSqlite($"Data Source={TAG_FILTER_SETS_DATABASE_NAME_V3}")
                .UseLazyLoadingProxies();
        }
    }
}