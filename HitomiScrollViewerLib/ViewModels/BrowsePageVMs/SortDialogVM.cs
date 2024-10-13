using CommunityToolkit.Mvvm.ComponentModel;
using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace HitomiScrollViewerLib.ViewModels.BrowsePageVMs {
    public partial class SortDialogVM : DQObservableObject {
        private HitomiContext _context;
        public ObservableCollection<SortItemVM> ActiveSortItemVMs { get; }
        public ObservableCollection<SortItemVM> InactiveSortItemVMs { get; }

        [ObservableProperty]
        private string _sortCountText;

        public event Action SortDirectionChanged;

        public SortDialogVM(HitomiContext context) {
            _context = context;
            // TODO see if I need to create another DAO for GallerySort or SortItemVM
            SortDirectionEntity[] sortDirections = [.. _context.SortDirections.OrderBy(sd => sd.SortDirection)];
            GallerySortEntity[] gallerySortEntities = [.. _context.GallerySorts];
            foreach (GallerySortEntity gs in gallerySortEntities) {
                gs.SortDirectionChanged += () => SortDirectionChanged?.Invoke();
            }
            ActiveSortItemVMs = new(
                gallerySortEntities
                .Where(gs => gs.IsActive)
                .OrderBy(gs => gs.Index)
                .Select(gs => new SortItemVM(gs, sortDirections))
            );
            InactiveSortItemVMs = new(
                gallerySortEntities
                .Where(gs => !gs.IsActive)
                .Select(gs => new SortItemVM(gs, sortDirections))
            );

            foreach (SortItemVM vm in ActiveSortItemVMs) {
                vm.AddRequested += ActivateSortItem;
                vm.RemoveRequested += DeactivateSortItem;
            }
            foreach (SortItemVM vm in InactiveSortItemVMs) {
                vm.AddRequested += ActivateSortItem;
                vm.RemoveRequested += DeactivateSortItem;
            }
            SortCountText = $"{ActiveSortItemVMs.Count} sorts"; // TODO string localization
            ActiveSortItemVMs.CollectionChanged += (object _0, NotifyCollectionChangedEventArgs e) => {
                SortCountText = $"{ActiveSortItemVMs.Count} sorts"; // TODO string localization
                for (int i = 0; i < ActiveSortItemVMs.Count; i++) {
                    ActiveSortItemVMs[i].GallerySort.Index = i;
                }
                _context.SaveChanges();
            };
        }

        private void ActivateSortItem(SortItemVM vm) {
            InactiveSortItemVMs.Remove(vm);
            ActiveSortItemVMs.Add(vm);
            vm.GallerySort.IsActive = true;
            _context.SaveChanges();
        }
        private void DeactivateSortItem(SortItemVM vm) {
            ActiveSortItemVMs.Remove(vm);
            InactiveSortItemVMs.Add(vm);
            vm.GallerySort.IsActive = false;
            _context.SaveChanges();
        }
    }
}
