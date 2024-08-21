using HitomiScrollViewerLib.Entities;
using Microsoft.EntityFrameworkCore;
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