using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleWithVictorApi
{
    static class Validation
    {
        public static bool PhoneValidation(string number)
        {
            return (number[0] == '7' && number.Length == 11);
        }
    }
}
