using System.Threading.Tasks;

namespace TeleWithVictorApi.Interfaces
{
    public interface IAuthorization
    {
        void LogOut();
        bool Authorize();
        bool IsUserAuthorized { get; }
        Task EnterPhoneNumber(string number);
        Task<bool> EnterIncomingCode(string code);
    }
}
