using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Telegram.Annotations;

namespace Telegram.Model {
    public class DialogMessageItem {

        public string Sender { get; set; }
        

        public string Text { get; set; }
        public string Time { get; set; }
        public bool IsOut { get; set; }

        public Visibility SenderVisibility { 
            get { return Sender.Length > 0 ? Visibility.Visible : Visibility.Collapsed; }
        }

        private string _forwardedFrom;

        public string ForwardedFrom {
            get {
                var resp = _forwardedFrom ?? "";
                return "Forwarded from " + resp;
            }

            set { _forwardedFrom = value; }
        }

        public Visibility ForwardedVisibility {
            get {
                if (_forwardedFrom == null)
                    return Visibility.Collapsed;

                return _forwardedFrom.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}
