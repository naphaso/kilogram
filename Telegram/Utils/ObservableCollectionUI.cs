using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;

namespace Telegram.Utils {
    public class ObservableCollectionUI<t> : ObservableCollection<t> {
        // Override the event so this class can access it
        public override event NotifyCollectionChangedEventHandler CollectionChanged;

        public ObservableCollectionUI(IEnumerable<t> collection) : base(collection) { }
        public ObservableCollectionUI(List<t> collection) : base(collection) { }

        public ObservableCollectionUI() {
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e) {
            // Be nice - use BlockReentrancy like MSDN said
            using (BlockReentrancy()) {
                var eventHandler = CollectionChanged;
                if (eventHandler != null) {
                    Delegate[] delegates = eventHandler.GetInvocationList();
                    // Walk thru invocation list
                    foreach (NotifyCollectionChangedEventHandler handler in delegates) {
//                            dispatcherObject.Dispatcher.Invoke(DispatcherPriority.DataBind,
//                                          handler, this, e);
                            Deployment.Current.Dispatcher.BeginInvoke(handler, this, e);

                    }
                }
            }
        }
    }
}