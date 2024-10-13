using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

namespace HitomiScrollViewerLib.DAOs {
    public class TagFilterDAO {
        /// <summary>
        /// Do not manipulate this collection or any properties of the items in this collection directly.
        /// Instead, use the provided methods.
        /// </summary>
        public ObservableCollection<TagFilter> LocalTagFilters { get; }

        public TagFilterDAO() {
            using HitomiContext context = new();
            LocalTagFilters = new(context.TagFilters.Local.ToObservableCollection());
        }

        public void Add(TagFilter tf) {
            using HitomiContext context = new();
            LocalTagFilters.Add(tf);
            context.TagFilters.Add(tf);
            context.SaveChanges();
        }

        public void AddRange(IEnumerable<TagFilter> tfs) {
            using HitomiContext context = new();
            foreach (TagFilter tf in tfs) {
                LocalTagFilters.Add(tf);
            }
            context.TagFilters.AddRange(tfs);
            context.SaveChanges();
        }

        // TODO add undo functionality by using context.ChangeTracker.Clear(); and passing removed entities
        public void Remove(TagFilter tf) {
            using HitomiContext context = new();
            LocalTagFilters.Remove(tf);
            context.TagFilters.Remove(tf);
            context.SaveChanges();
        }
        
        public void RemoveRange(IEnumerable<TagFilter> tfs) {
            using HitomiContext context = new();
            foreach (var tf in tfs) {
                LocalTagFilters.Remove(tf);
            }
            context.TagFilters.RemoveRange(tfs);
            context.SaveChanges();
        }

        public void UpdateName(TagFilter tagFilter, string name) {
            using HitomiContext context = new();
            context.TagFilters.Find(tagFilter.Id).Name = name;
            context.SaveChanges();
            LocalTagFilters.First(tf => tf.Id == tagFilter.Id).Name = name;
        }

        public void UpdateTags(TagFilter tagFilter, ICollection<Tag> tags) {
            using HitomiContext context = new();
            // TODO why??????????
            tagFilter.Tags = tags;
            context.TagFilters.Find(tagFilter.Id).Tags = tags;
            context.SaveChanges();
            LocalTagFilters.First(tf => tf.Id == tagFilter.Id).Tags = tags;
        }
    }
}
