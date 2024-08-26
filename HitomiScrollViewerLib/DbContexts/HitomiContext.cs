using HitomiScrollViewerLib.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static HitomiScrollViewerLib.SharedResources;
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

        private static readonly string[] ALPHABETS_WITH_123 =
            Enumerable.Concat(
                ["123"],
                Enumerable.Range('a', 26).Select(intValue => Convert.ToChar(intValue).ToString())
            ).ToArray();

        private const string TAG_RES_ROOT_DIR = "TagResources";
        private static readonly Dictionary<Category, string> CATEGORY_DIR_DICT = new() {
            { Category.Tag, "Tags" },
            { Category.Male, "Males" },
            { Category.Female, "Females" },
            { Category.Artist, "Artists" },
            { Category.Group, "Groups" },
            { Category.Character, "Characters" },
            { Category.Series, "Series" }
        };

        public static readonly int TAG_DATABASE_INIT_NUM = Tag.CATEGORY_NUM * ALPHABETS_WITH_123.Length;
        public event EventHandler<int> AddtDatabaseTagsProgressChanged;
        public static void AddDatabaseTags() {
            int progressValue = 0;
            for (int i = 0; i < Tag.CATEGORY_NUM; i++) {
                Category category = (Category)i;
                string categoryStr = CATEGORY_DIR_DICT[category];
                string dir = Path.Combine(
                    Windows.ApplicationModel.Package.Current.InstalledPath,
                    TAG_RES_ROOT_DIR,
                    categoryStr
                );
                foreach (string alphanumStr in ALPHABETS_WITH_123) {
                    string path = Path.Combine(dir, $"{categoryStr.ToLower()}-{alphanumStr}.txt");
                    string[] tagValues = File.ReadAllLines(path);
                    Main.Tags.AddRange(tagValues.Select(tagValue => new Tag() { Category = category, Value = tagValue }));
                    Main.AddtDatabaseTagsProgressChanged?.Invoke(null, ++progressValue);
                }
            }
            Main.SaveChanges();
        }

        public static void ClearInvocationList() {
            var invocList = Main.AddtDatabaseTagsProgressChanged?.GetInvocationList();
            if (invocList != null) {
                foreach (Delegate d in invocList) {
                    Main.AddtDatabaseTagsProgressChanged -= (EventHandler<int>)d;
                }
            }
        }

        public void AddExampleTagFilterSets() {
            ResourceMap resourceMap = MainResourceMap.GetSubtree("ExampleTFSNames");
            TagFilterSets.AddRange(
                new() {
                    Name = resourceMap.GetValue("ExampleTagFilterSet_1").ValueAsString,
                    Tags = [
                        Tag.GetTag("full color", Category.Tag),
                        Tag.GetTag("very long hair", Category.Female),
                    ]
                },
                new() {
                    Name = resourceMap.GetValue("ExampleTagFilterSet_2").ValueAsString,
                    Tags = [
                        Tag.GetTag("glasses", Category.Female),
                        Tag.GetTag("sole male", Category.Male),
                    ]
                },
                new() {
                    Name = resourceMap.GetValue("ExampleTagFilterSet_3").ValueAsString,
                    Tags = [
                        Tag.GetTag("naruto", Category.Series),
                        Tag.GetTag("big breasts", Category.Tag),
                    ]
                },
                new() {
                    Name = resourceMap.GetValue("ExampleTagFilterSet_4").ValueAsString,
                    Tags = [
                        Tag.GetTag("non-h imageset", Category.Tag)
                    ]
                }
            );
            SaveChanges();
        }
    }
}