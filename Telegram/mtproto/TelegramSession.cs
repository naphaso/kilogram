using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Telegram.Core.Logging;
using Telegram.Model.Wrappers;
using Telegram.MTProto.Components;
using Telegram.MTProto.Crypto;
using Telegram.MTProto.Exceptions;

namespace Telegram.MTProto {

    public interface ISession {
        ulong Id { get; }
        int GenerateSequence(bool confirmed);
    }
    
    public class TelegramEndpoint {
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
    public class TelegramDC {
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
        private Auth_authorizationConstructor authorization = null;
        private Dictionary<int, UserModel> users = null;
        private Dictionary<int, ChatModel> chats = null; 
        
        // transient
        public static TelegramSession instance = loadIfExists();

        private MTProtoGateway gateway = null;
        private TLApi api = null;

        private Dialogs dialogs = null;
        private UpdatesProcessor updates = null;
        
        public TelegramSession(BinaryReader reader) {
            read(reader);
        }
        public TelegramSession(ulong id, int sequence) {
            this.id = id;
            this.sequence = sequence;
            dcs = new Dictionary<int, TelegramDC>();
            updates = new UpdatesProcessor(this);
            dialogs = new Dialogs(this);
            users = new Dictionary<int, UserModel>();
            chats = new Dictionary<int, ChatModel>();
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

        public Dialogs Dialogs {
            get { return dialogs; }
        }

        public void write(BinaryWriter writer) {
            logger.info("saving session...");
            writer.Write(id);
            writer.Write(sequence);
            writer.Write(mainDcId);
            writer.Write(dcs.Count);
            foreach(var telegramEndpoint in dcs) {
                writer.Write(telegramEndpoint.Key);
                telegramEndpoint.Value.write(writer);
            }

            if(authorization == null) {
                writer.Write(0);
            } else {
                writer.Write(1);
                authorization.Write(writer);
            }

            dialogs.save(writer);

            writer.Write(users.Count);
            foreach (var userModel in users) {
                writer.Write(userModel.Key);
                userModel.Value.RawUser.Write(writer);
            }

            writer.Write(chats.Count);
            foreach (var chatModel in chats) {
                writer.Write(chatModel.Key);
                chatModel.Value.RawChat.Write(writer);
            }
            logger.info("saving session complete");
        }

        public void read(BinaryReader reader) {
            logger.info("read session...");
            id = reader.ReadUInt64();
            sequence = reader.ReadInt32();
            mainDcId = reader.ReadInt32();
            int count = reader.ReadInt32();
            dcs = new Dictionary<int, TelegramDC>(count);
            for(int i = 0; i < count; i++) {
                int endpointId = reader.ReadInt32();
                dcs.Add(endpointId, new TelegramDC(reader));
            }

            int authorizationExists = reader.ReadInt32();
            if(authorizationExists != 0) {
                authorization = new Auth_authorizationConstructor();
                reader.ReadUInt32();
                authorization.Read(reader);
            }
            updates = new UpdatesProcessor(this);
            logger.info("reading dialogs...");
            dialogs = new Dialogs(this, reader);

            int usersCount = reader.ReadInt32();
            users = new Dictionary<int, UserModel>(usersCount + 10);
            for(int i = 0; i < usersCount; i++) {
                users.Add(reader.ReadInt32(), new UserModel(TL.Parse<User>(reader)));
            }

            int chatsCount = reader.ReadInt32();
            chats = new Dictionary<int, ChatModel>(chatsCount + 10);
            for(int i = 0; i < chatsCount; i++) {
                chats.Add(reader.ReadInt32(), new ChatModel(TL.Parse<Chat>(reader)));
            }
            logger.info("session readed complete");
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
                logger.info("error loading session, create new...: {0}", e);
                Random random = new Random();
                ulong sessionId = (((ulong) random.Next()) << 32) | ((ulong) random.Next());
                session = new TelegramSession(sessionId, 0);
                TelegramEndpoint endpoint = new TelegramEndpoint("173.240.5.1", 443);
                TelegramDC dc = new TelegramDC();
                dc.Endpoints.Add(endpoint);
                session.Dcs.Add(1, dc);
                session.SetMainDcId(1);

                logger.info("created new telegram session: {0}", session);
            }

            return session;
        }

        public void save() {
            logger.debug("Saving session instance");
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

        
        public static TelegramSession Instance {
            get {
                return instance;
            }
        }

        private TaskCompletionSource<object> establishedTask = new TaskCompletionSource<object>();

        public Task Established {
            get {
                return establishedTask.Task;
            }
        }

        public async Task ConnectAsync() {
            if(gateway == null) {
                logger.info("creating new mtproto gateway...");
                gateway = new MTProtoGateway(MainDc, this);
                gateway.UpdatesEvent += updates.ProcessUpdates;
                while(true) {
                    try {
                        await gateway.ConnectAsync();
                        break;
                    } catch(MTProtoBrokenSessionException e) {
                        logger.info("creating new session... TODO: destroy old session");
                        // creating new session
                        Random random = new Random();
                        id = (((ulong) random.Next()) << 32) | ((ulong) random.Next());
                        sequence = 0;
                        gateway.Dispose();
                        gateway = new MTProtoGateway(MainDc, this);
                        gateway.UpdatesEvent += updates.ProcessUpdates;
                    }
                }
                api = new TLApi(gateway);

                establishedTask.SetResult(null);
            }
        }


        public async Task SaveAuthorization(auth_Authorization authorization) {
            this.authorization = (Auth_authorizationConstructor) authorization;
            await dialogs.DialogsRequest();
            save();
        }

        public bool AuthorizationExists() {
            return authorization != null;
        }

        public Peer SelfPeer {
            get {
                return TL.peerUser(((UserSelfConstructor) authorization.user).id);
            }
        }

        public UpdatesProcessor Updates {
            get {
                return updates;
            }
        }
        
        public TLApi Api {
            get { return api; }
        }

        public async Task Migrate(int dc) {
            if(gateway == null) {
                logger.error("gateway not found, migration impossible");
                return;
            }

            if (gateway.Config == null) {
                logger.error("config in gateway not found, migration impossible");
                return;
            }

            ConfigConstructor config = (ConfigConstructor) gateway.Config;
            if(config.this_dc == dc) {
                logger.warning("migration to same dc: {0}", dc);
                return;
            }

            TelegramDC newDc = new TelegramDC();
            foreach(var dcOption in config.dc_options) {
                DcOptionConstructor optionConstructor = (DcOptionConstructor) dcOption;
                if(optionConstructor.id == dc) {
                    TelegramEndpoint endpoint = new TelegramEndpoint(optionConstructor.ip_address, optionConstructor.port);
                    newDc.Endpoints.Add(endpoint);
                }
            }

            dcs[dc] = newDc;
            mainDcId = dc;

            gateway.Dispose();
            gateway = null;
            establishedTask = new TaskCompletionSource<object>();
            await ConnectAsync();
        }

        public UserModel GetUser(int id) {
            return users[id];
        }

        public ChatModel GetChat(int id) {
            return chats[id];
        }

        public void SaveUser(User user) {
            UserModel model = new UserModel(user);
            if(users.ContainsKey(model.Id)) {
                users[model.Id].SetUser(user);
            } else {
                users[model.Id] = model;    
            }
        }

        public void SaveChat(Chat chat) {
            ChatModel model = new ChatModel(chat);
            if(chats.ContainsKey(model.Id)) {
                chats[model.Id].SetChat(chat);
            } else {
                chats[model.Id] = model;
            }
        }
    }
}
