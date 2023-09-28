using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TLua
{//异常类
    public class Lua_dofile_Exception : global::NLua.Exceptions.LuaException
    {
        public Lua_dofile_Exception(string message, string source) : base(message)
        {
            this._source = source;
        }
        private readonly string _source;
        public Lua_dofile_Exception(global::System.Exception innerException, string source) : base("A .NET exception occurred in user-code", innerException)
        {
            this._source = source;
        }
    }
    public class Lua_dofile_once_Exception : global::NLua.Exceptions.LuaException
    {
        public Lua_dofile_once_Exception(string message, string source) : base(message)
        {
            this._source = source;
        }
        private readonly string _source;
        public Lua_dofile_once_Exception(global::System.Exception innerException, string source) : base("A .NET exception occurred in user-code", innerException)
        {
            this._source = source;
        }
    }
    public class Lua_PluginImportNoFound_Exception : global::NLua.Exceptions.LuaException
    {
        public Lua_PluginImportNoFound_Exception(string message, string source) : base(message)
        {
            this._source = source;
        }
        private readonly string _source;
        public Lua_PluginImportNoFound_Exception(global::System.Exception innerException, string source) : base("A .NET exception occurred in user-code", innerException)
        {
            this._source = source;
        }
    }

    public class Lua_UseEvent_Exception : global::NLua.Exceptions.LuaException
    {
        public Lua_UseEvent_Exception(string message, string source) : base(message)
        {
            this._source = source;
        }
        private readonly string _source;
        public Lua_UseEvent_Exception(global::System.Exception innerException, string source) : base("A .NET exception occurred in user-code", innerException)
        {
            this._source = source;
        }
    }
}
