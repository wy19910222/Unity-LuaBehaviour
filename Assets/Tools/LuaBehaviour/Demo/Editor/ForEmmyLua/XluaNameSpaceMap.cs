using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ICSharpCode.SharpZipLib.Zip;

public class XLuaNameSpaceMap : Editor
{
	private const string FILE_NAME = "NameSpaceMap";
	private static readonly string[] EXT_NAMESPACE = { typeof(AssetDatabase).Namespace?.Split('.')[0] };
	private static readonly string OUTPUT_PATH = Application.dataPath + "/../LuaLib";

	[MenuItem("XLua/EmmyLua/GenerateNameSpaceMap/IDEA", false, -100)]
	public static void GenZip()
	{
		GenAll(true);
	}

	[MenuItem("XLua/EmmyLua/GenerateNameSpaceMap/Rider | VSCode | Other", false, -100)]
	public static void GenLuaFile()
	{
		GenAll(false);
	}

	public static void GenAll(bool compressToZip)
	{
		HashSet<string> nameSpaceSet = new HashSet<string> {"System"};
		Array.ForEach(EXT_NAMESPACE, ns => nameSpaceSet.Add(ns));
		List<string> nameSpaceList = new List<string>(nameSpaceSet);
		HashSet<string> noNameSpaceTypeSet = new HashSet<string>();
		List<string> noNameSpaceTypeList = new List<string>(noNameSpaceTypeSet);

		CSObjectWrapEditor.Generator.GetGenConfig(XLua.Utils.GetAllTypes());
		foreach (Type type in CSObjectWrapEditor.Generator.LuaCallCSharp)
		{
			if (string.IsNullOrEmpty(type.Namespace))
			{
				Type t = type;
				while (t.DeclaringType != null)
				{
					t = t.DeclaringType;
				}
				string typeName = t.FullName;
				if (!noNameSpaceTypeSet.Contains(typeName))
				{
					noNameSpaceTypeSet.Add(typeName);
					noNameSpaceTypeList.Add(typeName);
				}
			}
			else
			{
				string namespaceName = type.Namespace.Split('.')[0];
				if (!nameSpaceSet.Contains(namespaceName))
				{
					nameSpaceSet.Add(namespaceName);
					nameSpaceList.Add(namespaceName);
				}
			}
		}
		nameSpaceList.Sort();
		noNameSpaceTypeList.Sort();

		Dictionary<string, byte[]> fileDict = new Dictionary<string, byte[]>();

		StringBuilder mapSb = new StringBuilder();
		mapSb.AppendLine("CS = {");
		foreach (string nameSpace in nameSpaceList)
		{
			mapSb.Append("\t");
			mapSb.Append(nameSpace);
			mapSb.Append(" = ");
			mapSb.Append(nameSpace);
			mapSb.AppendLine(";");
		}
		mapSb.AppendLine();
		foreach (string typeName in noNameSpaceTypeList)
		{
			mapSb.Append("\t");
			mapSb.Append(typeName);
			mapSb.Append(" = ");
			mapSb.Append(typeName);
			mapSb.AppendLine(";");
		}
		mapSb.Append("}");

		const string mapFileName = FILE_NAME + ".lua";
		byte[] mapBytes = Encoding.UTF8.GetBytes(mapSb.ToString());
		fileDict[mapFileName] = mapBytes;

		StringBuilder utilSb = new StringBuilder();
		utilSb.AppendLine("---@param CSClass table");
		utilSb.AppendLine("---@return System.Type");
		utilSb.AppendLine("function typeof(CSClass) end");
		utilSb.AppendLine();
		TextAsset ta = Resources.Load<TextAsset>("xlua/util.lua");
		if (ta)
		{
			string text = ta.text;
			text = text.Replace("local function cs_generator(func",
				"---@param func fun()" + Environment.NewLine +
				"---@param ... any[]" + Environment.NewLine +
				"---@return System.Collections.IEnumerator" + Environment.NewLine +
				"local function cs_generator(func");
			int index = text.LastIndexOf("return {");
			if (index != -1)
			{
				utilSb.Append(text.Substring(0, index));
				utilSb.Append("util = {");
				utilSb.Append(text.Substring(index + "return {".Length));
				utilSb.Append("return util");
			}
		}
		const string utilFileName = "util.lua";
		byte[] utilBytes = Encoding.UTF8.GetBytes(utilSb.ToString());
		fileDict[utilFileName] = utilBytes;

		if (compressToZip)
		{
			string zipFileName = OUTPUT_PATH + "/" + FILE_NAME + ".zip";
			WriteToZip(zipFileName, fileDict);
		}
		else
		{
			string dirPath = OUTPUT_PATH + "/";
			WriteToFolder(dirPath, fileDict, false);
		}

		Debug.Log("NameSpaceMap generating is complete!");
	}

	private static void WriteToZip(string zipFileName, Dictionary<string, byte[]> fileDict, int compressionLevel = 9)
	{
		FileInfo zipFile = new FileInfo(zipFileName);
		if (zipFile.Exists)
		{
			zipFile.IsReadOnly = false;
			zipFile.Delete();
		}
		DirectoryInfo dir = zipFile.Directory;
		if (dir != null && !dir.Exists)
		{
			dir.Create();
		}

		using (FileStream fileStream = zipFile.Create())
		{
			using (ZipOutputStream zipStream = new ZipOutputStream(fileStream))
			{
				zipStream.SetLevel(compressionLevel);
				foreach (string fileName in fileDict.Keys)
				{
					zipStream.PutNextEntry(new ZipEntry(fileName));
					byte[] buffer = fileDict[fileName];
					zipStream.Write(buffer, 0, buffer.Length);
				}
			}
		}
	}

	private static void WriteToFolder(string dirPath, Dictionary<string, byte[]> fileDict, bool clearDir)
	{
		if (Directory.Exists(dirPath))
		{
			if (clearDir)
			{
				string[] existFilePaths = Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories);
				foreach (string filePath in existFilePaths)
				{
					FileInfo file = new FileInfo(filePath)
					{
						IsReadOnly = false
					};
					file.Delete();
				}
				Directory.Delete(dirPath, true);
				Directory.CreateDirectory(dirPath);
			}
		}
		else
		{
			Directory.CreateDirectory(dirPath);
		}
		
		foreach (string filename in fileDict.Keys)
		{
			string filePath = dirPath + "/" + filename.Replace('|', '_');
			byte[] bytes = fileDict[filename];
			FileInfo file = new FileInfo(filePath);
			if (!clearDir && file.Exists)
			{
				file.IsReadOnly = false;
				file.Delete();
			}
			using (FileStream fs = file.OpenWrite())
			{
				fs.Write(bytes, 0, bytes.Length); 
				fs.Flush();
			}
			file.IsReadOnly = true;
		}
	}
}
