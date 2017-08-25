using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TelegramClient.Core;
using TelegramClient.Entities.TL;
using TelegramClient.Entities.TL.Messages;
using TeleWithVictorApi.Interfaces;

namespace TeleWithVictorApi.Services
{
    class DialogsService : IDialogsService
    {
        private readonly ITelegramClient _client;
        private readonly SimpleIoC _ioc;
        private int _userId;
        
        public Dialog Dialog { get; set; }
        public IEnumerable<DialogShort> DialogList { get; private set; }

        public DialogsService(SimpleIoC ioc)
        {
            _ioc = ioc;
            _client = ioc.Resolve<ITelegramClient>();
            using (var file = File.OpenRead("currentUser.json"))
            {
                using (StreamReader sr = new StreamReader(file))
                {
                    string userData = sr.ReadToEnd();
                    Contact user = JsonConvert.DeserializeObject<Contact>(userData);
                    _userId = user.Id;
                }
            }
        }

        public async Task FillDialog(string dialogName, Peer peer, int dialogId)
        {
            Stack<Message> messages = new Stack<Message>();

            Dialog = _ioc.Resolve<Dialog>();
            dynamic history;
            var dialogs = (TlDialogs)await _client.GetUserDialogsAsync();
            try
            {
                switch (peer)
                {
                    case Peer.User:

                        var user = dialogs.Users.Lists.OfType<TlUser>().FirstOrDefault(c => c.Id == dialogId);
                        history = await _client.GetHistoryAsync(
                            new TlInputPeerUser {UserId = user.Id, AccessHash = (long) user.AccessHash}, 0, -1, 50);
                        break;

                    case Peer.Chat:
                        history = await _client.GetHistoryAsync(new TlInputPeerChat {ChatId = dialogId}, 0, -1, 50);
                        break;

                    default:
                        var channel = dialogs.Chats.Lists.OfType<TlChannel>().FirstOrDefault(c => c.Id == dialogId);
                        history = await _client.GetHistoryAsync(
                            new TlInputPeerChannel {ChannelId = channel.Id, AccessHash = (long) channel.AccessHash}, 0,
                            -1,
                            50);
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            foreach (var message in history.Messages.Lists)
            {
                if (message is TlMessage)
                {
                    string senderName = dialogName;

                    if (_userId == message.FromId)
                    {
                        senderName = "You";
                    }
                    else
                    {
                        foreach (TlUser user in history.Users.Lists)
                        {
                            if (user.Id == message.FromId)
                            {
                                senderName = $"{user.FirstName} {user.LastName}";
                                break;
                            }
                        }
                    }

                    AddMsg(message, messages, senderName);
                }
            }
            //foreach (var message in history.Messages.Lists)
            //{
            //    string senderName = dialogName;

            //    if (_userId == message.FromId)
            //    {
            //        senderName = "You";
            //    }
            //    else
            //    {
            //        foreach (TlUser user in history.Users.Lists)
            //        {
            //            if (user.Id == message.FromId)
            //            {
            //                senderName = $"{user.FirstName} {user.LastName}";
            //                break;
            //            }
            //        }
            //    }
                
            //    AddMsg(message, messages, senderName);
            //}
            Dialog.FillValues(dialogName, messages);
            Dialog.Id = dialogId;
        }

        private void AddMsg(TlMessage message, Stack<Message> messages, string senderName)
        {
            var msg = _ioc.Resolve<Message>();
            string text = message.GetTextMessage();
            msg.FillValues(senderName, text, message.TimeUnixToWindows(true));
            messages.Push(msg);
        }

        public async Task FillDialogList()
        {
            var dialogs = (TlDialogs)await _client.GetUserDialogsAsync();
            //int realDialogsCount = dialogs.Dialogs.Lists.Count;
            //if (DialogList != null && DialogList.Count() >)

            List<DialogShort> dialogsShort = new List<DialogShort>();

            
            foreach (var dlg in dialogs.Dialogs.Lists)
            {
                int id;
                Peer peer;
                string title;
                switch (dlg.Peer)
                {
                    case TlPeerUser peerUser:
                        id = peerUser.UserId;
                        peer = Peer.User;
                        var user = dialogs.Users.Lists
                            .OfType<TlUser>()
                            .FirstOrDefault(c => c.Id == id);
                        title = $"{user?.FirstName} {user?.LastName}";
                        break;
                    case TlPeerChannel peerChannel:
                        id = peerChannel.ChannelId;
                        peer = Peer.Channel;
                        title = dialogs.Chats.Lists
                            .OfType<TlChannel>()
                            .FirstOrDefault(c => c.Id == id)?.Title;
                        break;
                    case TlPeerChat peerChat:
                        id = peerChat.ChatId;
                        peer = Peer.Chat;
                        title = dialogs.Chats.Lists
                            .OfType<TlChat>()
                            .FirstOrDefault(c => c.Id == id)?.Title;
                        break;
                    default:
                        id = -1;
                        peer = Peer.Unknown;
                        title = String.Empty;
                        break;
                }
                var dlgShort = new DialogShort();
                dlgShort.FillValues(title, peer);
                dlgShort.Id = id;
                dialogsShort.Add(dlgShort);
            }
            DialogList = dialogsShort;
        }
    }

    public class Dialog
    {
        public string DialogName { get; private set; }
        public IEnumerable<Message> Messages { get; private set; }
        public int Id { get; set; }

        public void FillValues(string dialogName, IEnumerable<Message> messages)
        {
            DialogName = dialogName;
            Messages = messages;
        }
    }
    public class Message
    {
        public string SenderName { get; private set; }
        public string MessageText { get; private set; }
        public DateTime MessageDate { get; private set; }

        public void FillValues(string senderName, string text, DateTime date)
        {
            SenderName = senderName;
            MessageText = text;
            MessageDate = date;
        }

        public override string ToString()
        {
            return $"{MessageDate} from {SenderName}: {MessageText}";
        }
    }
    public class DialogShort : IHaveId
    {
        public string DialogName { get; private set; }
        public Peer Peer { get; private set; }
        public int Id { get; set; }

        public void FillValues(string dlName, Peer dlPeer)
        {
            DialogName = dlName;
            Peer = dlPeer;
        }
    }
}
