using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Phone.Controls;

namespace Telegram.UI.Controls {
    public class ExtendedSelector : LongListSelector {
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(object), typeof(ExtendedSelector), new PropertyMetadata(default(object)));

        public static readonly DependencyProperty SelectionModeProperty =
            DependencyProperty.Register("SelectionMode", typeof(SelectionMode), typeof(ExtendedSelector), new PropertyMetadata(default(SelectionMode)));

        public SelectionMode SelectionMode {
            get { return (SelectionMode)GetValue(SelectionModeProperty); }
            set { SetValue(SelectionModeProperty, value); }
        }

        public new object SelectedItem {
            get { return GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public ExtendedSelector() {
            SelectionMode = SelectionMode.Single;

            SelectionChanged += (sender, args) => {
                if (SelectionMode == SelectionMode.Single)
                    SelectedItem = args.AddedItems[0];
                else if (SelectionMode == SelectionMode.Multiple) {
                    if (SelectedItem == null) {
                        SelectedItem = new List<object>();
                    }

                    foreach (var item in args.AddedItems) {
                        ((List<object>)SelectedItem).Add(item);
                    }

                    foreach (var removedItem in args.RemovedItems) {
                        if (((List<object>)SelectedItem).Contains(removedItem)) {
                            ((List<object>)SelectedItem).Remove(removedItem);
                        }
                    }
                }
            };

            Loaded += (sender, args) => {
                if (ItemsSource.Count > 0)
                    ScrollTo(ItemsSource[ItemsSource.Count - 1]);

                ((INotifyCollectionChanged)ItemsSource).CollectionChanged += (sender2, args2) => {
                    if (ItemsSource.Count > 0 && args2.NewItems != null)
                        ScrollTo(ItemsSource[ItemsSource.Count-1]);
                };
            };
        }

    }
}
