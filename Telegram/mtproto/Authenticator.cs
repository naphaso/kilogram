using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Phone.Logging;
using Telegram.Core.Logging;
using Telegram.mtproto.Crypto;
using Telegram.MTProto.Crypto;
using Logger = Telegram.Core.Logging.Logger;
using RSA = Telegram.MTProto.Crypto.RSA;

namespace Telegram.MTProto {
    internal class Authenticator : MTProtoPlainClient {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(Authenticator));
        //private AutoResetEvent pendingEvent = new AutoResetEvent(false);
        private TaskCompletionSource<byte[]> completionSource; 
        //private byte[] response;
        private byte[] nonce = new byte[16];
        private byte[] serverNonce;
        private byte[] newNonce = new byte[32];
        private Random random = new Random();

        private int timeOffset;

        public async Task<AuthKey> Generate(TelegramDC dc, int maxRetries) {    
            ConnectedEvent += delegate {};
            await ConnectAsync(dc, maxRetries);

            

            random.NextBytes(nonce);

            using(MemoryStream memoryStream = new MemoryStream()) {
                using(BinaryWriter binaryWriter = new BinaryWriter(memoryStream)) {
                    binaryWriter.Write(0x60469778);
                    binaryWriter.Write(nonce);
                    Send(memoryStream.ToArray());
                }
            }

            completionSource = new TaskCompletionSource<byte[]>();
            byte[] response = await completionSource.Task;

            BigInteger pq;
            List<byte[]> fingerprints = new List<byte[]>();

            using(var memoryStream = new MemoryStream(response, false)) {
                using(var binaryReader = new BinaryReader(memoryStream)) {
                    int responseCode = binaryReader.ReadInt32();
                    if(responseCode != 0x05162463) {
                        logger.error("invalid response code: {0}", responseCode);
                        return null;
                    }


                    byte[] nonceFromServer = binaryReader.ReadBytes(16);
                    if(!nonceFromServer.SequenceEqual(nonce)) {
                        logger.debug("invalid nonce from server");
                        return null;
                    }


                    serverNonce = binaryReader.ReadBytes(16);

                    byte[] pqbytes = Serializers.Bytes.read(binaryReader);
                    pq = new BigInteger(1, pqbytes);

                    int vectorId = binaryReader.ReadInt32();

                    if(vectorId != 0x1cb5c415) {
                        logger.debug("invalid fingerprints vector id: {0}", vectorId);
                        return null;
                    }

                    int fingerprintCount = binaryReader.ReadInt32();
                    for(int i = 0; i < fingerprintCount; i++) {
                        byte[] fingerprint = binaryReader.ReadBytes(8);
                        fingerprints.Add(fingerprint);
                    }

                }
            }

            FactorizedPair pqPair = Factorizator.Factorize(pq);

            logger.debug("stage 1: ok");

            random.NextBytes(newNonce);

            byte[] reqDhParamsBytes;

            using(MemoryStream pqInnerData = new MemoryStream(255)) {
                using(BinaryWriter pqInnerDataWriter = new BinaryWriter(pqInnerData)) {
                    pqInnerDataWriter.Write(0x83c95aec); // pq_inner_data
                    Serializers.Bytes.write(pqInnerDataWriter, pq.ToByteArrayUnsigned());
                    Serializers.Bytes.write(pqInnerDataWriter, pqPair.Min.ToByteArrayUnsigned());
                    Serializers.Bytes.write(pqInnerDataWriter, pqPair.Max.ToByteArrayUnsigned());
                    pqInnerDataWriter.Write(nonce);
                    pqInnerDataWriter.Write(serverNonce);
                    pqInnerDataWriter.Write(newNonce);

                    logger.debug("pq_inner_data: {0}", BitConverter.ToString(pqInnerData.GetBuffer()));

                    byte[] ciphertext = null;
                    byte[] targetFingerprint = null;
                    foreach(byte[] fingerprint in fingerprints) {
                        ciphertext = RSA.Encrypt(BitConverter.ToString(fingerprint).Replace("-", string.Empty),
                                                 pqInnerData.GetBuffer(), 0, (int) pqInnerData.Position);
                        if(ciphertext != null) {
                            targetFingerprint = fingerprint;
                            break;
                        }
                    }

                    if(ciphertext == null) {
                        logger.error("not found valid key for fingerprints: {0}", String.Join(", ", fingerprints));
                        return null;
                    }

                    using(MemoryStream reqDHParams = new MemoryStream(1024)) {
                        using(BinaryWriter reqDHParamsWriter = new BinaryWriter(reqDHParams)) {
                            reqDHParamsWriter.Write(0xd712e4be); // req_dh_params
                            reqDHParamsWriter.Write(nonce);
                            reqDHParamsWriter.Write(serverNonce);
                            Serializers.Bytes.write(reqDHParamsWriter, pqPair.Min.ToByteArrayUnsigned());
                            Serializers.Bytes.write(reqDHParamsWriter, pqPair.Max.ToByteArrayUnsigned());
                            reqDHParamsWriter.Write(targetFingerprint);
                            Serializers.Bytes.write(reqDHParamsWriter, ciphertext);

                            logger.debug("sending req_dh_paras: {0}", BitConverter.ToString(reqDHParams.ToArray()));
                            reqDhParamsBytes = reqDHParams.ToArray();
                        }
                    }
                }
            }

            completionSource = new TaskCompletionSource<byte[]>();
            Send(reqDhParamsBytes);
            response = await completionSource.Task;

            logger.debug("dh response: {0}", BitConverter.ToString(response));

            byte[] encryptedAnswer;

            using(MemoryStream responseStream = new MemoryStream(response, false)) {
                using(BinaryReader responseReader = new BinaryReader(responseStream)) {
                    uint responseCode = responseReader.ReadUInt32();

                    if(responseCode == 0x79cb045d) {
                        // server_DH_params_fail
                        logger.error("server_DH_params_fail: TODO");
                        return null;
                    }

                    if(responseCode != 0xd0e8075c) {
                        logger.error("invalid response code: {0}", responseCode);
                        return null;
                    }

                    byte[] nonceFromServer = responseReader.ReadBytes(16);
                    if(!nonceFromServer.SequenceEqual(nonce)) {
                        logger.debug("invalid nonce from server");
                        return null;
                    }

                    byte[] serverNonceFromServer = responseReader.ReadBytes(16);
                    if(!serverNonceFromServer.SequenceEqual(serverNonce)) {
                        logger.error("invalid server nonce from server");
                        return null;
                    }

                    encryptedAnswer = Serializers.Bytes.read(responseReader);
                }
            }

            logger.debug("encrypted answer: {0}", BitConverter.ToString(encryptedAnswer));

            AESKeyData key = AES.GenerateKeyDataFromNonces(serverNonce, newNonce);
            byte[] plaintextAnswer = AES.DecryptAES(key, encryptedAnswer);

            logger.debug("plaintext answer: {0}", BitConverter.ToString(plaintextAnswer));

            int g;
            BigInteger dhPrime;
            BigInteger ga;

            using(MemoryStream dhInnerData = new MemoryStream(plaintextAnswer)) {
                using(BinaryReader dhInnerDataReader = new BinaryReader(dhInnerData)) {
                    byte[] hashsum = dhInnerDataReader.ReadBytes(20);
                    uint code = dhInnerDataReader.ReadUInt32();
                    if(code != 0xb5890dba) {
                        logger.error("invalid dh_inner_data code: {0}", code);
                        return null;
                    }

                    logger.debug("valid code");

                    byte[] nonceFromServer1 = dhInnerDataReader.ReadBytes(16);
                    if(!nonceFromServer1.SequenceEqual(nonce)) {
                        logger.error("invalid nonce in encrypted answer");
                        return null;
                    }

                    logger.debug("valid nonce");

                    byte[] serverNonceFromServer1 = dhInnerDataReader.ReadBytes(16);
                    if(!serverNonceFromServer1.SequenceEqual(serverNonce)) {
                        logger.error("invalid server nonce in encrypted answer");
                        return null;
                    }

                    logger.debug("valid server nonce");

                    g = dhInnerDataReader.ReadInt32();
                    dhPrime = new BigInteger(1, Serializers.Bytes.read(dhInnerDataReader));
                    ga = new BigInteger(1, Serializers.Bytes.read(dhInnerDataReader));

                    int serverTime = dhInnerDataReader.ReadInt32();
                    timeOffset = serverTime - (int)(Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds) / 1000);

                    logger.debug("g: {0}, dhprime: {1}, ga: {2}", g, dhPrime, ga);
                }
            }

            BigInteger b = new BigInteger(2048, random);
            BigInteger gb = BigInteger.ValueOf(g).ModPow(b, dhPrime);
            BigInteger gab = ga.ModPow(b, dhPrime);

            logger.debug("gab: {0}", gab);

            // prepare client dh inner data
            byte[] clientDHInnerDataBytes;
            using(MemoryStream clientDhInnerData = new MemoryStream()) {
                using(BinaryWriter clientDhInnerDataWriter = new BinaryWriter(clientDhInnerData)) {
                    clientDhInnerDataWriter.Write(0x6643b654); // client_dh_inner_data
                    clientDhInnerDataWriter.Write(nonce);
                    clientDhInnerDataWriter.Write(serverNonce);
                    clientDhInnerDataWriter.Write((long) 0); // TODO: retry_id
                    Serializers.Bytes.write(clientDhInnerDataWriter, gb.ToByteArrayUnsigned());

                    using(MemoryStream clientDhInnerDataWithHash = new MemoryStream()) {
                        using(BinaryWriter clientDhInnerDataWithHashWriter = new BinaryWriter(clientDhInnerDataWithHash)) {
                            using(SHA1 sha1 = new SHA1Managed()) {
                                clientDhInnerDataWithHashWriter.Write(sha1.ComputeHash(clientDhInnerData.GetBuffer(), 0, (int)clientDhInnerData.Position));
                                clientDhInnerDataWithHashWriter.Write(clientDhInnerData.GetBuffer(), 0, (int)clientDhInnerData.Position);
                                clientDHInnerDataBytes = clientDhInnerDataWithHash.ToArray();
                            }
                        }
                    }
                }
            }

            logger.debug("client dh inner data papared len {0}: {1}", clientDHInnerDataBytes.Length, BitConverter.ToString(clientDHInnerDataBytes).Replace("-",""));

            // encryption
            byte[] clientDhInnerDataEncryptedBytes = AES.EncryptAES(key, clientDHInnerDataBytes);

            logger.debug("inner data encrypted {0}: {1}", clientDhInnerDataEncryptedBytes.Length, BitConverter.ToString(clientDhInnerDataEncryptedBytes).Replace("-",""));

            // prepare set_client_dh_params
            byte[] setclientDhParamsBytes;
            using(MemoryStream setClientDhParams = new MemoryStream()) {
                using(BinaryWriter setClientDhParamsWriter = new BinaryWriter(setClientDhParams)) {
                    setClientDhParamsWriter.Write(0xf5045f1f);
                    setClientDhParamsWriter.Write(nonce);
                    setClientDhParamsWriter.Write(serverNonce);
                    Serializers.Bytes.write(setClientDhParamsWriter, clientDhInnerDataEncryptedBytes);

                    setclientDhParamsBytes = setClientDhParams.ToArray();
                }
            }

            logger.debug("set client dh params prepared: {0}", BitConverter.ToString(setclientDhParamsBytes));


            completionSource = new TaskCompletionSource<byte[]>();
            Send(setclientDhParamsBytes);
            response = await completionSource.Task;

            using(MemoryStream responseStream = new MemoryStream(response)) {
                using(BinaryReader responseReader = new BinaryReader(responseStream)) {
                    uint code = responseReader.ReadUInt32();
                    if(code == 0x3bcbf734) { // dh_gen_ok
                        logger.debug("dh_gen_ok");

                        byte[] nonceFromServer = responseReader.ReadBytes(16);
                        if(!nonceFromServer.SequenceEqual(nonce)) {
                            logger.error("invalid nonce");
                            return null;
                        }

                        byte[] serverNonceFromServer = responseReader.ReadBytes(16);

                        if(!serverNonceFromServer.SequenceEqual(serverNonce)) {
                            logger.error("invalid server nonce");
                            return null;
                        }

                        byte[] newNonceHash1 = responseReader.ReadBytes(16);
                        logger.debug("new nonce hash 1: {0}", BitConverter.ToString(newNonceHash1));

                        AuthKey authKey = new AuthKey(gab);

                        byte[] newNonceHashCalculated = authKey.CalcNewNonceHash(newNonce, 1);

                        if(!newNonceHash1.SequenceEqual(newNonceHashCalculated)) {
                            logger.error("invalid new nonce hash");
                            return null;
                        }

                        logger.info("generated new auth key: {0}", gab);
                        logger.info("saving time offset: {0}", timeOffset);
                        TelegramSettings.Instance.TimeOffset = timeOffset;
                        return authKey;
                    }
                    else if(code == 0x46dc1fb9) { // dh_gen_retry
                        logger.debug("dh_gen_retry");
                        return null;
                    }
                    else if(code == 0xa69dae02) {
                        // dh_gen_fail
                        logger.debug("dh_gen_fail");
                        return null;
                    } else {
                        logger.debug("dh_gen unknown: {0}", code);
                        return null;
                    }
                }
            }
        }

        protected override void OnMTProtoReceive(byte[] response) {
            completionSource.SetResult(response);
        }
    }
}