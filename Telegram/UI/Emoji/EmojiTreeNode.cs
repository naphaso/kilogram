using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.UI.Emoji {
    public class EmojiTreeNode {
        public Dictionary<Char, EmojiTreeNode> items;
        public long code;

        public EmojiTreeNode(long code) {
            this.code = code;
        }

        public EmojiTreeNode() {
            this.items = new Dictionary<Char, EmojiTreeNode>();
        }

        public EmojiTreeNode get(Char ch) {
            return items[ch];
        }

        public void put(Char ch, EmojiTreeNode node) {
            items.Add(ch, node);
        }

        public bool isSubtree() {
            return items != null;
        }

        public bool isLeaf() {
            return items == null;
        }
    }
}
