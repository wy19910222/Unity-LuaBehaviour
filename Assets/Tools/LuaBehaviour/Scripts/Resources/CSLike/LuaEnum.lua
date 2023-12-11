--@Author: wangyun
--@CreateTime: 2022-03-15 12:44:38 363
--@LastEditor: wangyun
--@EditTime: 2022-03-15 12:44:38 364

---@class CSLike.LuaEnum : CSLike.Object
---@field public Keys string[] @property
---@field public Values number[] @property
local LuaEnum = CSLike.Class("CSLike", "LuaEnum");
CSLike.LuaEnum = LuaEnum;

--- static
---@private
---@param nameSpace string
---@param className string
---@param initialValues table
---@return CSLike.Object
function LuaEnum.Create(nameSpace, className, initialValues)
	local sortedValues = {};
	for _, value in pairs(initialValues) do
		table.insert(sortedValues, value);
	end
	table.sort(sortedValues);
	
	local keys = {};
	for key, _ in pairs(initialValues) do
		table.insert(keys, key);
	end
	for _, key in ipairs(keys) do
		initialValues[initialValues[key]] = key;
	end
	
	local sortedKeys = {};
	for _, value in pairs(sortedValues) do
		table.insert(sortedKeys, initialValues[value]);
	end
	
	local enumClass = CSLike.Class(nameSpace, className, CSLike.LuaEnum, initialValues);
	CSLike.SetProperty(enumClass, {
		Keys = {
			Get = function(_)
				return sortedKeys;
			end;
		},
		Values = {
			Get = function(_)
				return sortedValues;
			end;
		}
	});
	return enumClass;
end

return LuaEnum;