using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;

namespace Telegram.Utils {
    public class ObservableCollectionUI<T> : ObservableCollection<T> {
        // Override the event so this class can access it
        public override event NotifyCollectionChangedEventHandler CollectionChanged;

        
        public ObservableCollectionUI(IEnumerable<T> collection) : base(collection) { }
        public ObservableCollectionUI(List<T> collection) : base(collection) { }

        public ObservableCollectionUI() {
        }

        private volatile bool suppressNotifications = false;

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e) {
            if(suppressNotifications) {
                return;
            }

            using (BlockReentrancy()) {
                var eventHandler = CollectionChanged;
                if (eventHandler != null) {
                    Delegate[] delegates = eventHandler.GetInvocationList();
                    // Walk thru invocation list
                    foreach (NotifyCollectionChangedEventHandler handler in delegates) {
                            Deployment.Current.Dispatcher.BeginInvoke(handler, this, e);
                    }
                }
            }
        }

        public void AddRange(IEnumerable<T> list) {
            if(list == null) {
                throw new ArgumentNullException("list");
            }

            suppressNotifications = true;

            foreach(var element in list) {
                Add(element);
            }

            suppressNotifications = false;

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void AddRange(List<T> list) {
            if (list == null) {
                throw new ArgumentNullException("list");
            }

            suppressNotifications = true;

            foreach (var element in list) {
                Add(element);
            }

            suppressNotifications = false;

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}