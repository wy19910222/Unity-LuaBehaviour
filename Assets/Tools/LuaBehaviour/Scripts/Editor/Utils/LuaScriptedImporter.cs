using System.IO;
using UnityEngine;
#if UNITY_2020_3_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

[ScriptedImporter(1, "lua")]
public class LuaScriptedImporter : ScriptedImporter {
	public override void OnImportAsset(AssetImportContext ctx) {
		TextAsset lua = new TextAsset(File.ReadAllText(ctx.assetPath));
		ctx.AddObjectToAsset("main", lua);
		ctx.SetMainObject(lua);
	}
}
