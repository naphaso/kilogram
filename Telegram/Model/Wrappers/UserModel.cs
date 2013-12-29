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
    }
}
