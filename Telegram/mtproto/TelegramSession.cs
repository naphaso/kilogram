using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Telegram.Core.Logging;
using Telegram.Model.Wrappers;
using Telegram.MTProto.Components;
using Telegram.MTProto.Crypto;
using Telegram.MTProto.Exceptions;
using Telegram.Utils;

namespace Telegram.MTProto {

    public interface ISession {
        ulong Id { get; }
        int GenerateSequence(bool confirmed);
    }

    public class TelegramSalt {
        private ulong value;
        private int validSince;
        private int validUntil;

        public TelegramSalt(ulong value, int validSince, int validUntil) {
            this.value = value;
            this.validSince = validSince;
            this.validUntil = validUntil;
        }

        public ulong Value { get { return value; } }
        
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

    public class TelegramFileSession : ISession {
        private ulong id;
        private int sequence;

        public TelegramFileSession(ulong id, int sequence) {
            this.id = id;
            this.sequence = sequence;
        }

        public TelegramFileSession(BinaryReader reader) {
            Read(reader);
        }

        public ulong Id {
            get { return id; }
        }
        public int GenerateSequence(bool confirmed) {
            lock (this) {
                return confirmed ? sequence++ * 2 + 1 : sequence * 2;
            }
        }

        public void Write(BinaryWriter writer) {
            writer.Write(id);
            writer.Write(sequence);
        }

        public void Read(BinaryReader reader) {
            id = reader.ReadUInt64();
            sequence = reader.ReadInt32();
        }
    }

    public class TelegramDC {
        private List<TelegramEndpoint> endpoints; 
        private AuthKey authKey;
        private TelegramFileSession fileSession;
        private Auth_authorizationConstructor fileAuthorization;
        // transient fields
        private MTProtoGateway fileGateway = null;


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

        SemaphoreSlim _lock = new SemaphoreSlim(1);
        public async Task<MTProtoGateway> GetFileGateway(ulong salt) {
            await _lock.WaitAsync();
//            fileGateway = new MTProtoGateway(this, fileSession);
            try {
                if (fileGateway != null) {
                    return fileGateway;
                }

                if (fileSession == null) {
                    fileSession = new TelegramFileSession(Helpers.GenerateRandomUlong(), 0);
                }

                fileGateway = new MTProtoGateway(this, fileSession, false, salt);
                await fileGateway.ConnectAsync();

                return fileGateway;
            }
            finally {
                _lock.Release();
            }
        }

        public bool FileAuthorized {
            get {
                return fileAuthorization != null;
            }
        }

        public void SaveFileAuthorization(auth_Authorization authorization) {
            this.fileAuthorization = (Auth_authorizationConstructor) authorization;
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

            //if(fileSession == null) {
            //    writer.Write(0);
            //} else {
            //    writer.Write(1);
            //    fileSession.Write(writer);
            //}

            if(fileAuthorization == null) {
                writer.Write(0);
            } else {
                writer.Write(1);
                fileAuthorization.Write(writer);
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

            //int fileSessionExists = reader.ReadInt32();
            //if(fileSessionExists == 0) {
            //    fileSession = null;
            //} else {
            //    fileSession = new TelegramFileSession(reader);
            //}

            int fileAuthExists = reader.ReadInt32();
            if(fileAuthExists == 0) {
                fileAuthorization = null;
            } else {
                fileAuthorization = (Auth_authorizationConstructor) TL.Parse<auth_Authorization>(reader);
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
        private int timeOffset;
        private Dictionary<int, TelegramDC> dcs;
        private Auth_authorizationConstructor authorization = null;
        private Dictionary<int, UserModel> users = null;
        private Dictionary<int, ChatModel> chats = null;

        private ulong cachedSalt;

        // transient
        public static TelegramSession instance = loadIfExists();

        private MTProtoGateway gateway = null;
        private volatile TLApi api = null;

        private Dialogs dialogs = null;
        private UpdatesProcessor updates = null;
        private Files files = null;
        private EncryptedChats encryptedChats;

        private Timer timer = null;
        
        public TelegramSession(BinaryReader reader) {
            read(reader);

            SubscribeToUpdates();
        }
        public TelegramSession(ulong id, int sequence) {
            this.id = id;
            this.sequence = sequence;
            dcs = new Dictionary<int, TelegramDC>();
            updates = new UpdatesProcessor(this);
            dialogs = new Dialogs(this);
            files = new Files(this);
            encryptedChats = new EncryptedChats(this);
            users = new Dictionary<int, UserModel>();
            chats = new Dictionary<int, ChatModel>();

            SubscribeToUpdates();
        }

        private void SubscribeToUpdates() {
            timer = new Timer(delegate {
                Deployment.Current.Dispatcher.BeginInvoke(() => dialogs.UpdateTypings());
            }, this, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            updates.UserStatusEvent += SetUserStatus;
            updates.UserTypingEvent += dialogs.SetUserTyping;
            updates.ChatTypingEvent += dialogs.SetChatTyping;
            updates.UserNameEvent += SetUserName;
            updates.UserPhotoEvent += SetUserPhoto;
            updates.MessagesReadEvent += dialogs.MessagesRead;
            updates.EncryptedChatEvent += encryptedChats.UpdateChatHandler;
            updates.EncryptedMessageEvent += dialogs.ReceiveMessage;
        }

        

        private void SetUserPhoto(int userId, int date, UserProfilePhoto photo, bool previous) {
            if(users.ContainsKey(userId)) {
                Deployment.Current.Dispatcher.BeginInvoke(() => users[userId].SetPhoto(photo));
            } else {
                logger.warning("update photo for unknown user {0}", userId);
            }
        }

        private void SetUserName(int userId, string firstName, string lastName) {
            if(users.ContainsKey(userId)) {
                Deployment.Current.Dispatcher.BeginInvoke(() => users[userId].SetName(firstName, lastName));
            } else {
                logger.warning("update name for unknown user {0} to {1} {2}", userId, firstName, lastName);
            }
        }

        private void SetUserStatus(int userId, UserStatus status) {
            if(users.ContainsKey(userId)) {
                Deployment.Current.Dispatcher.BeginInvoke(() => users[userId].SetUserStatus(status));
            } else {
                logger.warning("set user status {0} to unknown user {1}", status, userId);
            }
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

        private string _stateMarker;
        public string ContactsStateMarker {
            get {
                return _stateMarker ?? "";
            }
            set {
                _stateMarker = value;
            }
        }

        public Dictionary<int, TelegramDC> Dcs {
            get { return dcs; }
            set { dcs = value; }
        }

        public Dialogs Dialogs {
            get { return dialogs; }
        }

        public Files Files {
            get { return files; }
        }

        public void write(BinaryWriter writer) {
            logger.info("saving session...");
            writer.Write(id);
            writer.Write(sequence);
            writer.Write(mainDcId);
            writer.Write(timeOffset);
            writer.Write(gateway != null ? gateway.Salt : (ulong)0);
            writer.Write(dcs.Count);

            // contacts sync marker
            Serializers.String.write(writer, ContactsStateMarker);

            foreach(var dc in dcs) {
                writer.Write(dc.Key);
                dc.Value.write(writer);
            }

            if(authorization == null) {
                writer.Write(0);
            } else {
                writer.Write(1);
                authorization.Write(writer);
            }

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

            updates.Write(writer);
            dialogs.Write(writer);
            // files
            encryptedChats.Write(writer);

            logger.info("saving session complete");
        }

        public void read(BinaryReader reader) {
            logger.info("read session...");
            id = reader.ReadUInt64();
            sequence = reader.ReadInt32();
            mainDcId = reader.ReadInt32();
            timeOffset = reader.ReadInt32();
            cachedSalt = reader.ReadUInt64();
            int count = reader.ReadInt32();

            // contacts sync marker
            ContactsStateMarker = Serializers.String.read(reader);

            dcs = new Dictionary<int, TelegramDC>(count);
            for(int i = 0; i < count; i++) {
                int endpointId = reader.ReadInt32();
                dcs.Add(endpointId, new TelegramDC(reader));
            }

            int authorizationExists = reader.ReadInt32();
            if(authorizationExists != 0) {
                authorization = (Auth_authorizationConstructor) TL.Parse<auth_Authorization>(reader);
            }


            int usersCount = reader.ReadInt32();
            users = new Dictionary<int, UserModel>(usersCount + 10);
            for (int i = 0; i < usersCount; i++) {
                users.Add(reader.ReadInt32(), new UserModel(TL.Parse<User>(reader)));
            }

            int chatsCount = reader.ReadInt32();
            chats = new Dictionary<int, ChatModel>(chatsCount + 10);
            for (int i = 0; i < chatsCount; i++) {
                chats.Add(reader.ReadInt32(), new ChatModel(TL.Parse<Chat>(reader)));
            }

            logger.info("reading updates state....");
            updates = new UpdatesProcessor(this, reader);

            logger.info("reading dialogs...");
            dialogs = new Dialogs(this, reader);
            
            files = new Files(this);
            encryptedChats = new EncryptedChats(this, reader);

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
            lock(typeof(TelegramSession)) {
                try {
                    using(IsolatedStorageFile fileStorage = IsolatedStorageFile.GetUserStoreForApplication())
                    using(Stream fileStream = new IsolatedStorageFileStream("session.dat", FileMode.Open, fileStorage))
                    using(BinaryReader fileReader = new BinaryReader(fileStream)) {
                        session = new TelegramSession(fileReader);
                        logger.info("loaded telegram session: {0}", session);
                    }
                } catch(Exception e) {
                    logger.info("error loading session, create new...: {0}", e);
                    ulong sessionId = Helpers.GenerateRandomUlong();
                    session = new TelegramSession(sessionId, 0);
                    // prod 173.240.5.1 
                    // test 173.240.5.253
                    TelegramEndpoint endpoint = new TelegramEndpoint("173.240.5.1", 443);
                    TelegramDC dc = new TelegramDC();
                    dc.Endpoints.Add(endpoint);
                    session.Dcs.Add(1, dc);
                    session.SetMainDcId(1);


                    logger.info("created new telegram session: {0}", session);
                }
            }

            return session;
        }

        public void clear() {
            lock (typeof (TelegramSession)) {
                using (IsolatedStorageFile fileStorage = IsolatedStorageFile.GetUserStoreForApplication()) {
                    if (fileStorage.FileExists("/session.dat")) {
                        fileStorage.DeleteFile("/session.dat");
                    }
                }
                instance = loadIfExists();
            }
        }

        public void save() {
            logger.debug("Saving session instance");
            try {
                lock(typeof(TelegramSession)) {
                    using(IsolatedStorageFile fileStorage = IsolatedStorageFile.GetUserStoreForApplication()) {
                        using(Stream fileStream = new IsolatedStorageFileStream("session.dat.tmp", FileMode.OpenOrCreate, FileAccess.Write, fileStorage))
                        using(BinaryWriter fileWriter = new BinaryWriter(fileStream)) {
                            write(fileWriter);
                        }
                        
                        if(fileStorage.FileExists("/session.dat")) {
                            fileStorage.DeleteFile("/session.dat");
                        }

                        fileStorage.MoveFile("/session.dat.tmp", "/session.dat");
                    }
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

        // save timer
        private static bool saveSessionTimerInitialized = false;
        private static async Task SaveSessionTimer() {
            await Task.Delay(TimeSpan.FromSeconds(5));
            Instance.save();
            SaveSessionTimer();
        }
        // save timer end

        public async Task ConnectAsync() {
            try {
                if (gateway == null) {
                    logger.info("creating new mtproto gateway...");
                    gateway = new MTProtoGateway(MainDc, this, true, cachedSalt);
                    gateway.UpdatesEvent += updates.ProcessUpdates;
        
                    while (true) {
                        try {
                            await gateway.ConnectAsync();
                            break;
                        }
                        catch (MTProtoBrokenSessionException e) {
                            logger.info("creating new session... TODO: destroy old session");
                            // creating new session
                            id = Helpers.GenerateRandomUlong();
                            sequence = 0;
                            gateway.Dispose();
                            gateway = new MTProtoGateway(MainDc, this, true, cachedSalt);
                            gateway.UpdatesEvent += updates.ProcessUpdates;
                        }
                    }
                    api = new TLApi(gateway);
                    logger.info("connection established, notifying");
                    establishedTask.SetResult(null);

                    updates.RequestDifference();
                    gateway.ReconnectEvent += updates.RequestDifference;

                    if(!saveSessionTimerInitialized) {
                        SaveSessionTimer();
                        saveSessionTimerInitialized = true;
                    }
                }
            }
            catch (Exception ex) {
                logger.error("session exception: {0}", ex);
                throw ex;
            }
        }

        


        public async Task SaveAuthorization(auth_Authorization authorization) {
            this.authorization = (Auth_authorizationConstructor) authorization;
            Task dialogsTask = dialogs.DialogsRequest();
            Task updateTask = updates.GetStateRequest();
            await Task.WhenAll(dialogsTask, updateTask);
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

        public int SelfId {
            get {
                return ((UserSelfConstructor)authorization.user).id;
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

        public int TimeOffset {
            get {
                return timeOffset;
            }
            set {
                timeOffset = value;
            }
        }

        public EncryptedChats EncryptedChats {
            get {
                return encryptedChats;
            }
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
            ConnectAsync();
            await Established;
        }

        public async Task<TLApi> GetFileSessionMain() {
            return await GetFileSession(mainDcId);
        }
        public async Task<TLApi> GetFileSession(int dc) {
            logger.debug("Getting file session for dc {0}", dc);
            await Established;
            ConfigConstructor config = (ConfigConstructor) gateway.Config;

            TelegramDC targetDc;
            if(dcs.ContainsKey(dc)) {
                targetDc = dcs[dc];
            } else {
                targetDc = new TelegramDC();
                foreach(var dcOption in config.dc_options) {
                    DcOptionConstructor optionConstructor = (DcOptionConstructor) dcOption;
                    if(optionConstructor.id == dc) {
                        TelegramEndpoint endpoint = new TelegramEndpoint(optionConstructor.ip_address, optionConstructor.port);
                        targetDc.Endpoints.Add(endpoint);
                    }
                }

                dcs[dc] = targetDc;
            }
            
            MTProtoGateway fileGateway = await targetDc.GetFileGateway(gateway.Salt);
            
            TLApi fileGatewayApi = new TLApi(fileGateway);

            if(targetDc.FileAuthorized || dc == mainDcId) {
                return fileGatewayApi;
            } 

            Task<auth_ExportedAuthorization> exportAuthTask = Api.auth_exportAuthorization(dc);
            Auth_exportedAuthorizationConstructor exportedAuth = (Auth_exportedAuthorizationConstructor) await exportAuthTask;
            auth_Authorization authorization = await fileGatewayApi.auth_importAuthorization(exportedAuth.id, exportedAuth.bytes);
            targetDc.SaveFileAuthorization(authorization);

            return fileGatewayApi;
        }

        public UserModel GetUser(int id) {
            return users[id];
        }

        public ChatModel GetChat(int id) {
            return chats[id];
        }

        public void SaveUser(User user) {
                UserModel model = new UserModel(user);
                if (users.ContainsKey(model.Id)) {
                    Deployment.Current.Dispatcher.BeginInvoke(delegate {
                        users[model.Id].SetUser(user);
                    });
                }
                else {
                    users[model.Id] = model;
                }

        }

        public void SaveChat(Chat chat) {
            ChatModel model = new ChatModel(chat);
            if (chats.ContainsKey(model.Id)) {
                Deployment.Current.Dispatcher.BeginInvoke(delegate {
                    chats[model.Id].SetChat(chat);
                });
            }
            else {
                chats[model.Id] = model;
            }

        }
    }
}
