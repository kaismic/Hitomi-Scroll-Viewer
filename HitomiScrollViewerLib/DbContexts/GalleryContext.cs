using HitomiScrollViewerLib.Entities;
using Microsoft.EntityFrameworkCore;
using static HitomiScrollViewerLib.Utils;

namespace HitomiScrollViewerLib.DbContexts
{
    internal class GalleryContext : DbContext
    {
        public DbSet<Gallery> Galleries { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // db file storage location = Windows.Storage.ApplicationData.Current.LocalFolder.Path
            optionsBuilder.UseSqlite($"Data Source={GALLERIES_DATABASE_NAME_V3}");
        }
    }
}
