using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace HitomiScrollViewerLib.DAOs {
    public static class TagFilterDAO {
        /// <summary>
        /// Do not manipulate this collection or any properties of the items in this collection directly.
        /// Instead, use the provided methods.
        /// </summary>
        public static ObservableCollection<TagFilter> LocalTagFilters { get; }

        static TagFilterDAO() {
            using HitomiContext context = new();
            LocalTagFilters = context.TagFilters.Local.ToObservableCollection();
        }

        public static void Add(TagFilter tf) {
            using HitomiContext context = new();
            LocalTagFilters.Add(tf);
            context.TagFilters.Add(tf);
            context.SaveChanges();
        }

        // TODO add undo functionality by using context.ChangeTracker.Clear(); and passing removed entities
        public static void Remove(TagFilter tf) {
            using HitomiContext context = new();
            LocalTagFilters.Remove(tf);
            context.TagFilters.Remove(tf);
            context.SaveChanges();
        }
        
        public static void RemoveRange(IEnumerable<TagFilter> tfs) {
            using HitomiContext context = new();
            foreach (var tf in tfs) {
                LocalTagFilters.Remove(tf);
            }
            context.TagFilters.RemoveRange(tfs);
            context.SaveChanges();
        }

        /// <summary>
        /// <paramref name="tf"/> must be an item in <see cref="LocalTagFilters"/>
        /// </summary>
        /// <param name="tf"></param>
        /// <param name="name"></param>
        public static void UpdateName(TagFilter tf, string name) {
            using HitomiContext context = new();
            TagFilter entity = context.TagFilters.Find(tf.Id);
            tf.Name = name;
            entity.Name = name;
            context.SaveChanges();
        }

        /// <summary>
        /// <paramref name="tf"/> must be an item in <see cref="LocalTagFilters"/>
        /// </summary>
        /// <param name="tf"></param>
        /// <param name="tags"></param>
        public static void UpdateTags(TagFilter tf, ICollection<Tag> tags) {
            using HitomiContext context = new();
            TagFilter entity = context.TagFilters.Find(tf.Id);
            tf.Tags = tags;
            entity.Tags = tags;
            context.SaveChanges();
        }
    }
}
