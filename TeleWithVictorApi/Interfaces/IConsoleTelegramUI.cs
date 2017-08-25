using System.Threading.Tasks;

namespace TeleWithVictorApi.Interfaces
{
    public interface IConsoleTelegramUi
    {
        void Authorize();
        void Start();
        Task SendTextMessage(int index, string text, bool isContact);
        Task PrintDialogMessages(int index, bool isContact);
        Task SendFile(int index, string path, string caption, bool isContact);
        void PrintContacts();
        void PrintDialogs();
        void PrintUnreadMessages();
    }
}
