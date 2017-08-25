using System.Collections.Generic;
using System.Threading.Tasks;
using TeleWithVictorApi.Services;

namespace TeleWithVictorApi.Interfaces
{
    public interface IDialogsService
    {
        Dialog Dialog { get; set; }
        Task FillDialog(string dialogName, Peer peer, int dialogId);//TlAbsPeer peer - тип диалога: chat, channel, user

        IEnumerable<DialogShort> DialogList { get; }
        Task FillDialogList();
    }
}
