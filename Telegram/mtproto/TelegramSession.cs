using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Telegram.Core.Logging;
using Telegram.MTProto.Crypto;

namespace Telegram.MTProto {

    public interface ISession {
        ulong Id { get; }
        int GenerateSequence(bool confirmed);
    }
    
    class TelegramEndpoint {
        private string host;
        private int port;

        public TelegramEndpoint(string host, int port) {
            this.host = host;
            this.port = port;
        }

        public TelegramEndpoint(BinaryReader reader) {
            read(reader);
        }

        public void read(BinaryReader reader) {
            host = Serializers.String.read(reader);
            port = reader.ReadInt32();
        }

        public void write(BinaryWriter writer) {
            Serializers.String.write(writer, host);
            writer.Write(port);
        }

        public string Host {
            get { return host; }
        }

        public int Port {
            get { return port; }
        }

        public override string ToString() {
            return string.Format("(Host: {0}, Port: {1})", host, port);
        }
    }
    class TelegramDC {
        private List<TelegramEndpoint> endpoints; 
        private AuthKey authKey;

        public TelegramDC(BinaryReader reader) {
            read(reader);
        }

        public TelegramDC() {
            endpoints = new List<TelegramEndpoint>();
            authKey = null;
        }

        public List<TelegramEndpoint> Endpoints {
            get { return endpoints; }
        }

        public AuthKey AuthKey {
            get { return authKey; }
            set { authKey = value; }
        }

        public void write(BinaryWriter writer) {
            writer.Write(endpoints.Count);
            foreach(var telegramEndpoint in endpoints) {
                telegramEndpoint.write(writer);
            }

            if(authKey == null) {
                writer.Write(0);
            } else {
                writer.Write(1);
                Serializers.Bytes.write(writer, authKey.Data);    
            }
        }

        public void read(BinaryReader reader) {
            int endpointCount = reader.ReadInt32();
            endpoints = new List<TelegramEndpoint>(endpointCount);
            for(int i = 0; i < endpointCount; i++) {
                endpoints.Add(new TelegramEndpoint(reader));
            }

            int keyExists = reader.ReadInt32();
            if(keyExists == 0) {
                authKey = null;
            } else {
                authKey = new AuthKey(Serializers.Bytes.read(reader));    
            }
        }

        public override string ToString() {
            string endpointsStr = endpoints.Aggregate("", (sum, kv) => string.Format("{0}, {1}", sum, kv));
            return string.Format("(Endpoints: [{0}], AuthKey: {1})", endpointsStr, authKey != null ? authKey.ToString() : "null");
        }
    }    
    public class TelegramSession : ISession {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(TelegramSession));
        private ulong id;
        private int sequence;
        private int mainDcId;
        private Dictionary<int, TelegramDC> dcs;
        
        // transient
        private MTProtoGateway gateway = null;
        private TLApi api = null;
        
        public TelegramSession(BinaryReader reader) {
            read(reader);
        }
        public TelegramSession(ulong id, int sequence) {
            this.id = id;
            this.sequence = sequence;
            dcs = new Dictionary<int, TelegramDC>();
        }

        public ulong Id {
            get { return id; }
        }

        public int Sequence {
            get { return sequence; }
        }

        public int GenerateSequence(bool confirmed) {
            lock(this) {
                return confirmed ? sequence++ * 2 + 1 : sequence * 2;
            }
        }

        public TelegramDC MainDc {
            get { return dcs[mainDcId]; }
        }

        public void SetMainDcId(int id) {
            mainDcId = id;
        }

        public Dictionary<int, TelegramDC> Dcs {
            get { return dcs; }
            set { dcs = value; }
        }

        public void write(BinaryWriter writer) {
            writer.Write(id);
            writer.Write(sequence);
            writer.Write(mainDcId);
            writer.Write(dcs.Count);
            foreach(var telegramEndpoint in dcs) {
                writer.Write(telegramEndpoint.Key);
                telegramEndpoint.Value.write(writer);
            }
        }

        public void read(BinaryReader reader) {
            id = reader.ReadUInt64();
            sequence = reader.ReadInt32();
            mainDcId = reader.ReadInt32();
            int count = reader.ReadInt32();
            dcs = new Dictionary<int, TelegramDC>(count);
            for(int i = 0; i < count; i++) {
                int endpointId = reader.ReadInt32();
                dcs.Add(endpointId, new TelegramDC(reader));
            }
        }

        public byte[] serialize() {
            using(MemoryStream memory = new MemoryStream())
            using(BinaryWriter writer = new BinaryWriter(memory)) {
                write(writer);
                return memory.ToArray();
            }
        }

        public void deserialize(byte[] data) {
            using(MemoryStream memory = new MemoryStream(data, false))
            using(BinaryReader reader = new BinaryReader(memory)) {
                read(reader);
            }
        }

        private static TelegramSession loadIfExists() {
            TelegramSession session;
            try {
                using(IsolatedStorageFile fileStorage = IsolatedStorageFile.GetUserStoreForApplication())
                using(Stream fileStream = new IsolatedStorageFileStream("session.dat", FileMode.Open, fileStorage))
                using(BinaryReader fileReader = new BinaryReader(fileStream)) {
                    session = new TelegramSession(fileReader);
                    logger.info("loaded telegram session: {0}", session);
                }
            } catch(Exception e) {
                logger.info("error loading session, create new...");
                Random random = new Random();
                ulong sessionId = (((ulong) random.Next()) << 32) | ((ulong) random.Next());
                session = new TelegramSession(sessionId, 0);
                TelegramEndpoint endpoint = new TelegramEndpoint("173.240.5.253", 443);
                TelegramDC dc = new TelegramDC();
                dc.Endpoints.Add(endpoint);
                session.Dcs.Add(1, dc);
                session.SetMainDcId(1);

                logger.info("created new telegram session: {0}", session);
            }

            return session;
        }

        public void save() {
            try {
                lock(typeof(TelegramSession))
                    using(IsolatedStorageFile fileStorage = IsolatedStorageFile.GetUserStoreForApplication())
                    using(
                        Stream fileStream = new IsolatedStorageFileStream("session.dat", FileMode.OpenOrCreate,
                                                                          FileAccess.Write, fileStorage))
                    using(BinaryWriter fileWriter = new BinaryWriter(fileStream)) {
                        write(fileWriter);
                    }
            } catch(Exception e) {
                logger.info("failed to save session: {0}", e);
            }
        }

        public override string ToString() {
            try {
                string dcsStr = dcs.Aggregate("", (sum, kv) => string.Format("{0}, {1} => {2}", sum, kv.Key, kv.Value));
                return string.Format("(Id: {0}, Sequence: {1}, MainDcId: {2}, Dcs: [{3}])", id, sequence, mainDcId,
                                     dcsStr);
            } catch(Exception e) {
                logger.info("some error: {0}", e);
                return "WTF";
            }
        }

        public static TelegramSession instance = loadIfExists();
        public static TelegramSession Instance {
            get {
                return instance;
            }
        }

        public async Task ConnectAsync() {
            if(gateway == null) {
                logger.info("creating new mtproto gateway...");
                gateway = new MTProtoGateway(MainDc, this);
                await gateway.ConnectAsync();
                api = new TLApi(gateway);
            }
        }

        
        
        public TLApi Api {
            get { return api; }
        }
    }
}
