using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TLua.TsumugiLua;

namespace TLua
{
    //事件管理
    public static class TLuaCBFuncManage
    {
        public static Dictionary<string, bool> EventToBool = new Dictionary<string, bool>();//状态标记判断
        public static Dictionary<string, ArrayList> EventToFunc = new Dictionary<string, ArrayList>();//函数存储
        public static ArrayList UseEventName = new ArrayList();//UseEvent的其中一种只能用一次
        public static ArrayList EventList = new ArrayList();//事件列表
        public static ArrayList EventExtra = new ArrayList();//额外名

        public static void AddEventList(string Event,string Extra)
        {
            EventList.Add(Event);
            EventToFunc.Add(Event, new ArrayList());
            EventToBool.Add(Event, false);
            EventExtra.Add(Extra);
        }

        public static void TestCallBack(string EventName,string ExtraName)//测试事件是否写了回调函数
        {
            TsumugiLuaManage tempLua = LuaManage[i] as TsumugiLuaManage;
            NLua.Lua temp = tempLua.TsumugiLuaStack;
            TLuaCallBackBase tempEvent = temp[ExtraName + EventName] as TLuaCallBackBase;//转成其基类
            if (tempEvent.getFlag())//提取其标识
            {
                foreach (NLua.LuaFunction luaFunction in tempEvent.Func)//用循环遍历其Func来获取函数标识符
                {
                    EventToFunc[EventName].Add(luaFunction);//写入数组
                }
                EventToBool[EventName] = false;//重置它，使下一个模组不冲突
            }
        }
        public static void LoadCallBack(string EventName, params object[] par)//第一个为事件名，第二个为参数
        {
            if (EventToFunc[EventName].Count != 0)
            {
                foreach (NLua.LuaFunction luaFunction in EventToFunc[EventName])
                {
                    luaFunction.Call(par);
                }
            }
            return;
        }
        public static void Lua_UseEvent(string EventName)
        {
            foreach (string str in UseEventName)
            {//先判断是不是已有的
                if (str == EventName)
                {
                    return;//如果是有的不加载
                }
            }
            TsumugiLuaManage temp = LuaManage[i] as TsumugiLuaManage;//类型转换
            foreach (string str in EventList)
            {//遍历字符串
                if (str == EventName)
                {//如果遍历到了是有的
                    string ExtraName = EventExtra[EventList.IndexOf(str)] as string;
                    temp.TsumugiLuaStack.DoString(ExtraName + EventName + " = " + ExtraName + EventName + "()");//那么加载它
                    EventToBool[EventName] = true;
                    UseEventName.Add(EventName);//如果是没有的就增加，并且它得是才能增加到里面
                    return;
                }
            }
            throw new Lua_UseEvent_Exception("No " + EventName + " Event", "UseEvent");//如果没有事件抛异常
        }
    }
    //事件基类
    public abstract class TLuaCallBackBase
    {
        public TLuaCallBackBase()
        {
            Func = new ArrayList();
        }
        public ArrayList Func;
        public bool HasBool = false;
        public void Event(params NLua.LuaFunction[] luaFunctionHeap)
        {
            foreach (NLua.LuaFunction luaFunction in luaFunctionHeap)
            {
                Func.Add(luaFunction);
            }
            HasBool = true;
        }
        public bool getFlag()
        {
            return HasBool;
        }
    }
}
