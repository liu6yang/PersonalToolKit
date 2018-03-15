using UnityEngine;
using System.Collections;
using LuaInterface;

namespace JJYGame
{
    public class LuaManager : Manager
    {
        private LuaState lua;
        public LuaState getLuaState()
        {
            return lua;
        }
        private LuaLoader loader;
        private LuaLooper loop = null;

        // Use this for initialization
        void Awake()
        {
            loader = new LuaLoader();
            lua = new LuaState();
            this.OpenLibs();
            lua.LuaSetTop(0);
            LuaBinder.Bind(lua);
            LuaCoroutine.Register(lua, this);
        }
        public void InitStart()
        {
            InitLuaPath();
            InitLuaBundle();
            this.lua.Start();    //启动LUAVM
            this.StartMain();
            this.StartLooper();
        }

        void StartLooper()
        {
            loop = gameObject.AddComponent<LuaLooper>();
            loop.luaState = lua;
        }

        //cjson 比较特殊，只new了一个table，没有注册库，这里注册一下
        protected void OpenCJson()
        {
            lua.LuaGetField(LuaIndexes.LUA_REGISTRYINDEX, "_LOADED");
            lua.OpenLibs(LuaDLL.luaopen_cjson);
            lua.LuaSetField(-2, "cjson");

            lua.OpenLibs(LuaDLL.luaopen_cjson_safe);
            lua.LuaSetField(-2, "cjson.safe");
        }

        void StartMain()
        {
            lua.DoFile("Main.lua");

            LuaFunction main = lua.GetFunction("Main");
            main.Call();
            main.Dispose();
            main = null;
        }

        /// <summary>
        /// 初始化加载第三方库
        /// </summary>
        void OpenLibs()
        {
            lua.OpenLibs(LuaDLL.luaopen_pb);
            lua.OpenLibs(LuaDLL.luaopen_sproto_core);
            lua.OpenLibs(LuaDLL.luaopen_protobuf_c);
            lua.OpenLibs(LuaDLL.luaopen_lpeg);
            lua.OpenLibs(LuaDLL.luaopen_bit);
            lua.OpenLibs(LuaDLL.luaopen_socket_core);

            OpenLuaSocket();

            this.OpenCJson();
        }

        protected void OpenLuaSocket()
        {
            LuaConst.openLuaSocket = true;

            lua.BeginPreLoad();
            lua.RegFunction("socket.core", LuaOpen_Socket_Core);
            lua.RegFunction("mime.core", LuaOpen_Mime_Core);
            lua.EndPreLoad();
        }

        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int LuaOpen_Socket_Core(System.IntPtr L)
        {
            return LuaDLL.luaopen_socket_core(L);
        }

        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int LuaOpen_Mime_Core(System.IntPtr L)
        {
            return LuaDLL.luaopen_mime_core(L);
        }

        /// <summary>
        /// 初始化Lua代码加载路径
        /// </summary>
        void InitLuaPath()
        {
            if (AppConst.DebugMode)
            {
                string rootPath = AppConst.FrameworkRoot;
                lua.AddSearchPath(rootPath + "/Lua");
                lua.AddSearchPath(rootPath + "/ToLua/Lua");
#if DEBUG_TEST
                lua.AddSearchPath(Application.dataPath + "/StreamingAssets/Lua");
#endif
            }
            else
            {
                lua.AddSearchPath(Util.DataPath + "lua");
            }
        }

        /// <summary>
        /// 初始化LuaBundle
        /// </summary>
        void InitLuaBundle()
        {
            Util.LogError("InitLuaBundle");
            if (loader.beZip)
            {
                //查找所有的unity3d
                string[] filesNames = System.IO.Directory.GetFiles(Util.DataPath + "lua/", "*.unity3d");
                foreach (string filename in filesNames)
                {
                    string luaName = filename.Replace('\\', '/');
                    luaName = luaName.Substring(luaName.LastIndexOf('/') + 1);
                    loader.AddBundle("lua/" + luaName);
                }

                //loader.AddBundle("lua/lua.unity3d");
                //loader.AddBundle("lua/lua_logic_base.unity3d");
            }
        }

        public object[] DoFile(string filename)
        {
            return lua.DoFile(filename);
        }

        // Update is called once per frame
        public object[] CallFunction(string funcName, params object[] args)
        {
            LuaFunction func = lua.GetFunction(funcName);
            if (func != null)
            {
                return func.Call(args);
            }
            return null;
        }

        public void LuaGC()
        {
            lua.LuaGC(LuaGCOptions.LUA_GCCOLLECT);
        }

        public void Close()
        {
            loop.Destroy();
            loop = null;

            lua.Dispose();
            lua = null;
            loader = null;
        }
    }
}