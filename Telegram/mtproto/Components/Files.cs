using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Core.Logging;
using Telegram.MTProto.Crypto;
using Telegram.MTProto.Exceptions;
using Telegram.Utils;

namespace Telegram.MTProto.Components {
    public delegate void FileUploadProcessHandler(float progress);
    public class Files {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(Files));

        private TelegramSession session;

        public Files(TelegramSession session) {
            this.session = session;
        }

        public async Task<string> GetFile(FileLocation fileLocation) {
            logger.debug("Getting file {0}", fileLocation);
            if(fileLocation.Constructor == Constructor.fileLocation) {
                FileLocationConstructor location = (FileLocationConstructor) fileLocation;
                string filePath = FileLocationToCachePath(location);
                using(IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication()) {
                    if(!storage.DirectoryExists("cache")) {
                        storage.CreateDirectory("cache");
                    }

                    if(storage.FileExists(filePath)) {
                        logger.debug("Getting file {0} from cache", filePath);
                        return filePath;
                    }

                    TLApi api = await session.GetFileSession(location.dc_id);
                    logger.debug("Got file session for dc {0}", location.dc_id);

                    Upload_fileConstructor file = (Upload_fileConstructor) await api.upload_getFile(TL.inputFileLocation(location.volume_id, location.local_id, location.secret), 0, int.MaxValue);

                    logger.debug("File constructor found");

                    using (Stream fileStream = new IsolatedStorageFileStream(filePath, FileMode.OpenOrCreate,
                                                                          FileAccess.Write, storage)) {
                        await fileStream.WriteAsync(file.bytes, 0, file.bytes.Length);
                    }

                    logger.debug("File saved successfully");
                    return filePath;
                }
            } else {
                throw new MTProtoFileUnavailableException();
            }
        }

        private string FileLocationToCachePath(FileLocationConstructor fileLocation) {
            return String.Format("cache/{0}.{1}.{2}.jpg", fileLocation.volume_id, fileLocation.local_id, fileLocation.secret);
        }

        public async Task<string> GetAvatar(FileLocation location) {
            return await Task.Run(() => GetFile(location));
        }

        // upload

        public async Task<InputFile> UploadFile(string filename, Stream stream, FileUploadProcessHandler handler) {
            TLApi api = await session.GetFileSessionMain();
            long fileId = Helpers.GenerateRandomLong();
            MD5 hash = new MD5();
            
            if(stream.Length < 128*1024) {
                handler(0.0f);
                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, (int) stream.Length);
                bool result = await api.upload_saveFilePart(fileId, 0, data);
                //while(result != true) {
                //    result = await api.upload_saveFilePart(fileId, 0, data);
                //}
                hash.Update(data);
                handler(1.0f);

                return TL.inputFile(fileId, 1, filename, hash.FinalString());
            }

            bool big = stream.Length > 10*1024*1024;
            float allStreamLength = stream.Length;
            int chunkSize = 128*1024;
            int chunkCount = (int) (stream.Length/chunkSize);
            int lastChunkSize = (int) (stream.Length - chunkSize*chunkCount);
            int allChunksCount = chunkCount + (lastChunkSize != 0 ? 1 : 0);

            for(int i = 0; i < chunkCount; i++) {
                handler((float) i*(float) chunkSize/allStreamLength);
                byte[] data = new byte[chunkSize];
                stream.Read(data, 0, chunkSize);
                bool result = big ? await api.upload_saveBigFilePart(fileId, i, allChunksCount, data) : await api.upload_saveFilePart(fileId, i, data);
                
                //while(result != true) {
                //    result = await api.upload_saveFilePart(fileId, i, data);
                //}
                hash.Update(data);
            }

            

            if(lastChunkSize != 0) {
                handler((float) chunkCount*(float) chunkSize/allStreamLength);
                byte[] lastChunkData = new byte[lastChunkSize];
                stream.Read(lastChunkData, 0, lastChunkSize);
                bool lastChunkResult = big ? await api.upload_saveBigFilePart(fileId, chunkCount, allChunksCount, lastChunkData) ? await api.upload_saveFilePart(fileId, chunkCount, lastChunkData);
                //while(lastChunkResult != true) {
                //    lastChunkResult = await api.upload_saveFilePart(fileId, chunkCount, lastChunkData);
                //}
                hash.Update(lastChunkData);
            }

            handler(1.0f);
            
            

            return TL.inputFile(fileId, allChunksCount, filename, hash.FinalString());
        }


    }
}
