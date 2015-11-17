using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CastleDBGen
{
    internal abstract class BaseDBWriter
    {
        public abstract void WriteClassDefinitions(CastleDB database, string fileBase, string sourceFileName, Dictionary<string, string> switches, List<string> errors);

        protected string GetTabString(int ct)
        {
            string str = "    ";
            for (int i = 0; i < ct; ++i)
                str += "    ";
            return str;
        }
    }
}
