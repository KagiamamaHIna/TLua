using System;
using System.Collections;
using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using NLua;
using UnityEngine;

namespace TLua
{
    [AttributeUsage(AttributeTargets.Method)]//特性类，标记的类方法会被注册为Lua函数
    public class LuaFunctionAttribute : Attribute
    {
        public string Func;
        public object obj;
        public LuaFunctionAttribute(string name, object obj = null)
        {
            Func = name;
            this.obj = obj;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]//特性类，标记的类会被注册为Lua类
    public class LuaClassAttribute : Attribute
    {
        public string ClassName;
        public LuaClassAttribute(string className)
        {
            ClassName = className;
        }
    }

    [BepInPlugin("TLua.TsumugiLuaLoad", "TLuaCore", "0.0.0.1")]
    public class TsumugiLua : BaseUnityPlugin
    {
        //成员数据
        public static ArrayList LuaManage;
        public static int i = -1;

        private static ArrayList DoFileName;
        private static ArrayList PluginImportName;
        private static string ExePath;//获得程序运行路径
        private static string[] directories;//Lua列表
        //方法
        void Awake()//启动使加载
        {
            Debug.Log("TLua:加载~");

            LuaManage = new ArrayList();//声明并实例化一个全局Lua栈变量用于控制交互
            DoFileName = new ArrayList();
            PluginImportName = new ArrayList();
            ExePath = global::System.IO.Directory.GetCurrentDirectory();//获得程序运行路径
            if (!Directory.Exists(@ExePath + "\\BepInEx\\TLua"))
            {
                Debug.Log("TLua:没有在BepInEx路径下发现TLua，现已新建");
                Directory.CreateDirectory(@ExePath + "\\BepInEx\\TLua");//如果不存在TLua文件夹就新建
            }
            directories = System.IO.Directory.GetDirectories(@ExePath + "\\BepInEx\\TLua");//获得Lua列表
            {
                int index = 0;
                foreach (string dir in directories)
                {//第一步初始化，设置编码和构建TsumugiLuaManage数组
                    if (File.Exists(dir + "\\main.lua"))//判断是否存在main.lua文件，如果不存在则不需要lua栈管理
                    {//存在的情况下
                        NLua.Lua temp = new NLua.Lua();
                        temp.State.Encoding = System.Text.Encoding.UTF8;//设置编码防止乱码
                        LuaManage.Add(new TsumugiLuaManage(temp, dir, index));
                        index++;
                    }
                }
            }
            foreach (TsumugiLuaManage mainLua in LuaManage)
            {//第二步初始化，注册特定全局函数
                mainLua.TsumugiLuaStack.LoadCLRPackage();
                mainLua.TsumugiLuaStack.RegisterFunction("print", null, typeof(TLuaGlobalFunc).GetMethod("Lua_print"));
                mainLua.TsumugiLuaStack.RegisterFunction("dofile", null, typeof(TLuaGlobalFunc).GetMethod("Lua_dofile"));
                mainLua.TsumugiLuaStack.RegisterFunction("dofile_once", null, typeof(TLuaGlobalFunc).GetMethod("Lua_dofile_once"));
                mainLua.TsumugiLuaStack.RegisterFunction("PluginImport", null, typeof(TLuaGlobalFunc).GetMethod("Lua_PluginImport"));
                mainLua.TsumugiLuaStack.RegisterFunction("GamePath", null, typeof(TLuaGlobalFunc).GetMethod("Lua_GamePath"));
                mainLua.TsumugiLuaStack.RegisterFunction("LuaPath", null, typeof(TLuaGlobalFunc).GetMethod("Lua_LuaPath"));
                mainLua.TsumugiLuaStack.RegisterFunction("UseEvent", null, typeof(TLuaCBFuncManage).GetMethod("Lua_UseEvent"));
            }
            foreach (TsumugiLuaManage mainLua in LuaManage)
            {//执行步奏
                DoFileName.Clear();//清除dofile_once用于判断的缓存，因为已经到下一个lua了
                TLuaCBFuncManage.UseEventName.Clear();//同上
                PluginImportName.Clear();
                i++;//因为已经判断过是否存在了，所以不需要再次判断
                string LuaBuf = File.ReadAllText(mainLua.LuaPath + "\\main.lua");
                mainLua.TsumugiLuaStack.DoString(LuaBuf);
                foreach (string str in TLuaCBFuncManage.EventList)
                {
                    if (TLuaCBFuncManage.EventToBool[str])
                    {//如果静态标识为真
                        TLuaCBFuncManage.TestCallBack(str, TLuaCBFuncManage.EventExtra[TLuaCBFuncManage.EventList.IndexOf(str)] as string);
                    }
                }
            }
        }
        public class TsumugiLuaManage//管理Lua栈变量用的
        {
            public TsumugiLuaManage(NLua.Lua LuaStack, string path, int index)
            {
                TsumugiLuaStack = LuaStack;
                LuaPath = path;
                this.index = index;
            }
            public NLua.Lua TsumugiLuaStack;
            public string LuaPath;
            public int index;
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
                    temp.TsumugiLuaStack.DoString(LuaBuf);//加载文件
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
                    temp.TsumugiLuaStack.DoString(LuaBuf);//加载文件
                }
                catch (NLua.Exceptions.LuaScriptException ER)
                {
                    throw new Lua_dofile_once_Exception(ER.Message + " | File:" + FilePath, ER.Source);
                }
            }
            public static string Lua_GamePath()
            {
                return ExePath;
            }
            public static string Lua_LuaPath()
            {
                return (LuaManage[i] as TsumugiLuaManage).LuaPath;
            }
            public static void Lua_PluginImport(string AssemblyName,string ExtraName = "")
            {
                bool LoadNow = false;
                string AssemblyPath = ExePath + "\\BepInEx\\plugins\\" + AssemblyName + ".dll";//获得路径
                if (File.Exists(AssemblyPath))//判断程序集是否存在
                {
                    foreach (string str in PluginImportName)
                    {//先判断是不是已有的
                        if (str == AssemblyPath)
                        {
                            return;//如果是有的不加载
                        }
                    }
                    Debug.Log("加载Plugin，路径:" + AssemblyPath);
                    Type[] types = Assembly.LoadFrom(AssemblyPath).GetTypes();//获得所有类型
                    PluginImportName.Add(AssemblyName);
                    TsumugiLuaManage temp = LuaManage[i] as TsumugiLuaManage;//类型转换
                    foreach (Type type in types)//遍历类型
                    {
                        Debug.Log("发现类："+type.Name);
                        LuaClassAttribute luaClass = type.GetCustomAttribute<LuaClassAttribute>();
                        if (luaClass != null)
                        {
                            Debug.Log(type.Name + "类被标记为LuaClassAttribute，将被注册到Lua中"+"，变量名称为:"+ ExtraName + luaClass.ClassName);
                            //Debug.Log("luanet.load_assembly(\"" + AssemblyName + "\")");
                            //Debug.Log(type.Name + "=luanet.import_type(\"" + type.ToString() + "\")");
                            temp.TsumugiLuaStack.DoString("luanet.load_assembly(\"" + AssemblyName + "\")");//加载程序集
                            LoadNow = true;
                            temp.TsumugiLuaStack.DoString(ExtraName+ luaClass.ClassName + "=luanet.import_type(\"" + type.ToString()+ "\")");//导入类
                        }
                        MethodInfo[] methods = type.GetMethods();
                        foreach (MethodInfo method in methods)
                        {
                            LuaFunctionAttribute luaFunction = method.GetCustomAttribute<LuaFunctionAttribute>();
                            if (luaFunction != null)
                            {
                                Debug.Log("注册" + ExtraName + luaFunction.Func + "函数到Lua，该函数为:"+ type.ToString()+"." + method.Name+" C#方法");
                                temp.TsumugiLuaStack.RegisterFunction(ExtraName + luaFunction.Func, luaFunction.obj, method);//导入函数
                            }
                        }
                        if (type.IsSubclassOf(typeof(TLuaCallBackBase)))
                        {
                            if (!LoadNow)
                            {
                                temp.TsumugiLuaStack.DoString("luanet.load_assembly(\"" + AssemblyName + "\")");//加载程序集
                            }
                            temp.TsumugiLuaStack.DoString(ExtraName + type.Name + "=luanet.import_type(\"" + type.ToString() + "\")");//导入类
                            TLuaCBFuncManage.AddEventList(type.Name, ExtraName);
                        }
                    }
                }
                else
                {
                    throw new Lua_PluginImportNoFound_Exception("No found " + AssemblyName+"\nPath:"+ AssemblyPath, "PluginImport");
                }
            }
        }
    }
}
