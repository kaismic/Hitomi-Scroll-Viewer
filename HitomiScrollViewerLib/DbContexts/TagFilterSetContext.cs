using HitomiScrollViewerLib.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using static HitomiScrollViewerLib.SharedResources;
using static HitomiScrollViewerLib.Utils;
using static HitomiScrollViewerLib.Entities.TagFilterV3;

namespace HitomiScrollViewerLib.DbContexts {
    public class TagFilterSetContext : DbContext {
        public DbSet<TagFilterSet> TagFilterSets { get; set; }

        private static TagFilterSetContext _mainContext;
        public static TagFilterSetContext MainContext {
            get => _mainContext ??= new TagFilterSetContext();
        }

        private TagFilterSetContext(): base() {}

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            // db file storage location = Windows.Storage.ApplicationData.Current.LocalFolder.Path
            optionsBuilder
                .UseSqlite($"Data Source={TAG_FILTER_SETS_DATABASE_NAME_V3}")
                .UseLazyLoadingProxies();
        }

        /// <returns><see langword="true" /> if the database is created, <see langword="false" /> if it already existed.</returns>
        public bool Init() {
            bool created = Database.EnsureCreated();
            TagFilterSets.Load(); // TODO test if this is needed or where else I can put it
            return created;
        }

        public void AddExampleTagFilterSets() {
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

            TagFilterSets.AddRange(
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

            SaveChanges();
        }
    }
}