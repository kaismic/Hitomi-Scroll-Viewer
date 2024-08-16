using HitomiScrollViewerLib.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static HitomiScrollViewerLib.Entities.TagFilterV3;
using static HitomiScrollViewerLib.SharedResources;
using static HitomiScrollViewerLib.Utils;

namespace HitomiScrollViewerLib.DbContexts {
    public class TagFilterSetContext(string dbFileName) : DbContext() {
        public DbSet<TagFilterSet> TagFilterSets { get; set; }

        private static TagFilterSetContext _main;
        public static TagFilterSetContext Main {
            get => _main ??= new TagFilterSetContext(Path.GetFileName(TFS_MAIN_DATABASE_PATH_V3));
            set => _main = value;
        }

        private readonly string _dbFileName = dbFileName;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            // db file storage location = Windows.Storage.ApplicationData.Current.LocalFolder.Path
            optionsBuilder.UseSqlite($"Data Source={_dbFileName};Pooling=False");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<TagFilterSet>()
                .HasMany(t => t.TagFilters)
                .WithOne() // Assuming TagFilterV3 has a navigation property to TagFilterSet
                .OnDelete(DeleteBehavior.Cascade);
        }

        public async Task AddExampleTagFilterSetsAsync() {
            List<TagFilterV3> tagFilters1 = GetListInstance();
            tagFilters1[CATEGORY_INDEX_MAP["language"]].Tags.Add("english");
            tagFilters1[CATEGORY_INDEX_MAP["tag"]].Tags.Add("full_color");
            List<TagFilterV3> tagFilters2 = GetListInstance();
            tagFilters2[CATEGORY_INDEX_MAP["type"]].Tags.Add("doujinshi");
            tagFilters2[CATEGORY_INDEX_MAP["series"]].Tags.Add("naruto");
            tagFilters2[CATEGORY_INDEX_MAP["language"]].Tags.Add("korean");
            List<TagFilterV3> tagFilters3 = GetListInstance();
            tagFilters3[CATEGORY_INDEX_MAP["series"]].Tags.Add("blue_archive");
            tagFilters3[CATEGORY_INDEX_MAP["female"]].Tags.Add("sole_female");
            List<TagFilterV3> tagFilters4 = GetListInstance();
            tagFilters4[CATEGORY_INDEX_MAP["tag"]].Tags.Add("non-h_imageset");

            await TagFilterSets.AddRangeAsync(
                new TagFilterSet() {
                    Name = EXAMPLE_TAG_FILTER_SET_1,
                    TagFilters = tagFilters1
                },
                new TagFilterSet() {
                    Name = EXAMPLE_TAG_FILTER_SET_2,
                    TagFilters = tagFilters2
                },
                new TagFilterSet() {
                    Name = EXAMPLE_TAG_FILTER_SET_3,
                    TagFilters = tagFilters3
                },
                new TagFilterSet() {
                    Name = EXAMPLE_TAG_FILTER_SET_4,
                    TagFilters = tagFilters4
                }
            );
            await SaveChangesAsync();
        }
    }
}