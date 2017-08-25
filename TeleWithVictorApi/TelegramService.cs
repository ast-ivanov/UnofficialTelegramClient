using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelegramClient.Core;
using TelegramClient.Entities.TL;
using TeleWithVictorApi.Interfaces;
using TeleWithVictorApi.Services;

namespace TeleWithVictorApi
{
    class TelegramService : ITelegramService
    {
        private readonly ITelegramClient _client;
        private readonly SimpleIoC _ioc;
        private string _phoneNumber;
        private string _hash;

        public IContactsService ContactsService { get; private set; }
        public IDialogsService DialogsService { get; private set; }
        public ISendingService SendingService { get; private set; }
        public IReceivingService ReceivingService { get; private set; }

        public void LogOut()
        {
            try
            {
                File.Delete(Path.GetFullPath("session.dat"));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public bool IsUserAuthorized => _client.IsUserAuthorized();
        public bool Authorize()
        {
            _client.ConnectAsync().Wait();
            return _client.IsUserAuthorized();
        }

        public async Task EnterPhoneNumber(string number)
        {
            _phoneNumber = number;
            _hash = await _client.SendCodeRequestAsync(number);
        }

        public async Task<bool> EnterIncomingCode(string code)
        {
            try
            {
                TlUser tlUser = await _client.MakeAuthAsync(_phoneNumber, _hash, code);
                Contact currentUser = new Contact
                {
                    FirstName = tlUser.FirstName,
                    LastName = tlUser.LastName,
                    PhoneNumber = tlUser.Phone,
                    Id = tlUser.Id
                };
                string info = JsonConvert.SerializeObject(currentUser);

                using (var file = File.OpenWrite("currentUser.json"))
                {
                    using (StreamWriter sw = new StreamWriter(file))
                    {
                        sw.Write(info);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

        public TelegramService(SimpleIoC ioc)
        {
            _ioc = ioc;
            _client = ioc.Resolve<ITelegramClient>();
        }

        public async Task FillAsync()
        {
            DialogsService = _ioc.Resolve<IDialogsService>();
            SendingService = _ioc.Resolve<ISendingService>();
            ContactsService = _ioc.Resolve<IContactsService>();
            ReceivingService = _ioc.Resolve<IReceivingService>();
            ReceivingService.OnUpdateDialogs += ReceivingService_OnUpdateDialogs;
            ReceivingService.OnUpdateContacts += ReceivingService_OnUpdateContacts;

            await ContactsService.FillContacts();
            await DialogsService.FillDialogList();
        }

        private void ReceivingService_OnUpdateContacts()
        {
            ContactsService.FillContacts();
        }

        private void ReceivingService_OnUpdateDialogs()
        {
            DialogsService.FillDialogList();
        }

        private async Task Authenticate()
        {
            await _client.ConnectAsync();
            if (!_client.IsUserAuthorized())
            {
                string phoneNumber = string.Empty;
                Console.WriteLine("Input your phone number please: ");
                bool isPhoneCorrect = false;
                while (!isPhoneCorrect)
                {
                    Console.WriteLine("Number should be in format (7##########):");
                    phoneNumber = Console.ReadLine();
                    isPhoneCorrect = Validation.PhoneValidation(phoneNumber);
                }

                Console.WriteLine("Input a code, send to you in telegram: ");

                string hash = string.Empty;
                try
                {
                    hash = await _client.SendCodeRequestAsync(phoneNumber);
                }
                catch (Exception e)
                {

                }

                bool isCodeCorrect = false;
                while (!isCodeCorrect)
                {
                    var code = Console.ReadLine();
                    try
                    {
                        await _client.MakeAuthAsync(phoneNumber, hash, code);

                        isCodeCorrect = true;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Code is incorrect! Try again please: ");
                        Console.WriteLine(e.ToString());
                    }
                }

            }
            Console.WriteLine("\nWelcome!");
        }
    }
}
