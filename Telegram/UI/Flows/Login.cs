using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Core.Logging;
using Telegram.MTProto;
using Telegram.MTProto.Exceptions;

namespace Telegram.UI.Flows {

    public delegate void LoginSignupHandler(Login login);

    public delegate void LoginCodeHandler(Login login);

    public delegate void LoginWrongCodeHandler(Login login);

    public delegate void LoginLoginSuccess(Login login);

    public class Login {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(Login));

        private TelegramSession session;
        private string langCode;
        private TaskCompletionSource<string> phoneSource = new TaskCompletionSource<string>();
        private TaskCompletionSource<string> codeSource = new TaskCompletionSource<string>();
        private TaskCompletionSource<SignUpData> signupSource = new TaskCompletionSource<SignUpData>(); 
        public Login(TelegramSession session, string langCode) {
            this.langCode = langCode;
            this.session = session;
        }

        public event LoginCodeHandler NeedCodeEvent;
        public event LoginSignupHandler NeedSignupEvent;
        public event LoginWrongCodeHandler WrongCodeEvent;
        public event LoginLoginSuccess LoginSuccessEvent;

        public async Task Start() {
            await session.ConnectAsync();

            string phone = await phoneSource.Task;

            Auth_sentCodeConstructor sendCodeResponse = null;

            for(int i = 0; i < 5; i++) {
                // 5 migration tries
                bool sendCodeSuccess;
                int migrateDc = -1;
                try {
                    sendCodeResponse = (Auth_sentCodeConstructor) await session.Api.auth_sendCode(phone, 0, 1097, "712986b054dc1311bec3c2dd92e843e7", langCode);
                    sendCodeSuccess = true;
                } catch(MTProtoErrorException e) {
                    logger.warning("connect exception: {0}", e);
                    sendCodeSuccess = false;
                    if (e.ErrorMessage.StartsWith("PHONE_MIGRATE_") || e.ErrorMessage.StartsWith("NETWORK_MIGRATE_")) {
                        migrateDc = Convert.ToInt32(e.ErrorMessage.Replace("PHONE_MIGRATE_", "").Replace("NETWORK_MIGRATE_", ""), 10);
                    }
                }

                if(sendCodeSuccess) {
                    break;
                } else if(migrateDc != -1) {
                    await session.Migrate(migrateDc);
                }
            }

            if(sendCodeResponse == null) {
                logger.error("login failed");
                return;
            }

            if(sendCodeResponse.phone_registered) { // sign in
                string code;

                NeedCodeEvent(this);
                while (true) {

                    // wait 30 seconds and send phone call
                    if(await Task.WhenAny(codeSource.Task, Task.Delay(TimeSpan.FromSeconds(60))) == codeSource.Task) {
                        code = codeSource.Task.Result;
                    } else {
                        session.Api.auth_sendCall(phone, sendCodeResponse.phone_code_hash);
                        code = await codeSource.Task;
                    }

                    try {
                        Auth_authorizationConstructor authorization = (Auth_authorizationConstructor)await session.Api.auth_signIn(phone, sendCodeResponse.phone_code_hash, code);
                        session.SaveAuthorization(authorization);
                        LoginSuccessEvent(this);
                        break;
                    } catch(MTProtoErrorException e) {
                        codeSource = new TaskCompletionSource<string>();
                        WrongCodeEvent(this);
                    }
                }

            } else { // sign up
                string code;
                NeedCodeEvent(this);

                // wait 30 seconds and send phone call
                if (await Task.WhenAny(codeSource.Task, Task.Delay(TimeSpan.FromSeconds(60))) == codeSource.Task) {
                    code = codeSource.Task.Result;
                }
                else {
                    session.Api.auth_sendCall(phone, sendCodeResponse.phone_code_hash);
                    code = await codeSource.Task;
                }

                while(true) {
                    try {
                        await session.Api.auth_signIn(phone, sendCodeResponse.phone_code_hash, code);
                        codeSource = new TaskCompletionSource<string>();
                        WrongCodeEvent(this);
                    } catch(MTProtoErrorException e) {
                        if(e.ErrorMessage.StartsWith("PHONE_NUMBER_UNOCCUPIED")) {
                            // Код верен, но пользователя с таким номером телефона не существует
                            break;
                        } else if(e.ErrorMessage.StartsWith("PHONE_CODE_INVALID")) {
                            codeSource = new TaskCompletionSource<string>();
                            WrongCodeEvent(this);
                        } else if(e.ErrorMessage.StartsWith("PHONE_CODE_EXPIRED")) {
                            codeSource = new TaskCompletionSource<string>();
                            WrongCodeEvent(this);
                        } else if(e.ErrorMessage.StartsWith("PHONE_CODE_EMPTY")) {
                            codeSource = new TaskCompletionSource<string>();
                            WrongCodeEvent(this);
                        }
                    }
                }

                NeedSignupEvent(this);
                SignUpData signUpData = await signupSource.Task;

                while(true) {
                    try {
                        Auth_authorizationConstructor authorization = (Auth_authorizationConstructor) await session.Api.auth_signUp(phone, sendCodeResponse.phone_code_hash, code, signUpData.firstname, signUpData.lastname);
                        session.SaveAuthorization(authorization);

                        LoginSuccessEvent(this);
                        break;
                    } catch(MTProtoErrorException e) {
                        codeSource = new TaskCompletionSource<string>();
                        WrongCodeEvent(this);
                    }
                }
            }
        }

        public void SetPhone(string phone) {
            phoneSource.SetResult(phone);
        }

        public void SetCode(string code) {
            codeSource.SetResult(code);
        }

        public void SetSignUp(string firstname, string lastname) {
            signupSource.SetResult(new SignUpData(firstname, lastname));
        }
    }

    class SignUpData {
        public string firstname;
        public string lastname;

        public SignUpData(string firstname, string lastname) {
            this.firstname = firstname;
            this.lastname = lastname;
        }
    }
}
