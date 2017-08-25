using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TelegramClient.Core;
using TelegramClient.Entities.TL;
using TelegramClient.Entities.TL.Messages;
using TeleWithVictorApi.Interfaces;

namespace TeleWithVictorApi.Services
{
    class ReceivingService : IReceivingService
    {
        private readonly ITelegramClient _client;
        private readonly SimpleIoC _ioc;

        public Stack<Message> UnreadMessages { get; } = new Stack<Message>();
        public event Action OnUpdateDialogs;
        public event Action OnUpdateContacts;
        public event Action<int, Message> OnAddUnreadMessage;
        //public event Action<string, string, DateTime> OnAddUnreadMessageFromChannel;

        public ReceivingService(SimpleIoC ioc)
        {
            _ioc = ioc;
            _client = ioc.Resolve<ITelegramClient>();
            _client.Updates.RecieveUpdates += Updates_RecieveUpdates;
        }

        private async void Updates_RecieveUpdates(TlAbsUpdates update)
        {
            int id = -1;
            string text = String.Empty;
            DateTime time = DateTime.Now;
            
            switch (update)
            {
                case TlUpdateShort _:
                    break;

                case TlUpdates updates:
                    foreach (var item in updates.Updates.Lists)
                    {
                        switch (item)
                        {
                            case TlUpdateDeleteMessages _:
                                OnUpdateDialogs?.Invoke();
                                break;

                            case TlUpdateContactLink _:
                                OnUpdateDialogs?.Invoke();
                                OnUpdateContacts?.Invoke();
                                break;

                            case TlUpdateNewChannelMessage updateNewChannelMessage:
                                update.MessageInfo(out id, out text, out time);
                                AddNewMessageToUnread(id, text, time);
                                break;

                            case TlUpdateNewMessage updateNewMessage:
                                update.MessageInfo(out id, out text, out time);
                                AddNewMessageToUnread(id, text, time);

                                Directory.CreateDirectory($"{Directory.GetCurrentDirectory()}\\Downloads");

                                switch ((updateNewMessage.Message as TlMessage).Media)
                                {
                                    case TlMessageMediaDocument document:
                                        var file = document.Document as TlDocument;
                                        var fileName = file.Attributes.Lists.OfType<TlDocumentAttributeFilename>().FirstOrDefault().FileName;

                                        int blockNumber = file.Size % 1048576 == 0 ? file.Size / 1048576 : file.Size / 1048576 + 1;
                                        List<byte> bytes = new List<byte>();
                                        for (int i = 0; i < blockNumber; i++)
                                        {
                                            var resFile = await _client.GetFile(new TlInputDocumentFileLocation { Id = file.Id, AccessHash = file.AccessHash, Version = file.Version }, file.Size, i * 1048576);
                                            bytes.AddRange(resFile.Bytes);
                                        }

                                        ConsoleTelegramUI.WriteToFile(bytes.ToArray(), fileName);
                                        break;

                                    case TlMessageMediaPhoto photo:
                                        var filePhoto = photo.Photo as TlPhoto;
                                        var photoInfo = filePhoto.Sizes.Lists.OfType<TlPhotoSize>().Last();
                                        var tf = (TlFileLocation)photoInfo.Location;
                                        var resFilePhoto = await _client.GetFile(new TlInputFileLocation { LocalId = tf.LocalId, Secret = tf.Secret, VolumeId = tf.VolumeId}, 0);

                                        var date = (updateNewMessage.Message as TlMessage).TimeUnixToWindows(true).ToString();
                                        date = date.Replace(':', '-');
                                        string photoName = $"ConsoleTelegram_{date}.png";

                                        ConsoleTelegramUI.WriteToFile(resFilePhoto.Bytes, photoName);
                                        break;
                                }
                                break;
                        }
                    }
                    break;
                case TlUpdateShortMessage _:
                    update.MessageInfo(out id, out text, out time);
                    AddNewMessageToUnread(id, text, time);
                    break;
            }
        }

        private async Task AddNewMessageToUnread(int senderId, string text, DateTime dateTime)
        {
            var dialogs = (TlDialogs)await _client.GetUserDialogsAsync();
            var dialog = dialogs.Dialogs.Lists[0];

            string title = "Unknown sender";

            switch (dialog.Peer)
            {
                case TlPeerUser peerUser:
                    var user = dialogs.Users.Lists
                        .OfType<TlUser>()
                        .FirstOrDefault(c => c.Id == peerUser.UserId);
                    title = $"{user?.FirstName} {user?.LastName}";
                    break;
                case TlPeerChannel peerChannel:
                    var channel = dialogs.Chats.Lists
                        .OfType<TlChannel>()
                        .FirstOrDefault(c => c.Id == peerChannel.ChannelId);
                    title = $"{channel.Title}";
                    break;
                case TlPeerChat peerChat:
                    var chat = dialogs.Chats.Lists
                        .OfType<TlChat>()
                        .FirstOrDefault(c => c.Id == peerChat.ChatId);
                    title = $"{chat.Title}";
                    break;
            }

            var message = _ioc.Resolve<Message>();
            
            message.FillValues(title, text, dateTime);
            UnreadMessages.Push(message);
            OnAddUnreadMessage?.Invoke(senderId, message);
        }
    }
}
