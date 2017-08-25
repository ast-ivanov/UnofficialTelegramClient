using System.Threading.Tasks;

namespace TeleWithVictorApi.Interfaces
{
    public interface ITelegramService : IAuthorization
    {
        IContactsService ContactsService { get; }
        IDialogsService DialogsService { get; }
        ISendingService SendingService { get; }
        IReceivingService ReceivingService { get; }

        Task FillAsync();
    }
}
