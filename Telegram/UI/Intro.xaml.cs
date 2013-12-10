using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Threading;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Telegram.UI {
    public partial class Intro : PhoneApplicationPage {
        private int _currentIndex = 0;
        private DispatcherTimer dt;
        private bool _interrupted = false;
        public Intro() {
            InitializeComponent();
            
            introPivot.MouseMove += delegate {
                _interrupted = true;
            };

            introPivot.Tap += delegate {
                _interrupted = false;
                GoNext();
            };

            dt = new DispatcherTimer {Interval = TimeSpan.FromSeconds(3)};

            dt.Tick += delegate {
                if (!_interrupted)
                    GoNext();
            };

            dt.Start();
        }

        private void GoNext() {
            introPivot.SelectedIndex = (++_currentIndex % introPivot.Items.Count);
        }

        private void button_Click(object sender, RoutedEventArgs e) {
            NavigationService.Navigate(new Uri("/UI/StartPage.xaml", UriKind.Relative));

        }
    }
}