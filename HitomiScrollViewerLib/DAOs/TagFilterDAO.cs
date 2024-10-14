using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace HitomiScrollViewerLib.DAOs {
    /// <summary>
    /// I have no clue of why this happens but apparently you have to attach the entity to the local
    /// context AND THEN you can add or do whatever to <see cref="LocalTagFilters"/>. Otherwise a bunch
    /// of problems and errors happen.
    /// </summary>
    public class TagFilterDAO {
        /// <summary>
        /// Do not manipulate this collection or any properties of the items in this collection directly.
        /// Instead, use the provided methods.
        /// </summary>
        public ObservableCollection<TagFilter> LocalTagFilters { get; }

        public TagFilterDAO() {
            using HitomiContext context = new();
            context.TagFilters.Load();
            LocalTagFilters = new(context.TagFilters.Local.ToObservableCollection());
        }

        public void Add(TagFilter tf) {
            using HitomiContext context = new();
            context.TagFilters.Add(tf);
            LocalTagFilters.Add(tf);
            context.SaveChanges();
        }

        public void AddRange(IEnumerable<TagFilter> tfs) {
            using HitomiContext context = new();
            context.TagFilters.AddRange(tfs);
            foreach (TagFilter tf in tfs) {
                LocalTagFilters.Add(tf);
            }
            context.SaveChanges();
        }

        // TODO add undo functionality by using context.ChangeTracker.Clear(); and passing removed entities
        public void Remove(TagFilter tf) {
            using HitomiContext context = new();
            context.TagFilters.Remove(tf);
            LocalTagFilters.Remove(tf);
            context.SaveChanges();
        }
        
        public void RemoveRange(IEnumerable<TagFilter> tfs) {
            using HitomiContext context = new();
            context.TagFilters.RemoveRange(tfs);
            foreach (var tf in tfs) {
                LocalTagFilters.Remove(tf);
            }
            context.SaveChanges();
        }

        /// <summary>
        /// <paramref name="tagFilter"/> must be from <see cref="LocalTagFilters"/>
        /// </summary>
        /// <param name="tagFilter"></param>
        /// <param name="name"></param>
        public void UpdateName(TagFilter tagFilter, string name) {
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
        public void UpdateTags(TagFilter tagFilter, ICollection<Tag> tags) {
            using HitomiContext context = new();
            context.TagFilters.Attach(tagFilter);
            tagFilter.Tags = tags;
            context.SaveChanges();
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
