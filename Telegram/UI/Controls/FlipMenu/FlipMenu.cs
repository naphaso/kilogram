using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Clarity.Phone.Extensions;
using Coding4Fun.Toolkit.Controls;
using Microsoft.Phone.Controls;

namespace Telegram.UI.Controls {

    public class PhoneFlipMenuAction {
        #region NotificationCommand

        private class NotificationCommand : ICommand {
            private readonly Action _execute;
            private readonly Action _finish;
            private readonly Func<bool> _canExecute;

            public NotificationCommand(Action execute, Func<bool> canExecute, Action finishCommand) {
                _execute = execute;
                _canExecute = canExecute;
                _finish = finishCommand;
            }

            bool ICommand.CanExecute(object parameter) {
                return _canExecute();
            }

            event EventHandler ICommand.CanExecuteChanged {
                add { }
                remove { }
            }

            void ICommand.Execute(object parameter) {
                _execute();
                _finish();
            }
        }

        #endregion

        public object Content { get; set; }
        internal ICommand Command { get; private set; }

        internal PhoneFlipMenu Parent { get; set; }

        public PhoneFlipMenuAction(object content, Action execute)
            : this(content, execute, () => true) {
        }

        public PhoneFlipMenuAction(object content, Action execute, Func<bool> canExecute) {
            Content = content;
            Command = new NotificationCommand(execute, canExecute, () => { Parent.Hide(); });
        }
    }
    public class PhoneFlipMenuItem : HyperlinkButton {
        public PhoneFlipMenuItem() {
            DefaultStyleKey = typeof(PhoneFlipMenuItem);
        }
    }


    public class PhoneFlipMenu : PopUp<string, PopUpResult> {
        protected StackPanel theStackPanel;
        PhoneFlipMenuAction[] theActions;

        public PhoneFlipMenu(params PhoneFlipMenuAction[] actions) {
            DefaultStyleKey = typeof(PhoneFlipMenu);

            IsAppBarVisible = !CheckForApplicationBar();
            IsBackKeyOverride = false;
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Bottom;

            AnimationType = DialogService.AnimationTypes.Swivel;
            theActions = actions;
        }

        /// <summary>
        /// Checks for application bar.
        /// </summary>
        /// <returns></returns>
        private bool CheckForApplicationBar() {
            if (Page.ApplicationBar != null) {
                Background = CheckAppBarBackgroundColour(Page.ApplicationBar.BackgroundColor);
                Foreground = CheckAppBarForegroundColour(Page.ApplicationBar.ForegroundColor);
                return Page.ApplicationBar.IsVisible;
            }
            return false;
        }

        /// <summary>
        /// Checks the app bar foreground colour.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <returns></returns>
        private Brush CheckAppBarForegroundColour(Color color) {
            if (color.ToString().Equals("#00000000")) // Default system theme no colour is given
            {
                bool IsDark = ((Visibility)Application.Current.Resources["PhoneDarkThemeVisibility"] == System.Windows.Visibility.Visible);
                if (IsDark) {
                    color = Colors.White;
                } else {
                    color = Colors.Black;
                }
            }
            return new SolidColorBrush(color);
        }

        /// <summary>
        /// Checks the app bar background colour.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <returns></returns>
        private Brush CheckAppBarBackgroundColour(Color color) {
            if (color.ToString().Equals("#00000000")) // Default system theme no colour is given
            {
                bool IsDark = ((Visibility)Application.Current.Resources["PhoneDarkThemeVisibility"] == System.Windows.Visibility.Visible);
                if (IsDark) {
                    color = Color.FromArgb(255, 33, 32, 33);
                } else {
                    color = Color.FromArgb(255, 223, 223, 223);
                }
            }
            return new SolidColorBrush(color);
        }

        /// <summary>
        /// Called when [apply template].
        /// </summary>
        public override void OnApplyTemplate() {
            base.OnApplyTemplate();

            // Get the stackpanel from the template so we can populate its contents with the 
            // provided actions.
            theStackPanel = GetTemplateChild("TheStackPanel") as StackPanel;
            foreach (var action in theActions) {
                action.Parent = this;
                var menuItem = new PhoneFlipMenuItem {
                    Content = action.Content,
                    Command = action.Command,
                    Foreground = this.Foreground
                };
                theStackPanel.Children.Add(menuItem);
            }
        }

        internal void Close() {
            base.Hide();
        }

        /// <summary>
        /// Gets the frame.
        /// </summary>
        /// <value>The frame.</value>
        private static PhoneApplicationFrame Frame {
            get {
                return Application.Current.RootVisual as PhoneApplicationFrame;
            }
        }

        /// <summary>
        /// Gets the page.
        /// </summary>
        /// <value>The page.</value>
        private static PhoneApplicationPage Page {
            get {
                return Frame.Content as PhoneApplicationPage;
            }
        }
    }
}
