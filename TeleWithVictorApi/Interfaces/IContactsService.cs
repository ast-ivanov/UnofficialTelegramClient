using System.Collections.Generic;
using System.Threading.Tasks;
using TeleWithVictorApi.Services;

namespace TeleWithVictorApi.Interfaces
{
    public interface IContactsService
    {
        IEnumerable<Contact> Contacts { get; }
        Task FillContacts();
        Task AddContact(string firstName, string lastName, string phone);
        Task DeleteContact(int number);
    }
}
