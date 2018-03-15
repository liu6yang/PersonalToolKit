# README #

这套调试lua的方案，结合了公司实际项目结构。使用网上的工具。采用LuaSocket进行数据传输，
能在PC，Mac，Android，iOS上进行调试.

###目录结构###
--Project.xxx //对应公司的xxx目录,使用时可以对比不同，切记覆盖可能造成版本不一致，详情见后面
--Tools //工具集合

### 使用的工具 ###
vscode //ms提供的轻量级IDE
luaide //国内一个开发者基于vscode开发的plugin, 有付费版和免费版本(0.3.7),我们使用免费版本，见tools/kangping.luaide-0.3.7.zip
		免费版本 github：https://github.com/k0204/LuaIde/ 后期项目有需求可以在源码的基础上扩展

推荐一个vscode的插件，能让打开工程的文件及文件夹的icon更容易区分, 点击vscode左侧栏“扩展”(最后一个)，搜索 vscode-icons，安装即可

###步骤###
1.安装vscode
2.进入vscode，点击 文件->首选项->设置 在右侧用户设置里数据
{
    "git.ignoreMissingGitWarning": true,
    "extensions.autoUpdate": false,			//这个必须要填，否者vscode会将安装的luaide更新到最新版本(付费版本)
    "luaide.scriptRoots": [
        "xxx/Assets/xxx/Lua" //具体工程的目录
    ],
    "workbench.iconTheme": "vscode-icons"
}
3.解压 tools/kangping.luaide-0.3.7.zip 到 C:\Users\ASUS\.vscode\extensions ,根据自己情况调整路径
4.重启vscode, 打开文件夹 xxx/Assets/xxx/Lua, 根据自己情况调整路径
5.接下来添加一点代码和文件
	1).将Project.xxx下的文件copy到相应目录
	--editor 	
		--CustomSettings.cs //添加了LuaDebugTool,LuaValueInfo 以生成对应Warp
	--Lua
		--.vscode
			--launch.json		//vscode 启动运行的配置
			--settings.json		//vscode打开xxxx/xxx/Lua后的配置, 我添加了vscode ignore meta文件的配置
		--LuaDebugjit.lua		//Lua调试依赖的代码
		--Main.lua				//在lua代码启动时候，启动调试，这里可配置手机连接PC的IP以 调试手机上的lua代码，注意发版本和更新时候，关掉！
	--Scripts
		--Manager
			--LuaManager.cs		//开启LuaSocket 注意发版本和更新时候，关掉！
		--Utility
			--LuaDebugTool.cs	//调试时，在lua里通过反射，获取C#这边的一些变量信息。注意发版本和更新时候，关掉！
6.调试
	1).和vs操作一样,在vscode里下断点
	2).点击vscode左侧栏"调试"，会在 输出控制台看到 “调试消息端口:7003”，即vscode开了端口，正等客户端发消息过来。
	3).启动运行unity或手机客户端，出现“client connection!” 即表明客户端连接上来了。