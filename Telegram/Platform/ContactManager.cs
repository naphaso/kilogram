using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Phone.PersonalInformation;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.UserData;
using Telegram.Core.Logging;
using Telegram.Model.Wrappers;
using Telegram.MTProto;
using Telegram.MTProto.Crypto;
using Contact = Microsoft.Phone.UserData.Contact;

namespace Telegram.Platform {
    class ContactManager {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(ContactManager));

        public async Task SyncContacts() {
            await TelegramSession.Instance.Established;

            Contacts contacts = new Contacts();
            contacts.SearchCompleted += ContactsOnSearchCompleted;
            contacts.SearchAsync(String.Empty, FilterKind.None, "Addressbook Contacts Sync");

        }

        private void ContactsOnSearchCompleted(object sender, ContactsSearchEventArgs e) {
            List<InputContact> contacts = new List<InputContact>();

            foreach (Contact contact in e.Results) {
                if (contact.PhoneNumbers.Count() == 0)
                    continue;

                string phoneNumber = contact.PhoneNumbers.ToList()[0].PhoneNumber ?? "";
                string firstName = contact.CompleteName.FirstName ?? "";
                string lastName = contact.CompleteName.LastName ?? "";

                InputContact inputPhoneContact = TL.inputPhoneContact(0, phoneNumber, firstName,
                    lastName);

                contacts.Add(inputPhoneContact);

            }

            SyncContacts(contacts);
        }

        private async Task SyncContacts(List<InputContact> contacts) {
            try {
                logger.info("Importing {0} contacts", contacts.Count);
//                await TelegramSession.Instance.Api.contacts_getContacts()

                string contactsBlobStr = "";
                foreach (InputContact contact in contacts) {
                    contactsBlobStr += ((InputPhoneContactConstructor) contact).first_name;
                    contactsBlobStr += ((InputPhoneContactConstructor) contact).last_name;
                    contactsBlobStr += ",";
                }

                string hash = MD5.GetMd5String(contactsBlobStr);

                if (hash != TelegramSession.Instance.ContactsStateMarker) {
                    logger.debug("blob hash is outdated, importing all contacts");
                    await TelegramSession.Instance.Api.contacts_importContacts(contacts, true);
                    TelegramSession.Instance.ContactsStateMarker = hash;
                }
                else {
                    logger.debug("blob hash is up-to-date");
                }

                // TODO: access contacts store, and get ther ids !

                string hashStr = await GetUserIdsMd5InStore();
                logger.debug("user hash string for ids: {0}", hashStr);
                contacts_Contacts serverContacts = await TelegramSession.Instance.Api.contacts_getContacts(hashStr);
                if (serverContacts.Constructor == Constructor.contacts_contactsNotModified) {
                    logger.debug("Contacts not modified, finishing sync.");
                    return;
                }

                Contacts_contactsConstructor serverContactsConstructor = (Contacts_contactsConstructor) serverContacts;
                foreach (MTProto.User mtuser in serverContactsConstructor.users) {
                    TelegramSession.Instance.SaveUser(mtuser);
                }

                foreach (MTProto.Contact mtcontact in serverContactsConstructor.contacts) {
                    ContactConstructor mtcontactConstructor = (ContactConstructor) mtcontact;
                    UserModel user = TelegramSession.Instance.GetUser(mtcontactConstructor.user_id);
                    await UpdateContact(user.Id.ToString(), user.FirstName, user.LastName, user.PhoneNumber);
                }
            }
            catch (Exception ex) {
                logger.error("sync contacts exception {0}", ex);
            }
        }

        public async Task<string> GetUserIdsMd5InStore() {
            logger.debug("Loading saved user ids...");
            ContactStore store = await ContactStore.CreateOrOpenAsync();
            ContactQueryResult result = store.CreateContactQuery();
            IReadOnlyList<StoredContact> contacts = await result.GetContactsAsync();
            List<int> userIds = new List<int>();

            foreach (var storedContact in contacts) {
                userIds.Add(int.Parse(storedContact.RemoteId));
            }

            userIds.Sort();
            string sortedStr = userIds.Aggregate("", (current, userId) => current + (userId + ","));

            if (sortedStr != "") { 
                sortedStr = sortedStr.Substring(0, sortedStr.Length - 1);
                logger.debug("sorted str is {0}", sortedStr);
                return MD5.GetMd5String(sortedStr);
            }

            return "";
        }

        async private Task UpdateContact(string remoteId, string givenName, string familyName, string phone) {
            logger.debug("updating contact id={0} {1} {2} {3}", remoteId, givenName, familyName, phone);
            ContactStore store = await ContactStore.CreateOrOpenAsync();

            string taggedRemoteId = remoteId;
            StoredContact contact = await store.FindContactByRemoteIdAsync(taggedRemoteId);

            if (contact != null) {
                logger.debug("contact id={0} exists in custom store, updating", remoteId);
                contact.GivenName = givenName;
                contact.FamilyName = familyName;

                IDictionary<string, object> props = await contact.GetPropertiesAsync();
                props[KnownContactProperties.MobileTelephone] = phone;

                await contact.SaveAsync();
            }
            else {
                await AddContact(remoteId, givenName, familyName, phone);
            }
        }

        public async Task AddContact(string remoteId, string givenName, string familyName, string phone) {
            logger.debug("adding contact id={0} {1} {2} {3}", remoteId, givenName, familyName, phone);
            ContactStore store = await ContactStore.CreateOrOpenAsync();

            StoredContact contact = new StoredContact(store);

            contact.RemoteId = remoteId;
            contact.GivenName = givenName;
            contact.FamilyName = familyName;
            // TODO: picture sync

            IDictionary<string, object> props = await contact.GetPropertiesAsync();
            props.Add(KnownContactProperties.MobileTelephone, phone);

            await contact.SaveAsync();
        }

    }
}
