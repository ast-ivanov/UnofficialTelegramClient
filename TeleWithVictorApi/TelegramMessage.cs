using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelegramClient.Core;
using TelegramClient.Entities.TL;
using TelegramClient.Entities.TL.Messages;

namespace TeleWithVictorApi
{
    static class TelegramMessage
    {
        public static void MessageInfo(this TlAbsUpdates update, out int senderId, out string text, out DateTime time)
        {
            senderId = -1;
            text = null;
            time = DateTime.Now;
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
                                break;

                            case TlUpdateContactLink _:
                                break;

                            case TlUpdateNewChannelMessage updateNewChannelMessage:
                                var tlMessage = updateNewChannelMessage.Message as TlMessage;
                                text = tlMessage.GetTextMessage();
                                time = tlMessage.TimeUnixToWindows(true);
                                senderId = tlMessage.GetSenderId();
                                break;

                            case TlUpdateNewMessage updateNewMessage:
                                senderId = (updateNewMessage.Message as TlMessage).GetSenderId();
                                text = (updateNewMessage.Message as TlMessage).GetTextMessage();
                                time = (updateNewMessage.Message as TlMessage).TimeUnixToWindows(true);
                                break;
                        }
                    }
                    break;

                case TlUpdateShortMessage shortMessage:
                    senderId = shortMessage.UserId;
                    text = shortMessage.Message;
                    time = shortMessage.TimeUnixToWindows(true);
                    break;

                case TlUpdateShortChatMessage chatMessage:
                    senderId = chatMessage.ChatId;
                    text = chatMessage.Message;
                    time = chatMessage.TimeUnixToWindows(true);
                    break;

                default:
                    break;
            }
        }

        public static string GetTextMessage(this TlMessage message)
        {
            string text = String.Empty;
            if (message.Media != null)
            {
                switch (message.Media)
                {
                    case TlMessageMediaDocument document:
                        text = $"{(document.Document as TlDocument).Attributes.Lists.OfType<TlDocumentAttributeFilename>().FirstOrDefault().FileName} {document.Caption}";
                        break;
                    case TlMessageMediaPhoto photo:
                        text = $"[Photo] {photo.Caption}";
                        break;
                }
            }
            else
            {
                text = message.Message;
            }
            
            return text;
        }

        public static DateTime TimeUnixToWindows(this TlMessage message, bool isLocal)
        {
            return DateTimeService.TimeUnixToWindows(message.Date, isLocal);
        }

        public static DateTime TimeUnixToWindows(this TlUpdateShortChatMessage message, bool isLocal)
        {
            return DateTimeService.TimeUnixToWindows(message.Date, isLocal);
        }

        public static DateTime TimeUnixToWindows(this TlUpdateShortMessage message, bool isLocal)
        {
            return DateTimeService.TimeUnixToWindows(message.Date, isLocal);
        }

        public static DateTime TimeUnixToWindows(this TlUpdateShortSentMessage message, bool isLocal)
        {
            return DateTimeService.TimeUnixToWindows(message.Date, isLocal);
        }

        public static int GetSenderId(this TlMessage tlMessage)
        {
            int id = tlMessage.FromId ?? -1;
            if (id == -1)
            {
                var receiver = tlMessage.ToId;
                switch (receiver)
                {
                    case TlPeerChannel channel:
                        id = channel.ChannelId;
                        break;
                    case TlPeerChat chat:
                        id = chat.ChatId;
                        break;
                    case TlPeerUser user:
                        id = user.UserId;
                        break;
                }
            }
            return id;
        }
    }
}
