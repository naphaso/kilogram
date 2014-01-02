using System.IO;
using Telegram.MTProto;

namespace Telegram.Model.Wrappers {
    public class UserModel {
        private User user;

        public UserModel(User user) {
            this.user = user;
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

        public string Status {
            get {
                switch (user.Constructor) {
                    case Constructor.userEmpty:
                        return "loading";
                    case Constructor.userSelf:
                        return GetStatusString(((UserSelfConstructor) user).status);
                    case Constructor.userContact:
                        return GetStatusString(((UserContactConstructor)user).status);
                    case Constructor.userRequest:
                        return GetStatusString(((UserRequestConstructor) user).status);
                    case Constructor.userForeign:
                        return GetStatusString(((UserForeignConstructor) user).status);
                    case Constructor.userDeleted:
                        return "deleted";
                    default:
                        throw new InvalidDataException("invalid constructor");
                }
            }
        }

        public static string GetStatusString(UserStatus status) {
            string statusString = "unknown";
            switch (status.Constructor) {
                case Constructor.userStatusEmpty:
                    statusString = "loading";
                    break;
                case Constructor.userStatusOffline:
                    statusString = "offline";
                    break;
                case Constructor.userStatusOnline:
                    statusString = "online";
                    break;
            }

            return statusString;
        }

        public static string GetLastOnlineTime(int lastOnline) {
            return "last online 14:88a";
        }
    }
}
