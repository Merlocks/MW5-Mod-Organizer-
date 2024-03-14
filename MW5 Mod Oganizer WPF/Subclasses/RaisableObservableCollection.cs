using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace MW5_Mod_Organizer_WPF.Subclasses
{
    public sealed class RaisableObservableCollection<T> : ObservableCollection<T>
    {
        public RaisableObservableCollection() : base() { }

        public RaisableObservableCollection(IEnumerable<T> collection) : base(collection) { }

        // Method to raise the CollectionChanged event
        public void RaiseCollectionChanged()
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
