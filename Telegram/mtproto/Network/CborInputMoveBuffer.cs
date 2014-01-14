using System;
using System.Linq;
using Ionic.Crc;
using Telegram.Core.Logging;
using Telegram.MTProto.Crypto;

namespace Telegram.MTProto.Network {
    class CborInputMoveBuffer : CborInput {
        public event InputHandler InputEvent;
        private byte[] buffer;
        private int readIndex;
        private int writeIndex;

        public CborInputMoveBuffer(int size) {
            this.buffer = new byte[size];
            readIndex = 0;
            writeIndex = 0;
        }



        public void AddChunk(byte[] chunk) {
            lock(this) {
                if(buffer.Length - writeIndex < chunk.Length) {
                    if(writeIndex - readIndex != 0) {
                        Array.Copy(buffer, readIndex, buffer, 0, writeIndex - readIndex);
                        writeIndex = writeIndex - readIndex;
                        readIndex = 0;
                    } else {
                        readIndex = 0;
                        writeIndex = 0;
                    }
                }

                Array.Copy(chunk, 0, buffer, writeIndex, chunk.Length);
                writeIndex += chunk.Length;
                InputEvent();
            }
        }

        public void AddChunk(byte[] chunk, int offset, int length) {
            lock(this) {
                if(buffer.Length - writeIndex < length) {
                    if(writeIndex - readIndex != 0) {
                        Array.Copy(buffer, readIndex, buffer, 0, writeIndex - readIndex);
                        writeIndex = writeIndex - readIndex;
                        readIndex = 0;
                    } else {
                        readIndex = 0;
                        writeIndex = 0;
                    }
                }

                Array.Copy(chunk, offset, buffer, writeIndex, length);
                writeIndex += length;
                InputEvent();
            }
        }

        public bool HasBytes(int count) {
            return count <= writeIndex - readIndex;
        }

        public int GetByte() {
            return buffer[readIndex++];
        }

        public uint GetInt8() {
            return buffer[readIndex++];
        }

        public uint GetInt16() {
            readIndex += 2;
            return ((uint)buffer[readIndex - 1] << 8) | ((uint)buffer[readIndex - 2]);
        }

        public uint GetInt32() {
            uint r = ((uint)buffer[readIndex + 3] << 24) | ((uint)buffer[readIndex + 2] << 16) | ((uint)buffer[readIndex + 1] << 8) | ((uint)buffer[readIndex]);
            readIndex += 4;
            return r;
        }

        public uint ReadInt32() {
            return ((uint)buffer[readIndex + 3] << 24) | ((uint)buffer[readIndex + 2] << 16) | ((uint)buffer[readIndex + 1] << 8) | ((uint)buffer[readIndex]);
        }

        public ulong GetInt64() {
            readIndex += 8;
            return ((ulong)buffer[readIndex - 1] << 56) | ((ulong)buffer[readIndex - 2] << 48) | ((ulong)buffer[readIndex - 3] << 40) | ((ulong)buffer[readIndex - 4] << 32) | ((ulong)buffer[readIndex - 5] << 24) | ((ulong)buffer[readIndex - 6] << 16) | ((ulong)buffer[readIndex - 7] << 8) | ((ulong)buffer[readIndex - 8]);
        }

        public int Crc32(int currentReadPositionOffset, int length) {
            //Crc32 crc32 = new Crc32();
            //logger.info("calc crc32 offset {0}, length {1} on: {2}", currentReadPositionOffset, length, BitConverter.ToString(buffer, readIndex + currentReadPositionOffset, length));
            //return crc32.ComputeHash(buffer, readIndex + currentReadPositionOffset, length).Reverse().ToArray();
            CRC32 crc32 = new CRC32();
            crc32.SlurpBlock(buffer, readIndex+currentReadPositionOffset, length);
            return crc32.Crc32Result;
        }

        public byte[] GetBytes(int count) {
            byte[] data = new byte[count];
            Array.Copy(buffer, readIndex, data, 0, count);
            readIndex += count;
            return data;
        }
    }
}
