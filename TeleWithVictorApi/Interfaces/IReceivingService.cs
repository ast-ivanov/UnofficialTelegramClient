using System;
using System.Collections.Generic;
using TeleWithVictorApi.Services;

namespace TeleWithVictorApi.Interfaces
{
    public interface IReceivingService
    {
        Stack<Message> UnreadMessages { get; }
        event Action OnUpdateDialogs;
        event Action OnUpdateContacts;
        event Action<int, Message> OnAddUnreadMessage;
    }
}
