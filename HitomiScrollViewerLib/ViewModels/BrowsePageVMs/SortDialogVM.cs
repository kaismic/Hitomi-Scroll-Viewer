using CommunityToolkit.Mvvm.ComponentModel;
using HitomiScrollViewerLib.DbContexts;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace HitomiScrollViewerLib.ViewModels.BrowsePageVMs {
    public partial class SortDialogVM : DQObservableObject {
        private HitomiContext _context;
        public ObservableCollection<SortItemVM> ActiveSortItemVMs { get; }
        public ObservableCollection<SortItemVM> InactiveSortItemVMs { get; }

        [ObservableProperty]
        private string _dialogShowButtonText;

        public SortDialogVM(HitomiContext context) {
            _context = context;
            ActiveSortItemVMs = new(context.GallerySorts.Where(gs => gs.IsActive).Select(gs => new SortItemVM(context, gs)));
            InactiveSortItemVMs = new(context.GallerySorts.Where(gs => !gs.IsActive).Select(gs => new SortItemVM(context, gs)));

            foreach (SortItemVM vm in ActiveSortItemVMs) {
                vm.AddRequested += ActivateSortItem;
                vm.RemoveRequested += DeactivateSortItem;
            }
            foreach (SortItemVM vm in InactiveSortItemVMs) {
                vm.AddRequested += ActivateSortItem;
                vm.RemoveRequested += DeactivateSortItem;
            }
            DialogShowButtonText = $"{ActiveSortItemVMs.Count} sorts"; // TODO string localization
            ActiveSortItemVMs.CollectionChanged += (object _0, NotifyCollectionChangedEventArgs e) => {
                DialogShowButtonText = $"{ActiveSortItemVMs.Count} sorts"; // TODO string localization
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
