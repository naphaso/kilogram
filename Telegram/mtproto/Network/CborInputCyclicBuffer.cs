using System;
using System.Linq;

namespace Telegram.MTProto.Network {
    class CborInputCyclicBuffer : CborInput {
        private byte[] buffer;
        private int readIndex;
        private int writeIndex;
        public CborInputCyclicBuffer(int size) {
            buffer = new byte[size];
        }

        public event InputHandler InputEvent;
        public void AddChunk(byte[] chunk) {
            //Debug.Log("add chunk start: r " + readIndex + ", w " + writeIndex);
            if(readIndex <= writeIndex) {
                int headSize = buffer.Length - writeIndex;
                if(chunk.Length <= headSize) {
                    Array.Copy(chunk, 0, buffer, writeIndex, chunk.Length);
                    writeIndex += chunk.Length;
                    if(writeIndex == buffer.Length) {
                        writeIndex = 0;
                    }
                } else if(chunk.Length <= headSize + readIndex) {
                    int tailSize = chunk.Length - headSize;
                    Array.Copy(chunk, 0, buffer, writeIndex, headSize);
                    Array.Copy(chunk, headSize, buffer, 0, tailSize);
                    writeIndex = tailSize;
                } else {
                    throw new OverflowException("cyclic buffer overflow");
                }
            } else {
                // [----w----r----]
                int headSize = readIndex - writeIndex;
                if(chunk.Length <= headSize) {
                    Array.Copy(chunk, 0, buffer, writeIndex, chunk.Length);
                    writeIndex += chunk.Length;
                } else {
                    throw new OverflowException("cyclic buffer overflow");
                }
            }

            InputEvent();
            //Debug.Log("add chunk end: r " + readIndex + ", w " + writeIndex);
        }

        public void AddChunk(byte[] chunk, int offset, int length) {
            throw new NotImplementedException();
        }

        public bool HasBytes(int count) {
            return ToRead >= count;
        }

        public int GetByte() {
            byte r = buffer[readIndex++];
            if(readIndex == buffer.Count())
                readIndex = 0;
            return r;
        }

        public uint GetInt8() {
            uint r = buffer[readIndex++];
            if (readIndex == buffer.Count())
                readIndex = 0;
            return r;
        }

        private int ToRead {
            get {
                if(readIndex <= writeIndex) {
                    return writeIndex - readIndex;
                } else {
                    return buffer.Length - readIndex + writeIndex;
                }
            }
        }

        private int FullHead {
            get {
                if(readIndex <= writeIndex) {
                    return writeIndex - readIndex;
                } else {
                    return buffer.Length - readIndex;
                }
            }
        }
        public uint GetInt16() {
            if(FullHead >= 2) {
                uint r = ((uint) buffer[readIndex] << 8) | ((uint) buffer[readIndex + 1]);
                readIndex += 2;
                if(readIndex == buffer.Length)
                    readIndex = 0;
                return r;
            }

            return ((uint)GetByte() << 8) | ((uint)GetByte()); 
        }

        public uint GetInt32() {
            if (FullHead >= 4) {
                uint r = ((uint)buffer[readIndex] << 24) | ((uint)buffer[readIndex + 1] << 16) | ((uint)buffer[readIndex + 2] << 8) | ((uint)buffer[readIndex + 3]);
                readIndex += 4;
                if (readIndex == buffer.Length)
                    readIndex = 0;
                return r;
            }
            
            return ((uint)GetByte() << 24) | ((uint)GetByte() << 16) | ((uint)GetByte() << 8) | ((uint)GetByte());
        }

        public uint ReadInt32() {
            throw new NotImplementedException();
        }

        public ulong GetInt64() {
            if (FullHead >= 8) {
                ulong r = ((ulong)buffer[readIndex] << 56) | ((ulong)buffer[readIndex + 1] << 48) | ((ulong)buffer[readIndex + 2] << 40) | ((ulong)buffer[readIndex + 3] << 32) | ((ulong)buffer[readIndex + 4] << 24) | ((ulong)buffer[readIndex + 5] << 16) | ((ulong)buffer[readIndex + 6] << 8) | ((ulong)buffer[readIndex + 7]);
                readIndex += 8;
                if (readIndex == buffer.Length)
                    readIndex = 0;
                return r;
            }

            return ((ulong)GetByte() << 56) | ((ulong)GetByte() << 48) | ((ulong)GetByte() << 40) | ((ulong)GetByte() << 32) | ((ulong)GetByte() << 24) | ((ulong)GetByte() << 16) | ((ulong)GetByte() << 8) | ((ulong)GetByte());
        }

        public int Crc32(int currentReadPositionOffset, int length) {
            throw new NotImplementedException();
        }

        public byte[] GetBytes(int count) {
            byte[] r = new byte[count];
            if(FullHead >= count) {
                Array.Copy(buffer, readIndex, r, 0, count);
                readIndex += count;
                if(readIndex == buffer.Length)
                    readIndex = 0;
            } else {
                int fullHead = FullHead;
                Array.Copy(buffer, readIndex, r, 0, fullHead);
                Array.Copy(buffer, 0, r, FullHead, count - fullHead);
                readIndex = count - fullHead;
            }
            return r;
        }
    }
}
