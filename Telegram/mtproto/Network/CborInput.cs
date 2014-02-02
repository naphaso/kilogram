namespace Telegram.MTProto.Network {
    public delegate void InputHandler();
    public interface CborInput {
        event InputHandler InputEvent;
        void AddChunk(byte[] chunk);
        void AddChunk(byte[] chunk, int offset, int length);
        bool HasBytes(int count);
        int GetByte();
        uint GetInt8();
        uint GetInt16();
        uint GetInt32();
        uint ReadInt32();
        ulong GetInt64();
        int Crc32(int currentReadPositionOffset, int length);
        byte[] GetBytes(int count);

        void Clear();
    }
}
