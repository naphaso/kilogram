using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.UI.Emoji {
    class EmojiTree {
        public EmojiTreeNode root = new EmojiTreeNode();
        public void createTree(long[] array) {
            byte[] buffer = new byte[8];
            using (MemoryStream memory = new MemoryStream(buffer, true)) {
                for (int i = 0; i < array.Length; i++) {
                    using (BinaryWriter writer = new BinaryWriter(memory)) {
                        writer.Write(array[i]);
                    }

                    memory.Position = 0;
                    using (BinaryReader reader = new BinaryReader(memory)) {
                        while (buffer[memory.Position] == 0 && buffer[memory.Position + 1] == 0) {
                            reader.ReadChar();
                        }

                        allocateInTree(reader.ReadBytes((int) memory.Length - (int) memory.Position), root, buffer[i]);
                    }
                }
            }
//            ByteBuffer bb = ByteBuffer.allocate(8);
//        for (long item: array) {
//
//            bb.putLong(item);
//            bb.flip();
//
//            while (bb.array()[bb.position()] == 0 && bb.array()[bb.position()+1] == 0) {
//                bb.getChar();
//            }
//
//            allocateInTree(bb, root, item);
//            bb.clear();
//        }
    }

//        public void allocateInTree(ByteBuffer bb, EmojiTreeNode node, long payload) {
        public void allocateInTree(byte[] bb, EmojiTreeNode node, long payload) {

//            Character ch = bb.getChar();

            using (MemoryStream memory = new MemoryStream(bb, false)) {
                using (BinaryReader reader = new BinaryReader(memory)) {
                    Char ch = reader.ReadChar();
                    EmojiTreeNode newNode = node.get(ch);

                    if (newNode != null) {
                        if (memory.Length - memory.Position == 0) {
                            if (newNode.isSubtree()) {
                                newNode.put(ch, new EmojiTreeNode(payload));
                            }
                        }
                        else {
                            allocateInTree(reader.ReadBytes((int) memory.Length - (int) memory.Position), newNode,
                                payload);
                        }

                    }
                    else {
                        if (memory.Length - memory.Position == 0) {
                            node.put(ch, new EmojiTreeNode(payload));
                            return;
                        }

                        newNode = new EmojiTreeNode();
                        node.put(ch, newNode);
                        allocateInTree(reader.ReadBytes((int) memory.Length - (int) memory.Position), newNode,
                            payload);
                    }
                }

//            EmojiTreeNode newNode = node.get(ch);

//            if (newNode != null) {
//                if (bb.remaining() == 0) {
//                    if (newNode.isSubtree()) {
//                        newNode.put(ch, new EmojiTreeNode(payload));
//                    }
//                } else {
//                    allocateInTree(bb, newNode, payload);
//                }
//            } else {
//                if (bb.remaining() == 0) {
//                    node.put(ch, new EmojiTreeNode(payload));
//                    return;
//                }
//
//                newNode = new EmojiTreeNode();
//                node.put(ch, newNode);
//                allocateInTree(bb, newNode, payload);
//            }
            }
        }

        public List<EmojiSearchResult> findEmoji(string str) {
            List<EmojiSearchResult> emojiSearchResults = new List<EmojiSearchResult>();

            EmojiTreeNode currentNode = root;
            int start = 0;

            for (int i = 0; i < str.Length; i++) {
                Char ch = str[i];

                currentNode = currentNode.get(ch);

                if (currentNode != null) {
                    if (currentNode.isLeaf()) {
                        // save
                        emojiSearchResults.Add(new EmojiSearchResult(currentNode.code, start, i));
                    } else {
                        continue;
                    }
                }


                currentNode = root;
                start = i + 1;
            }

            return emojiSearchResults;
        }
    }
}
