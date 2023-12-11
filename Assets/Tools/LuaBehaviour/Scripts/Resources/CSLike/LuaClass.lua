--@Author: wangyun
--@CreateTime: 2022-02-27 05:50:24 152
--@LastEditor: wangyun
--@EditTime: 2022-02-27 05:50:24 153

---@class CSLike : table
CSLike = {};

---@class CSLike.Object : table
---@field public nameSpace string
---@field public className string
---@field public fullClassName string
---@field public class CSLike.Object
---@field public isClass boolean
---@field public super CSLike.Object @static
---@field private ctor fun(instance:CSLike.Object, ...:any) @static

--- static
---@overload fun(nameSpace:string, className:string):CSLike.Object
---@overload fun(nameSpace:string, className:string, super:CSLike.Object):CSLike.Object
---@param nameSpace string
---@param className string
---@param super CSLike.Object
---@param staticVars table
---@return CSLike.Object
function CSLike.Class(nameSpace, className, super, staticVars)
	if not className then
		error("ClassName is nil!");
		return nil;
	end
	if super and not super.isClass then
		error("Super is not a CSLike!");
		return nil;
	end

	---@type CSLike.Object
	local classTable = staticVars or {};
	local classTableMeta = {};
	setmetatable(classTable, classTableMeta);

	---@param table CSLike.Object
	---@param key any
	---@return any
	function classTableMeta.__index(table, key)
		if key == "nameSpace" then
			return nameSpace;
		elseif key == "className" then
			return className;
		elseif key == "fullClassName" then
			return nameSpace and nameSpace .. "." .. className or className;
		elseif key == "super" then
			return super;
		elseif key == "class" then
			return table;
		elseif key == "isClass" then
			return true;
		end

		if super then
			-- avoid return super.ctor
			if key == "ctor" then
				return nil;
			end
			return super[key];
		end
		return nil;
	end

	---@param table CSLike.Object
	---@param key any
	---@param value any
	function classTableMeta.__newindex(table, key, value)
		if key == "nameSpace" or
				key == "className" or
				key == "fullClassName" or
				key == "super" or
				key == "class" or
				key == "isClass" then
			error("Attempt to set a read only value ('" .. key .."')")
			return;
		end
		rawset(table, key, value)
	end

	---@param clsTable CSLike.Object
	---@param ... any
	---@return any
	function classTableMeta.__call(clsTable, ...)
		---@type CSLike.Object
		local instance = {};
		local instanceMeta = {};
		setmetatable(instance, instanceMeta);

		---@param key any
		---@return any
		function instanceMeta.__index(_, key)
			-- 某些关键的key直接返回固定的值
			if key == "class" then
				return clsTable;
			elseif key == "isClass" then
				return false;
			elseif key == "ctor" then
				return nil;
			end
			-- 从类中找值
			return clsTable and clsTable[key];
		end

		---@param table table
		---@param key any
		---@param value any
		function instanceMeta.__newindex(table, key, value)
			-- 某些关键的key不被篡改
			if key == "nameSpace" or
					key == "className" or
					key == "fullClassName" or
					key == "super" or
					key == "class" or
					key == "isClass" or
					key == "ctor" then
				error("Attempt to set a read only value ('" .. key .."')")
				return;
			end
			-- 往对象中赋值
			rawset(table, key, value);
		end

		do
			-- lua没有重载，每个类的构造函数都是唯一的，子类不需要显式调用父类构造函数
			local function create(ct, ...)
				if ct.super then
					create(ct.super, ...);
				end
				if ct.ctor then
					ct.ctor(instance, ...);
				end
			end
			create(clsTable, ...);
		end

		return instance;
	end

	return classTable;
end

---@type table
local propertyHandlesMap = setmetatable({}, {__mode = "k"});
local propertyValuesMap = setmetatable({}, {__mode = "k"});

---@class PropertyHandle : CSLike.Object
---@field public Get fun(t:any):any
---@field public Set fun(t:any, v:any)

--- static
---@param classOrInstance CSLike.Object
---@return table<any, PropertyHandle>, table
function CSLike.GetProperty(classOrInstance)
	if classOrInstance then
		return propertyHandlesMap[classOrInstance], propertyValuesMap[classOrInstance];
	end
	return nil, nil;
end

--- static
---@overload fun(instance:CSLike.Object)
---@overload fun(instance:CSLike.Object, propertyHandles:table<string, PropertyHandle>)
---@param classOrInstance CSLike.Object
---@param propertyHandles table<string, PropertyHandle>
---@param propertyValues table
function CSLike.SetProperty(classOrInstance, propertyHandles, propertyValues)
	if classOrInstance then
		local existPropertyHandles = propertyHandlesMap[classOrInstance];
		if existPropertyHandles then
			if propertyHandles then
				for k, v in pairs(propertyHandles) do
					existPropertyHandles[k] = v;
				end
			end
			local existPropertyValues = propertyValuesMap[classOrInstance];
			if propertyValues then
				for k, v in pairs(propertyValues) do
					existPropertyValues[k] = v;
				end
			end
		else
			propertyHandles = propertyHandles or {};
			propertyHandlesMap[classOrInstance] = propertyHandles;
			propertyValues = propertyValues or {};
			propertyValuesMap[classOrInstance] = propertyValues;

			---@type table
			local meta = getmetatable(classOrInstance);
			if not meta then
				meta = {};
				setmetatable(classOrInstance, meta);
			end
			---@type fun(t:any, k:any):any
			local __index = meta.__index;
			if type(__index) == "table" then
				local __indexTable = __index;
				__index = function(t, k) return __indexTable[k]; end;
			end
			---@type fun(t:any, k:any, v:any):any
			local __newindex = meta.__newindex;
			if type(__newindex) == "table" then
				local __newIndexTable = __newindex;
				__newindex = function(t, k, v) __newIndexTable[k] = v; end;
			end
			---@param t table
			---@param k any
			---@return any
			function meta.__index(t, k)
				local property = propertyHandles[k];
				if property then
					return property.Get and property.Get(propertyValues);
				end
				if __index then
					return __index(t, k);
				end
				return rawget(t, k);
			end
			---@param t table
			---@param k any
			---@param v any
			function meta.__newindex(t, k, v)
				local property = propertyHandles[k];
				if property then
					if property.Set then
						property.Set(propertyValues, v);
					end
					return;
				end
				if __newindex then
					__newindex(t, k, v);
				end
				rawset(t, k, v);
			end

		end
	end
end

--- static
---@param class CSLike.Object
---@return CSLike.Type | System.Type
function CSLike.Typeof(class)
	if not class then
		return nil;
	end
	if class.isClass then
		return CSLike.Type(class);
	end
	-- function "typeof" is come from XLua
	return typeof(class);
end

--- static
---@param instance CSLike.Object
---@return boolean
function CSLike.IsInstanceOf(instance, class)
	if not instance or not class or not class.isClass then
		return false;
	end
	local tempClass = instance.class;
	while tempClass do
		if tempClass == class then
			return true;
		end
		tempClass = tempClass.super;
	end
	return false;
end

---@class CSLike.Type : CSLike.Object
---@field public Name string
---@field private targetClass CSLike.Object
local Type = CSLike.Class("CSLike", "Type");
CSLike.Type = Type;

---@param targetClass CSLike.Object
function Type:ctor(targetClass)
	self.targetClass = targetClass;

	---@param type1 CSLike.Type
	---@param type2 CSLike.Type
	---@return boolean
	local function typeEquals(type1, type2)
		return type1.targetClass == type2.targetClass;
	end;
	getmetatable(self).__eq = typeEquals;

	---@type table<any, PropertyHandle>
	local propertyHandles = {
		---@type PropertyHandle
		Name = {
			Get = function(_)
				return targetClass.fullClassName;
			end;
		};
	};
	CSLike.SetProperty(self, propertyHandles)
end

return CSLike;