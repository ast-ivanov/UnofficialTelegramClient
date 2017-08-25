using System;
using TeleWithVictorApi.Services;

namespace TeleWithVictorApi.Interfaces
{
    public interface IHaveSendEvent
    {
        event Action<Message> OnSendMessage;
    }
}
