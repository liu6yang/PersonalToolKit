--require('mobdebug').start()
require "Logic/TimeSample"

local function EnableDebug()
	local breakSocketHandle,debugXpCall = require("LuaDebugjit")("localhost",7003)
	local timer = Timer.New(function()
	breakSocketHandle() end, 1, -1, false)
	timer:Start()
end

--主入口函数。从这里开始lua逻辑
function Main()					
	EnableDebug()
end
--场景切换通知
function OnLevelWasLoaded(level)
	Time.timeSinceLevelLoad = 0
end

