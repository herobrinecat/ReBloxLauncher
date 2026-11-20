using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReBloxLauncher.Classes
{
    public class Script
    {
        public string name;
        public string script;
        public string environmentID;
        public List<LuaValueNew> arguments;

        public Script(string name = "Script", string script = "print(\"Hello, world!\")", List<LuaValueNew> arguments = null, string environmentID = "RBX0")
        {
            this.name = name;
            this.script = script;
            this.environmentID = environmentID;
            this.arguments = arguments;
        }
    }
}
