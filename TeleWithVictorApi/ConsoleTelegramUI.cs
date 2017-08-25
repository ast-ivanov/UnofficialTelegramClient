using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using TeleWithVictorApi.Interfaces;

namespace TeleWithVictorApi
{
    class ConsoleTelegramUI : IConsoleTelegramUi
    {
        private ITelegramService _client;

        private List<char> textBuffer = new List<char>();

        public ConsoleTelegramUI(SimpleIoC ioc)
        {
            _client = ioc.Resolve<ITelegramService>();
        }

        public void Start()
        {
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
            };

            if (!_client.IsUserAuthorized)
            {
                Console.WriteLine("You need to authorize first.");
                return;
            }
            bool isRun = true;
            while (isRun)
            {
                Console.Write("\n->");
                var line = Console.ReadLine()?.Split(' ');
                var parseResult = Parser.Default.ParseArguments<PrintOptions, AddContactOptions, DeleteContactOptions, EnterDialogOptions, Quit>(line ?? new[] { "", "" });

                parseResult.
                    WithParsed<Quit>(quit => isRun = false).
                    WithParsed<PrintOptions>(Print).
                    WithParsed<EnterDialogOptions>(Enter).
                    WithParsed<DeleteContactOptions>(async opt => await _client.ContactsService.DeleteContact(opt.Index)).
                    WithParsed<AddContactOptions>(async opt => await _client.ContactsService.AddContact(opt.FirstName, opt.LastName, opt.Number));
            }
        }

        private void Print(PrintOptions opt)
        {
            if (opt.Contacts)
            {
                PrintContacts();
            }
            if (opt.Dialogs)
            {
                PrintDialogs();
            }
            if (opt.UnreadMessages)
            {
                PrintUnreadMessages();
            }
        }

        private void Enter(EnterDialogOptions opt)
        {
            try
            {
                PrintDialogMessages(opt.Index, opt.IsContact).Wait();
                while (true)
                {
                    //string mes = Console.ReadLine();
                    string mes = Message();
                    if (mes == null)
                    {
                        _client.DialogsService.Dialog = null;
                        _client.DialogsService.FillDialogList();
                        break;
                    }
                    if (mes == String.Empty)
                    {
                        continue;
                    }

                    if (mes.Length > 1 && mes.Substring(0, 2) == "-f")
                    {
                        mes = mes.Remove(0, 2).TrimStart(' ');
                        SendFile(opt.Index, mes, "file", opt.IsContact);
                    }
                    else
                    {
                        SendTextMessage(opt.Index, mes, opt.IsContact);
                    }
                }
            }
            catch (ArgumentOutOfRangeException e)
            {
                Console.WriteLine("Number is incorrect!");
            }
        }

        private string Message()
        {
            string temp = String.Empty;
            ConsoleKeyInfo cki;
            do
            {
                cki = Console.ReadKey();

                switch (cki.Key)
                {
                    case ConsoleKey.Enter:
                        if (textBuffer.Count != 0)
                        {
                            Console.WriteLine();
                        }
                        temp = new string(textBuffer.ToArray());
                        textBuffer.Clear();
                        break;
                    case ConsoleKey.Backspace:
                        if (textBuffer.Count != 0)
                        {
                            textBuffer.RemoveAt(textBuffer.Count - 1);
                            Console.Write(" \b");
                        }
                        break;
                    case ConsoleKey.Escape:
                        Console.Write("\b ");
                        break;
                    default:
                        textBuffer.Add(cki.KeyChar);
                        break;
                }
            } while (cki.Key != ConsoleKey.Enter && cki.Key != ConsoleKey.Escape);
            if (cki.Key == ConsoleKey.Escape)
                return null;
            
            return temp;
        }

        private void ClearAndWrite(string message)
        {
            foreach (var _ in textBuffer)
            {
                Console.Write('\b');
            }
            Console.WriteLine(message);
            Console.Write(textBuffer.ToArray());
        }

        public void Authorize()
        {
            if (!_client.Authorize())
            {
                string phone, code;
                do
                {
                    Console.Write("Enter your phone number:\n+7");
                    phone = $"7{Console.ReadLine()}";
                }
                while (!Validation.PhoneValidation(phone));

                _client.EnterPhoneNumber(phone);
                do
                {
                    Console.WriteLine("Enter incoming code:");
                    code = Console.ReadLine();
                }
                while (!_client.EnterIncomingCode(code).Result);

            }
            Console.WriteLine("Welcome");
            _client.FillAsync();
            _client.ReceivingService.OnAddUnreadMessage += (senderId, message) =>
            {
                Console.Beep();
                if (_client.DialogsService.Dialog?.Id == senderId)
                {
                    ClearAndWrite(message.ToString());
                    _client.ReceivingService.UnreadMessages.Pop();
                }
                if (_client.DialogsService.Dialog == null)
                {
                    _client.DialogsService.FillDialogList();
                }
            };
            _client.SendingService.OnSendMessage += message =>
            {
                Console.Beep();
                ClearAndWrite(message.ToString());
            };
        }

        public async Task SendTextMessage(int index, string text, bool isContact)
        {
            GetReceiverInfo(index, isContact, out Peer peer, out int id, out string dialogTitle);
            await _client.SendingService.SendTextMessage(peer, id, text);
        }

        public async Task SendFile(int index, string path, string caption, bool isContact)
        {
            GetReceiverInfo(index, isContact, out Peer peer, out int id, out string dialogTitle);
            try
            {
                await _client.SendingService.SendFile(peer, id, path, caption);
                Console.WriteLine("File otpravlen");
            }
            catch (ArgumentException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Sending failed! Try again");
            }
        }

        private void GetReceiverInfo(int index, bool isContact, out Peer peer, out int id, out string dialogTitle)
        {
            if (isContact)
            {
                var contact = _client.ContactsService.Contacts.ElementAt(index);
                peer = Peer.User;
                id = contact.Id;
                dialogTitle = contact.ToString();
            }
            else
            {
                var dialog = _client.DialogsService.DialogList.ElementAt(index);
                peer = dialog.Peer;
                id = dialog.Id;
                dialogTitle = dialog.DialogName;
            }
        }

        public async Task PrintDialogMessages(int index, bool isContact)
        {
            GetReceiverInfo(index, isContact, out Peer peer, out int id, out string dialogTitle);
            await _client.DialogsService.FillDialog(dialogTitle, peer, id);
            Console.Clear();
            Console.WriteLine($"{_client.DialogsService.Dialog.DialogName}");
            foreach (var item in _client.DialogsService.Dialog.Messages)
            {
                Console.WriteLine(item);
            }
        }

        public void PrintContacts()
        {
            int index = 0;
            Console.WriteLine("\nContacts:");
            foreach (var item in _client.ContactsService.Contacts)
            {
                Console.WriteLine($"{index} {item}");
                index++;
            }
        }

        public void PrintDialogs()
        {
            int index = 0;
            Console.WriteLine("\nDialogs:");
            foreach (var item in _client.DialogsService.DialogList)
            {
                Console.WriteLine($"{index} {item.DialogName}");
                index++;
            }
        }

        public void PrintUnreadMessages()
        {
            int index = 0;
            if (_client.ReceivingService.UnreadMessages.Count == 0)
            {
                Console.WriteLine("\nNo unread messages");
            }
            else
            {
                Console.WriteLine("\nUnread messages:");
                foreach (var item in _client.ReceivingService.UnreadMessages)
                {
                    Console.WriteLine($"{index} {item}");
                    index++;
                }
                _client.ReceivingService.UnreadMessages.Clear();
            }
        }

        public static async Task WriteToFile(byte[] bytes, string fileName)
        {
            try
            {
                using (FileStream fs = File.Create($"{Directory.GetCurrentDirectory()}\\Downloads\\{fileName}"))
                {
                    await fs.WriteAsync(bytes, 0, bytes.Length);
                    fs.Close();
                    Console.WriteLine($"{fileName} successfully installed in {fs.Name}");
                    //if (fileName.Contains(".png") || fileName.Contains(".jpg") || fileName.Contains(".jpeg"))
                    //{
                    //    ImageToConsole.ShowImageToConsole(fs.Name);
                    //}
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"saving of {fileName} failed!");
            }
            Console.Write("->");
        }
    }
}
