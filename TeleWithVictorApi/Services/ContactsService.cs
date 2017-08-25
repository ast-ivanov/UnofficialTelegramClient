using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TelegramClient.Core;
using TelegramClient.Entities;
using TelegramClient.Entities.TL;
using TelegramClient.Entities.TL.Contacts;
using TeleWithVictorApi.Interfaces;

namespace TeleWithVictorApi.Services
{
    class ContactsService : IContactsService
    {
        private readonly ITelegramClient _client;
        private readonly SimpleIoC _ioc;

        public IEnumerable<Contact> Contacts { get; private set; }

        public ContactsService(SimpleIoC ioc)
        {
            _ioc = ioc;
            _client = ioc.Resolve<ITelegramClient>();
        }

        public async Task AddContact(string firstName, string lastName, string phone)
        {
            var contacts = new TlVector<TlInputPhoneContact>();
            contacts.Lists.Add(new TlInputPhoneContact {  FirstName = firstName ?? String.Empty, LastName = lastName ?? String.Empty, Phone = phone ?? String.Empty });

            //Create request 
            var req = new TlRequestImportContacts
            {
                Contacts = contacts
            };
            await _client.SendRequestAsync<TlImportedContacts>(req);
            //FillContacts();
        }

        public async Task DeleteContact(int number)
        {
            var contacts = new TlVector<TlAbsInputUser>();
            contacts.Lists.Add(new TlInputUser { UserId = Contacts.ToList()[number].Id });
            var req = new TlRequestDeleteContacts
            {
                Id = contacts
            };
            await _client.SendRequestAsync<bool>(req);
            FillContacts();
        }

        public async Task FillContacts()
        {
            var cont = await _client.GetContactsAsync();
            IEnumerable<TlUser> users = cont.Users.Lists.Cast<TlUser>();
            List<Contact> contacts = new List<Contact>();
            foreach (var item in users)
            {
                var contact = new Contact();
                contact.FillValues(item.FirstName, item.LastName, item.Phone, item.Id);
                contacts.Add(contact);
            }
            Contacts = contacts;
        }
    }
    public struct Contact : IContactWithId
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public int Id { get; set; }

        void IContact.FillValues(string firstName, string lastName, string phone)
        {
            FirstName = firstName;
            LastName = lastName;
            PhoneNumber = phone;
        }

        public void FillValues(string firstName, string lastName, string phone, int id)
        {
            (this as IContact).FillValues(firstName, lastName, phone);
            Id = id;
        }

        public override string ToString()
        {
            return String.IsNullOrEmpty(LastName) ? $"{FirstName}" : $"{FirstName} {LastName}";
        }
    }
}
