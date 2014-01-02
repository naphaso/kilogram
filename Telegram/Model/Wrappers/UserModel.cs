using System.IO;
using Telegram.MTProto;

namespace Telegram.Model.Wrappers {

    public delegate void UserModelChangeHandler();
    public class UserModel {
        private User user;

        public event UserModelChangeHandler ChangeEvent;

        public UserModel(User user) {
            this.user = user;
        }

        public void SetUser(User user) {
            this.user = user;
            ChangeEvent();
        }

        public int Id {
            get {
                switch (user.Constructor) {
                    case Constructor.userEmpty:
                        return ((UserEmptyConstructor)user).id;
                    case Constructor.userSelf:
                        return ((UserSelfConstructor) user).id;
                    case Constructor.userContact:
                        return ((UserContactConstructor) user).id;
                    case Constructor.userRequest:
                        return ((UserRequestConstructor) user).id;
                    case Constructor.userForeign:
                        return ((UserForeignConstructor) user).id;
                    case Constructor.userDeleted:
                        return ((UserDeletedConstructor) user).id;
                    default:
                        throw new InvalidDataException("invalid constructor");
                }
            }
        }

        public User RawUser {
            get {
                return user;
            }
        }

        public string FullName {
            get {
                switch (user.Constructor) {
                    case Constructor.userEmpty:
                        return "empty";
                    case Constructor.userSelf:
                        return ((UserSelfConstructor)user).first_name + " " + ((UserSelfConstructor)user).first_name;
                    case Constructor.userContact:
                        return ((UserContactConstructor)user).first_name + " " + ((UserContactConstructor)user).last_name;
                    case Constructor.userRequest:
                        return ((UserRequestConstructor)user).first_name + " " + ((UserRequestConstructor)user).last_name;
                    case Constructor.userForeign:
                        return ((UserForeignConstructor)user).first_name + " " + ((UserForeignConstructor)user).last_name;
                    case Constructor.userDeleted:
                        return ((UserDeletedConstructor)user).first_name + " " + ((UserDeletedConstructor)user).last_name;
                    default:
                        throw new InvalidDataException("invalid constructor");
                }
            }
        }
    }
}
