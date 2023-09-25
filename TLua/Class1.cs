using System;
using System.Collections;
using System.IO;
using System.Reflection;
using BepInEx;
using NLua;
using UnityEngine;

namespace TLua
{
    public class BaseTLuaPlugin
    {
    }

    [BepInPlugin("TLua.TsumugiLuaLoad", "TLuaCore", "0.0.0.1")]
    public class TsumugiLua : BaseUnityPlugin
    {
        //成员数据
        private static ArrayList LuaManage;
        private static ArrayList DoFileName;
        private static string ExePath;//获得程序运行路径
        private static string[] directories;//Lua列表
        private static int i = -1;
        //方法
        void Awake()//启动使加载
        {
            Debug.Log("TLua:加载~");

            LuaManage = new ArrayList();//声明并实例化一个全局Lua栈变量用于控制交互
            DoFileName = new ArrayList();
            ExePath = global::System.IO.Directory.GetCurrentDirectory();//获得程序运行路径
            if (!Directory.Exists(@ExePath + "\\BepInEx\\TLua"))
            {
                Debug.Log("TLua:没有在BepInEx路径下发现TLua，现已新建");
                Directory.CreateDirectory(@ExePath + "\\BepInEx\\TLua");//如果不存在TLua文件夹就新建
            }
            directories = System.IO.Directory.GetDirectories(@ExePath + "\\BepInEx\\TLua");//获得Lua列表
            foreach (string dir in directories)
            {//第一步初始化，设置编码和构建TsumugiLuaManage数组
                NLua.Lua temp = new NLua.Lua();
                temp.State.Encoding = System.Text.Encoding.UTF8;//设置编码防止乱码
                LuaManage.Add(new TsumugiLuaManage(temp, dir));
            }
            foreach (TsumugiLuaManage mainLua in LuaManage)
            {//第二步初始化，注册特定全局函数
                mainLua.TsumugiLuaStack.LoadCLRPackage();
                mainLua.TsumugiLuaStack.RegisterFunction("print", null, typeof(TLuaGlobalFunc).GetMethod("Lua_print"));
                mainLua.TsumugiLuaStack.RegisterFunction("dofile", null, typeof(TLuaGlobalFunc).GetMethod("Lua_dofile"));
                mainLua.TsumugiLuaStack.RegisterFunction("dofile_once", null, typeof(TLuaGlobalFunc).GetMethod("Lua_dofile_once"));
            }
            foreach (TsumugiLuaManage mainLua in LuaManage)
            {//执行步奏
                DoFileName.Clear();
                i++;
                string LuaBuf = File.ReadAllText(mainLua.LuaPath + "\\main.lua");
                mainLua.TsumugiLuaStack.DoString(LuaBuf);
            }

            /*
            Debug.Log("TLua:开始尝试加载可能有的扩展Lua插件");
            Assembly assembly = Assembly.GetExecutingAssembly();//获得程序集

            // Loop through all the types in the assembly
            foreach (Type type in assembly.GetTypes())//获得所有类类型
            {
                // Check if the type has the DerivedAttribute
                if (type.IsSubclassOf(typeof(BaseTLuaPlugin)))//判断是否属于这个的派生类
                {
                    MethodInfo[] methods = type.GetMethods();//获得所有方法
                    string[] MethodsTable = {//需要忽视的方法表
                "GetType",
                "ToString",
                "Equals",
                "GetHashCode"
                };
                    foreach (MethodInfo method in methods)//遍历方法
                    {
                        foreach (string methodName in MethodsTable)
                        {
                            if (method.Name == methodName) goto next;//跳出内层循环
                        }
                        Console.WriteLine(method.Name);
                        var instance = Activator.CreateInstance(type);
                        //LuaStack.RegisterFunction(method.Name, instance, type.GetMethod(method.Name));
                    next:;
                    }
                    // Call the SayHello method
                }
            }*/
        }
        //嵌套类
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

        private class TsumugiLuaManage//管理Lua栈变量用的
        {
            public TsumugiLuaManage(NLua.Lua LuaStack, string path)
            {
                TsumugiLuaStack = LuaStack;
                LuaPath = path;
            }
            public NLua.Lua TsumugiLuaStack;
            public string LuaPath;
        }
        private static class TLuaGlobalFunc //嵌套类，用于注册特定函数
        {
            public static int Lua_print(params object[] par)
            {
                string str = "";
                foreach (object obj in par)
                {
                    str += obj.ToString();
                }
                Debug.Log(str);
                return str.Length;
            }
            public static void Lua_dofile(string FilePath)
            {
                string LuaBuf = File.ReadAllText(ExePath + "\\TLua\\" + FilePath);
                try
                {
                    TsumugiLuaManage temp = LuaManage[i] as TsumugiLuaManage;//类型转换
                    temp.TsumugiLuaStack.DoString(LuaBuf, "chunk");//加载文件
                }
                catch (NLua.Exceptions.LuaScriptException ER)
                {
                    throw new Lua_dofile_Exception(ER.Message + " | File:" + FilePath, ER.Source);
                }
            }
            public static void Lua_dofile_once(string FilePath)
            {
                string LuaBuf = File.ReadAllText(ExePath + "\\mods\\" + FilePath);
                foreach (string str in DoFileName)
                {//先判断是不是已有的
                    if (str == FilePath)
                    {
                        return;//如果是有的不加载
                    }
                }
                DoFileName.Add(FilePath);//如果是没有的就增加
                try
                {
                    TsumugiLuaManage temp = LuaManage[i] as TsumugiLuaManage;//类型转换
                    temp.TsumugiLuaStack.DoString(LuaBuf, "chunk");//加载文件
                }
                catch (NLua.Exceptions.LuaScriptException ER)
                {
                    throw new Lua_dofile_once_Exception(ER.Message + " | File:" + FilePath, ER.Source);
                }
            }
        }
    }
}
