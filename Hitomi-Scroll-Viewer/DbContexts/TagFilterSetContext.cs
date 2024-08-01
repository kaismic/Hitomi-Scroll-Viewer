using Hitomi_Scroll_Viewer.Entities;
using Microsoft.EntityFrameworkCore;
using static Hitomi_Scroll_Viewer.Utils;

namespace Hitomi_Scroll_Viewer.DbContexts
{
    internal class TagFilterSetContext : DbContext
    {
        public DbSet<TagFilterSet> TagFilterSets { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // db file storage location = Windows.Storage.ApplicationData.Current.LocalFolder.Path
            optionsBuilder.UseSqlite($"Data Source={TAG_FILTER_SETS_FILE_NAME}");
        }
    }
}
