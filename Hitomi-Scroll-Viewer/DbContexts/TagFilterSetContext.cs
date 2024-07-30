using Hitomi_Scroll_Viewer.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Hitomi_Scroll_Viewer.Utils;

namespace Hitomi_Scroll_Viewer.DbContexts
{
    internal class TagFilterSetContext : DbContext
    {
        public DbSet<TagFilterSet> TagFilterSets { get; set; }
        public DbSet<TagFilter> TagFilters { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // db file storage location = Windows.Storage.ApplicationData.Current.LocalFolder.Path
            optionsBuilder.UseSqlite($"Data Source={TAG_FILTER_SET_FILE_NAME}");
        }
    }
}
