using HitomiScrollViewerLib.Entities;
using Microsoft.EntityFrameworkCore;
using static HitomiScrollViewerLib.Utils;

namespace HitomiScrollViewerLib.DbContexts {
    public class HitomiContext : DbContext {
        public DbSet<TagFilterSet> TagFilterSets { get; set; }
        public DbSet<Gallery> Galleries { get; set; }
        public DbSet<Tag> Tags { get; set; }

        private static HitomiContext _main;
        public static HitomiContext Main {
            get => _main ??= new HitomiContext();
            set => _main = value;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            // db file storage location = Windows.Storage.ApplicationData.Current.LocalFolder.Path
            optionsBuilder.UseSqlite($"Data Source={MAIN_DATABASE_PATH_V3}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<Gallery>()
                .HasMany(t => t.Files)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);
        }

        // TODO
        //public async Task AddExampleTagFilterSetsAsync() {
        //    tagFilters1[CATEGORY_INDEX_MAP["language"]].Tags.Add("english");
        //    tagFilters1[CATEGORY_INDEX_MAP["tag"]].Tags.Add("full_color");
        //    List<TagFilterV3> tagFilters2 = GetListInstance();
        //    tagFilters2[CATEGORY_INDEX_MAP["type"]].Tags.Add("doujinshi");
        //    tagFilters2[CATEGORY_INDEX_MAP["series"]].Tags.Add("naruto");
        //    tagFilters2[CATEGORY_INDEX_MAP["language"]].Tags.Add("korean");
        //    List<TagFilterV3> tagFilters3 = GetListInstance();
        //    tagFilters3[CATEGORY_INDEX_MAP["series"]].Tags.Add("blue_archive");
        //    tagFilters3[CATEGORY_INDEX_MAP["female"]].Tags.Add("sole_female");
        //    List<TagFilterV3> tagFilters4 = GetListInstance();
        //    tagFilters4[CATEGORY_INDEX_MAP["tag"]].Tags.Add("non-h_imageset");

        //    await TagFilterSets.AddRangeAsync(
        //        new TagFilterSet() {
        //            Name = EXAMPLE_TAG_FILTER_SET_1,
        //            Tags = tagFilters1
        //        },
        //        new TagFilterSet() {
        //            Name = EXAMPLE_TAG_FILTER_SET_2,
        //            Tags = tagFilters2
        //        },
        //        new TagFilterSet() {
        //            Name = EXAMPLE_TAG_FILTER_SET_3,
        //            Tags = tagFilters3
        //        },
        //        new TagFilterSet() {
        //            Name = EXAMPLE_TAG_FILTER_SET_4,
        //            Tags = tagFilters4
        //        }
        //    );
        //    await SaveChangesAsync();
        //}
    }
}