using System.Threading.Tasks;

namespace TeleWithVictorApi.Interfaces
{
    public interface ISendingService : IHaveSendEvent
    {
        Task SendTextMessage(Peer peer, int receiverId, string msg);
        Task SendFile(Peer peer, int receiverId, string path, string caption);
    }
}
