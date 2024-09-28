using HitomiScrollViewerLib.Entities;
using Microsoft.EntityFrameworkCore;
using static HitomiScrollViewerLib.Constants;

namespace HitomiScrollViewerLib.DbContexts {
    public class UserContext : DbContext {
        private static UserContext _main;
        public static UserContext Main => _main ??= new();

        public DbSet<Tag> SavedBrowseTags { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            // db file storage location = Windows.Storage.ApplicationData.Current.LocalFolder.Path
            optionsBuilder.UseSqlite($"Data Source={USER_DATABASE_PATH_V3}");
        }
    }
}
