using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Phone.UserData;
using Telegram.Core.Logging;
using Telegram.MTProto;
using Contact = Microsoft.Phone.UserData.Contact;

namespace Telegram.Platform {
    class ContactManager {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(ContactManager));

        public void ImportContacts() {
            Contacts contacts = new Contacts();

            contacts.SearchCompleted += ContactsOnSearchCompleted;
            contacts.SearchAsync(String.Empty, FilterKind.None, "Addressbook Contacts");

        }

        private void ContactsOnSearchCompleted(object sender, ContactsSearchEventArgs e) {
            List<InputContact> contacts = new List<InputContact>();

            foreach (Contact contact in e.Results) {
                if (!contact.PhoneNumbers.Any())
                    continue;

                string phoneNumber = contact.PhoneNumbers.ToList()[0].PhoneNumber;
                string firstName = contact.CompleteName.FirstName;
                string lastName = contact.CompleteName.LastName;

                InputContact inputPhoneContact = TL.inputPhoneContact(0, phoneNumber, firstName,
                    lastName);

            }

            SyncContactsAsync(contacts);
        }

        private async Task SyncContactsAsync(List<InputContact> contacts) {
            try {
//                await TelegramSession.Instance.Api.contacts_getContacts()
                await TelegramSession.Instance.Api.contacts_importContacts(contacts, true);
            }
            catch (Exception ex) {
                
            }
        }

    }
}
