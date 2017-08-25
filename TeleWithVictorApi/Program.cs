using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using TeleWithVictorApi.Interfaces;
using TeleWithVictorApi.Services;

namespace TeleWithVictorApi
{
    class Program
    {
        static void Main(string[] args)
        {
            var ioc = new SimpleIoC();

            #region RegisterIoC

            ioc.RegisterInstance(TelegramClient.Core.ClientFactory.BuildClient(35699, "c5faabe85e286bbb3eac32df78b34517", "149.154.167.40", 443));
            ioc.Register<IContactsService, ContactsService>();
            ioc.Register<IDialogsService, DialogsService>();
            ioc.Register<ISendingService, SendingService>();
            ioc.Register<IReceivingService, ReceivingService>();

            ioc.Register<ITelegramService, TelegramService>();

            ioc.Register<IConsoleTelegramUi, ConsoleTelegramUI>();
            
            #endregion

            var ui = ioc.Resolve<IConsoleTelegramUi>();
            ui.Authorize();
            ui.Start();
            Console.ReadKey();
        }
    }
}
