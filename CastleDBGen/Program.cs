using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CastleDBGen
{
    class Program
    {
        static int GetLangIndex(string str)
        {
            if (str.Equals("cpp"))
                return 0;
            else if (str.Equals("as"))
                return 1;
            else if (str.Equals("cs"))
                return 2;
            else if (str.Equals("lua"))
                return 3;
            else if (str.Equals("asbind"))
                return 4;
            return -1;
        }

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("--------------------------------");
                Console.WriteLine("CastleDBGen - (C) JSandusky 2015");
                Console.WriteLine("--------------------------------");
                Console.WriteLine("usage:");
                Console.WriteLine("    CastleDBGen <input-db-path> <outputfilename>");
                Console.WriteLine(" ");
                Console.WriteLine("switches:");
                Console.WriteLine("    -ns: namespace, follow with namespace");
                Console.WriteLine("        default: none");
                Console.WriteLine("        C#: REQUIRED");
                Console.WriteLine("    -lang: <language>: cpp, as, cs, lua");
                Console.WriteLine("        default: cpp");
                Console.WriteLine("        option: as");
                Console.WriteLine("        option: cs");
                Console.WriteLine("        option: lua");
                Console.WriteLine("        option: asbind (generate AS bindings)");
                Console.WriteLine("    -hd: <header path string>, C++ only");
                Console.WriteLine("    -db: name for database class");
                Console.WriteLine("        default: GameDatabase");
                Console.WriteLine("    -id: type to use for Unique Identifiers");
                Console.WriteLine("        default: string");
                Console.WriteLine("        option: int");
                Console.WriteLine("    -bin: type of binary read/write to support");
                Console.WriteLine("        default: none");
                Console.WriteLine("        option: on");
                Console.WriteLine("        option: only");
                Console.WriteLine("    -inherit: <classname>");
                Console.WriteLine(" ");
                Console.WriteLine("examples:");
                Console.WriteLine("    CastleDBGen C:\\MyDdatabase.cdb -lang cpp -ns MyNamespace");
                Console.WriteLine("    CastleDBGen C:\\MyDdatabase.cdb -lang as");
                Console.WriteLine("    CastleDBGen C:\\MyDdatabase.cdb -lang cpp -hd \"../HeaderPath/\"");
                return;
            }

            if (!System.IO.File.Exists(args[0]))
            {
                Console.WriteLine(string.Format("ERROR: File does not exist {0}", args[0]));
                return;
            }

            int lang = 0;
            string outName = args[0];
            if (args.Length > 1)
                outName = args[1];

            Dictionary<string, string> switches = new Dictionary<string, string>();
            for (int i = 0; i < args.Length; i += 1)
            {
                if (args[i].StartsWith("-"))
                {
                    switches[args[i].Replace("-","")] = args[i + 1];
                    i += 1;
                }
            }

            if (switches.ContainsKey("lang"))
            {
                lang = GetLangIndex(switches["lang"]);
                if (lang == -1)
                    lang = 0;
            }

            CastleDB db = new CastleDB(args[0]);

            List<string> errors = new List<string>();
            switch (lang)
            {
            case 0:
                outName = System.IO.Path.ChangeExtension(outName, ".cpp");
                new CppWriter().WriteClassDefinitions(db, outName, args[0], switches, errors);
                break;
            case 1:
                outName = System.IO.Path.ChangeExtension(outName, ".as");
                new AngelscriptWriter().WriteClassDefinitions(db, outName, args[0], switches, errors);
                break;
            case 2:
                outName = System.IO.Path.ChangeExtension(outName, ".cs");
                new CSharpWriter().WriteClassDefinitions(db, outName, args[0], switches, errors);
                break;
            case 3:
                outName = System.IO.Path.ChangeExtension(outName, ".lua");
                new LuaWriter().WriteClassDefinitions(db, outName, args[0], switches, errors);
                break;
            case 4:
                outName = System.IO.Path.ChangeExtension(outName, ".cpp");
                new ASBindingWriter().WriteClassDefinitions(db, outName, args[0], switches, errors);
                break;
            }

            foreach (string error in errors)
            {
                Console.WriteLine(string.Format("ERR: {0}", error));
            }

            MyNamespace.MyDB dbTest = new MyNamespace.MyDB();
            dbTest.Load(Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(args[0])));
        }
    }
}
