using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace HitomiScrollViewerLib.DAOs {
    public class TagFilterDAO {
        /// <summary>
        /// Do not manipulate this collection or any properties of the items in this collection directly.
        /// Instead, use the provided methods.
        /// </summary>
        public ObservableCollection<TagFilter> LocalTagFilters { get; }

        public TagFilterDAO() {
            using HitomiContext context = new();
            LocalTagFilters = new([.. context.TagFilters.AsNoTracking()]);
        }

        public void Add(TagFilter tf) {
            using HitomiContext context = new();
            context.Tags.AttachRange(tf.Tags);
            context.TagFilters.Add(tf);
            context.SaveChanges();
            LocalTagFilters.Add(tf);
        }

        public void AddRange(IEnumerable<TagFilter> tfs) {
            using HitomiContext context = new();
            context.Tags.AttachRange(tfs.SelectMany(tfs => tfs.Tags));
            context.TagFilters.AddRange(tfs);
            context.SaveChanges();
            foreach (TagFilter tf in tfs) {
                LocalTagFilters.Add(tf);
            }
        }

        public void RemoveRange(IEnumerable<TagFilter> tfs) {
            using HitomiContext context = new();
            context.TagFilters.RemoveRange(tfs);
            context.SaveChanges();
            foreach (var tf in tfs) {
                LocalTagFilters.Remove(tf);
            }
        }

        /// <summary>
        /// <paramref name="tagFilter"/> must be from <see cref="LocalTagFilters"/>
        /// </summary>
        /// <param name="tagFilter"></param>
        /// <param name="name"></param>
        public static void UpdateName(TagFilter tagFilter, string name) {
            using HitomiContext context = new();
            context.TagFilters.Attach(tagFilter);
            tagFilter.Name = name;
            context.SaveChanges();
        }

        /// <summary>
        /// <paramref name="tagFilter"/> must be from <see cref="LocalTagFilters"/>
        /// </summary>
        /// <param name="tagFilter"></param>
        /// <param name="tags"></param>
        public static void UpdateTags(TagFilter tagFilter, ICollection<Tag> tags) {
            using HitomiContext context = new();
            context.TagFilters.Attach(tagFilter);
            tagFilter.Tags = tags;
            context.SaveChanges();
            // TODO figure out why not working
        }

        public void UpdateTags(string name, ICollection<Tag> tags) {
            using HitomiContext context = new();
            TagFilter tagFilter = LocalTagFilters.First(tf => tf.Name == name);
            context.TagFilters.Attach(tagFilter);
            tagFilter.Tags = tags;
            context.SaveChanges();
        }
    }
}
