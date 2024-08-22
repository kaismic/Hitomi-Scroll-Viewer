using HitomiScrollViewerLib.Entities;
using Microsoft.EntityFrameworkCore;
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
            Enumerable.Concat(["123"], Enumerable.Range('a', 26)
                .Select(letter => letter.ToString()))
                .ToArray();

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

        public static void InitAddDatabaseTags() {
            for (int i = 0; i < Tag.CATEGORY_NUM; i++) {
                Category category = (Category)i;
                string categoryStr = CATEGORY_DIR_DICT[category];
                string dir = Path.Combine(
                    Windows.ApplicationModel.Package.Current.InstalledPath,
                    TAG_RES_ROOT_DIR,
                    categoryStr
                );
                foreach (string alphanumStr in ALPHABETS_WITH_123) {
                    string[] tagValues = File.ReadAllLines(Path.Combine(dir, $"{categoryStr.ToLower()}-{alphanumStr}.txt"));
                    Main.Tags.AddRange(tagValues.Select(tagValue => new Tag() { Category = category, Value = tagValue }));
                }
            }
            Main.SaveChanges();
        }

        public void AddExampleTagFilterSets() {
            TagFilterSets.AddRange(
                new() {
                    Name = EXAMPLE_TAG_FILTER_SET_1,
                    Tags = [
                        Tag.GetTag("full_color", Category.Tag),
                        Tag.GetTag("very_long_hair", Category.Female),
                    ]
                },
                new() {
                    Name = EXAMPLE_TAG_FILTER_SET_2,
                    Tags = [
                        Tag.GetTag("glasses", Category.Female),
                        Tag.GetTag("sole_male", Category.Male),
                    ]
                },
                new() {
                    Name = EXAMPLE_TAG_FILTER_SET_3,
                    Tags = [
                        Tag.GetTag("naruto", Category.Series),
                        Tag.GetTag("big_breasts", Category.Tag),
                    ]
                },
                new() {
                    Name = EXAMPLE_TAG_FILTER_SET_4,
                    Tags = [
                        Tag.GetTag("non-h_imageset", Category.Tag)
                    ]
                }
            );
            SaveChanges();
        }
    }
}