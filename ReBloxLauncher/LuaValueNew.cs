using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReBloxLauncher.Classes
{
    public class LuaValueNew
    {
        public string type;
        public object value;

        public LuaValueNew(string type = "LUA_TNIL", object value = null)
        {
            this.type = type;
            this.value = value;
        }
    }
}
