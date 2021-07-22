using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security;

namespace Microsoft.Sales.MSALTokenGenerator
{
    public class ConvertStringToSecureString
    {
        private SecureString secure;
        public ConvertStringToSecureString()
        {
            secure = new SecureString();
        }

        public SecureString StringToSecureString(string password)
        {
            var charArray = password.ToCharArray();

            foreach (char ch in password.ToCharArray())
            {
                secure.AppendChar(ch);
            }

            return secure;
        }
    }
}
