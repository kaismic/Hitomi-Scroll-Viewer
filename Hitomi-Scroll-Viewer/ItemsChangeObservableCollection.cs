using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Hitomi_Scroll_Viewer {
    internal class ItemsChangeObservableCollection<T> : ObservableCollection<T> {
        public void NotifyItemChange() {
            base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
