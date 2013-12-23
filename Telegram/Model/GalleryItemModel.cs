using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.Model {
    class GalleryItemModel {
        public string Thumb { get; set; }
        public bool IsVideo { get; set; }
        public string VideoLength { get; set; }
    }
}
