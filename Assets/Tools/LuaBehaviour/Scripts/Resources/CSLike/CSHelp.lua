--@Author: wangyun
--@CreateTime: 2022-02-24 22:15:41 392
--@LastEditor: wangyun
--@EditTime: 2022-02-24 22:15:41 393

---@param func table | fun(...)
---@param args any[]
local function FuncInvoke(func, args)
	if func then
		local count = args and args.Length or 0;
		local argTable = {};
		for index = 1, count do
			argTable[index] = args[index - 1];
		end
		return func(table.unpack(argTable, 1, count));
	end
	return nil;
end

return {
	FuncInvoke = FuncInvoke;
}