/*
 * Tencent is pleased to support the open source community by making xLua available.
 * Copyright (C) 2016 THL A29 Limited, a Tencent company. All rights reserved.
 * Licensed under the MIT License (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
 * http://opensource.org/licenses/MIT
 * Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
*/

using System.Collections.Generic;
using System.Collections;
using System;
using XLua;
using System.Reflection;
using System.Linq;

//配置的详细介绍请看Doc下《XLua的配置.doc》
public static class XLuaGenConfig
{
    /***************如果你全lua编程，可以参考这份自动化配置***************/
    //--------------begin 纯lua编程配置参考----------------------------
    private static readonly List<string> exclude = new List<string> {
       "HideInInspector", "ExecuteInEditMode",
       "AddComponentMenu", "ContextMenu",
       "RequireComponent", "DisallowMultipleComponent",
       "SerializeField", "AssemblyIsEditorAssembly",
       "Attribute", "Types",
       "UnitySurrogateSelector", "TrackedReference",
       "TypeInferenceRules", "FFTWindow",
       // "Network",
       "UnityEngine.ClusterNetwork", "System.IO.Stream",
       "RPC", "MasterServer",
       "BitStream", "HostData",
       "ConnectionTesterStatus", "GUI", "EventType",
       "EventModifiers", "FontStyle", "TextAlignment",
       "TextEditor", "TextEditorDblClickSnapping",
       "TextGenerator", "TextClipping", "Gizmos",
       "ADBannerView", "ADInterstitialAd",
       "Android", "Tizen", "jvalue",
       "iPhone", "iOS", "Windows", "CalendarIdentifier",
       "CalendarUnit", "CalendarUnit",
       "ClusterInput", "FullScreenMovieControlMode",
       "FullScreenMovieScalingMode", "Handheld",
       "LocalNotification", "NotificationServices",
       "RemoteNotificationType", "RemoteNotification",
       "SamsungTV", "TextureCompressionQuality",
       "TouchScreenKeyboardType", "TouchScreenKeyboard",
       "MovieTexture", "UnityEngineInternal",
       "Terrain", "Tree", "SplatPrototype",
       "DetailPrototype", "DetailRenderMode",
       "MeshSubsetCombineUtility", "AOT", "Social", "Enumerator",
       "SendMouseEvents", "Cursor", "Flash", "ActionScript",
       "OnRequestRebuild", "Ping",
       "ShaderVariantCollection", "SimpleJson.Reflection",
       "CoroutineTween", "GraphicRebuildTracker",
       "Advertisements", "UnityEditor", "WSA",
       "EventProvider", "Apple",
       "ClusterInput", "Motion",
       "UnityEngine.UI.ReflectionMethodsCache", "NativeLeakDetection",
       "NativeLeakDetectionMode", "WWWAudioExtensions", "UnityEngine.Experimental",
       
       "InputManagerEntry", "InputRegistering",

        // 发现有问题自己加的
       "Wind","VehiclesModule",
       "UnityAnalytics", "Tilemap", 
       "JSONSerialize", "Cloth", "WebCamTexture", "Microphone",
       "CanvasRenderer", "MaterialChecks",
       // 不需要功能的话，不要生成Wrap，因为一旦生成Wrap就不会被裁减掉
       // LocationService LocationInfo 定位功能
       // WebCamTexture 使用摄像头权限 Microphone 使用录音权限 Handheld 震动权限
       
       "UnityEngine.Rendering.CoreRenderPipelinePreferences", "UnityEngine.Rendering.ResourceReloader",
       
       // Unity2021
       "UnityEngine.ClusterSerialization",
       "UnityEngine.CloudStreaming",
       "TMPro.TMP_EditorResourceManager",
       "TMPro.TMP_PackageResourceImporter",

       // UpgradeTo2022
       // "UnityEngine.Rendering.AdditionalGIBakeRequestsManager",
       // "UnityEngine.GamepadSpeakerOutputType",
       // "UnityEngine.TextureMipmapLimitGroups",
       // "UnityEngine.Rendering.SceneRenderPipeline",
       // "UnityEngine.Rendering.ProbeVolume",
       // "UnityEngine.Rendering.ProbeVolumePerSceneData",
       // "UnityEngine.Rendering.ProbeTouchupVolume",
    };

    private static bool isExcluded(Type type)
    {
        var fullName = type.FullName;
        foreach (var t in exclude)
        {
            if (fullName != null && fullName.Contains(t))
            {
                return true;
            }
        }
        return false;
    }

    [LuaCallCSharp]
    public static IEnumerable<Type> LuaCallCSharp
    {
        get {
            List<string> namespaces = new List<string>() // 在这里添加命名空间
            {
                "System.IO",
                "UnityEngine",
                "UnityEngine.UI",
                "UnityEngine.Events",
                "UnityEngine.EventSystems",
                "UnityEngine.Audio",
                "UnityEngine.Video",
                "UnityEngine.Animations",
                "UnityEngine.Playables",
                "UnityEngine.Networking",
                "UnityEngine.Rendering",
            };
            var unityTypes = from assembly in AppDomain.CurrentDomain.GetAssemblies()
                where !(assembly.ManifestModule is System.Reflection.Emit.ModuleBuilder)
                from type in assembly.GetExportedTypes()
                where type.Namespace != null && namespaces.Contains(type.Namespace) && !isExcluded(type) &&
                        type.BaseType != typeof(MulticastDelegate) && !type.IsInterface && !type.IsEnum
                select type;

            string[] customAssemblies =
            {
                "Assembly-CSharp-firstpass",
                "Assembly-CSharp",
                "DOTween",
                "DOTweenPro",
            };
            var customTypes = from assembly in AppDomain.CurrentDomain.GetAssemblies()
                where customAssemblies.Contains(assembly.GetName().Name)
                from type in assembly.GetExportedTypes()
                where (type.Namespace == null || !type.Namespace.StartsWith("XLua")) && !isExcluded(type) &&
                        type.BaseType != typeof(MulticastDelegate) && !type.IsInterface && !type.IsEnum
                select type;
            return unityTypes.Concat(customTypes);
        }
    }

    ////自动把LuaCallCSharp涉及到的delegate加到CSharpCallLua列表，后续可以直接用lua函数做callback
    [CSharpCallLua]
    public static List<Type> CSharpCallLua
    {
        get
        {
            var lua_call_csharp = LuaCallCSharp;
            var delegate_types = new List<Type>();
            
            const BindingFlags flag = BindingFlags.Instance | BindingFlags.Static |
                    BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly;
            foreach (var field in (from type in lua_call_csharp select type).SelectMany(type => type.GetFields(flag)))
            {
                if (typeof(Delegate).IsAssignableFrom(field.FieldType))
                {
                    delegate_types.Add(field.FieldType);
                }
            }

            foreach (var method in (from type in lua_call_csharp select type).SelectMany(type => type.GetMethods(flag)))
            {
                if (typeof(Delegate).IsAssignableFrom(method.ReturnType))
                {
                    delegate_types.Add(method.ReturnType);
                }
                foreach (var param in method.GetParameters())
                {
                    var paramType = param.ParameterType.IsByRef ? param.ParameterType.GetElementType() : param.ParameterType;
                    if (typeof(Delegate).IsAssignableFrom(paramType))
                    {
                        delegate_types.Add(paramType);
                    }
                }
            }
            
            delegate_types = delegate_types.Where(t => t.BaseType == typeof(MulticastDelegate) && !hasGenericParameter(t) && !delegateHasEditorRef(t)).Distinct().ToList();

            delegate_types.AddRange(new List<Type>()
            {
                typeof(IEnumerator),    // Coroutine
                typeof(Action),     // 常用
                typeof(UnityEngine.Events.UnityAction),     // 常用
                
                // UnityEngine.UIScrollRect.ScrollRectEvent.AddListener
                typeof(UnityEngine.Events.UnityAction<UnityEngine.Vector2>),
            });
            
            return delegate_types;
        }
    }
    //--------------end 纯lua编程配置参考----------------------------

    /***************热补丁可以参考这份自动化配置***************/
    [Hotfix]
    private static IEnumerable<Type> HotfixInject =>
            from type in Assembly.Load("Assembly-CSharp").GetTypes()
            where type.Namespace == null || !type.Namespace.StartsWith("XLua")
            select type;
    
    //--------------begin 热补丁自动化配置-------------------------
    private static bool hasGenericParameter(Type type)
    {
        if (type.IsGenericTypeDefinition) return true;
        if (type.IsGenericParameter) return true;
        if (type.IsByRef || type.IsArray)
        {
            return hasGenericParameter(type.GetElementType());
        }
        if (type.IsGenericType)
        {
            foreach (var typeArg in type.GetGenericArguments())
            {
                if (hasGenericParameter(typeArg))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static bool typeHasEditorRef(Type type)
    {
        if (type.Namespace != null && (type.Namespace == "UnityEditor" || type.Namespace.StartsWith("UnityEditor.")))
        {
            return true;
        }
        if (type.IsNested)
        {
            return typeHasEditorRef(type.DeclaringType);
        }
        if (type.IsByRef || type.IsArray)
        {
            return typeHasEditorRef(type.GetElementType());
        }
        if (type.IsGenericType)
        {
            foreach (var typeArg in type.GetGenericArguments())
            {
                if (typeArg.IsGenericParameter) {
                    //skip unsigned type parameter
                    continue;
                } 
                if (typeHasEditorRef(typeArg))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static bool delegateHasEditorRef(Type delegateType)
    {
        if (typeHasEditorRef(delegateType))
        {
            return true;
        }
        var method = delegateType.GetMethod("Invoke");
        if (method == null)
        {
            return false;
        }
        return typeHasEditorRef(method.ReturnType) || method.GetParameters().Any(pInfo => typeHasEditorRef(pInfo.ParameterType));
    }

    //黑名单
    [BlackList]
    public static List<List<string>> BlackList = new List<List<string>>()  {
                new List<string>(){"UnityEngine.AnimatorControllerParameter", "name"},
                new List<string>(){"UnityEngine.AudioSettings", "GetSpatializerPluginNames"},
                new List<string>(){"UnityEngine.AudioSettings", "SetSpatializerPluginName", "System.String"},
                new List<string>(){"UnityEngine.Caching", "SetNoBackupFlag", "System.String", "System.Int32"},
                new List<string>(){"UnityEngine.Caching", "SetNoBackupFlag", "System.String", "UnityEngine.Hash128"},
                new List<string>(){"UnityEngine.Caching", "SetNoBackupFlag", "UnityEngine.CachedAssetBundle"},
                new List<string>(){"UnityEngine.Caching", "ResetNoBackupFlag", "System.String", "System.Int32"},
                new List<string>(){"UnityEngine.Caching", "ResetNoBackupFlag", "System.String", "UnityEngine.Hash128"},
                new List<string>(){"UnityEngine.Caching", "ResetNoBackupFlag", "UnityEngine.CachedAssetBundle"},
                new List<string>(){"UnityEngine.DrivenRectTransformTracker", "StartRecordingUndo"},
                new List<string>(){"UnityEngine.DrivenRectTransformTracker", "StopRecordingUndo"},
                new List<string>(){"UnityEngine.Input", "IsJoystickPreconfigured", "System.String"},
                new List<string>(){"UnityEngine.Light", "SetLightDirty"},
                new List<string>(){"UnityEngine.Light", "shadowRadius"},
                new List<string>(){"UnityEngine.Light", "shadowAngle"},
                new List<string>(){"UnityEngine.LightProbeGroup", "dering"},
                new List<string>(){"UnityEngine.LightProbeGroup", "probePositions"},
                new List<string>(){"UnityEngine.MeshRenderer", "scaleInLightmap"},
                new List<string>(){"UnityEngine.MeshRenderer", "receiveGI"},
                new List<string>(){"UnityEngine.MeshRenderer", "stitchLightmapSeams"},
                new List<string>(){"UnityEngine.ParticleSystemForceField", "FindAll"},
                new List<string>(){"UnityEngine.ParticleSystemRenderer", "supportsMeshInstancing"},
                new List<string>(){"UnityEngine.QualitySettings", "streamingMipmapsRenderersPerFrame"},
                new List<string>(){"UnityEngine.Texture", "imageContentsHash"},
                new List<string>(){"UnityEngine.UI.DefaultControls", "factory"},    // set是UNITY_EDITOR
                new List<string>(){"UnityEngine.UI.Graphic", "OnRebuildRequested"},
                new List<string>(){"UnityEngine.UI.Text", "OnRebuildRequested"},
                new List<string>(){"UnityEngine.Rendering.ScriptableRenderContext", "EmitWorldGeometryForSceneView", "UnityEngine.Camera"},
                new List<string>(){"UnityEngine.Rendering.XRGraphics", "tryEnable"},
                new List<string>(){"UnityEngine.Rendering.RenderPipelineAsset", "terrainBrushPassIndex"},
                new List<string>(){"UnityEngine.Playables.PlayableGraph", "GetEditorName"},
                
                // Unity2021
                new List<string>(){"UnityEngine.AudioSource", "PlayOnGamepad", "System.Int32"},
                new List<string>(){"UnityEngine.AudioSource", "DisableGamepadOutput"},
                new List<string>(){"UnityEngine.AudioSource", "SetGamepadSpeakerMixLevel", "System.Int32", "System.Int32"},
                new List<string>(){"UnityEngine.AudioSource", "SetGamepadSpeakerMixLevelDefault", "System.Int32"},
                new List<string>(){"UnityEngine.AudioSource", "SetGamepadSpeakerRestrictedAudio", "System.Int32", "System.Boolean"},
                new List<string>(){"UnityEngine.AudioSource", "GamepadSpeakerSupportsOutputType", "UnityEngine.GamepadSpeakerOutputType"},
                new List<string>(){"UnityEngine.AudioSource", "gamepadSpeakerOutputType"},
                new List<string>(){"UnityEngine.LightingSettings", "autoGenerate"},
                new List<string>(){"UnityEngine.LightingSettings", "mixedBakeMode"},
                new List<string>(){"UnityEngine.LightingSettings", "albedoBoost"},
                new List<string>(){"UnityEngine.LightingSettings", "indirectScale"},
                new List<string>(){"UnityEngine.LightingSettings", "lightmapper"},
                new List<string>(){"UnityEngine.LightingSettings", "lightmapMaxSize"},
                new List<string>(){"UnityEngine.LightingSettings", "lightmapResolution"},
                new List<string>(){"UnityEngine.LightingSettings", "lightmapPadding"},
                new List<string>(){"UnityEngine.LightingSettings", "lightmapCompression"},
                new List<string>(){"UnityEngine.LightingSettings", "ao"},
                new List<string>(){"UnityEngine.LightingSettings", "aoMaxDistance"},
                new List<string>(){"UnityEngine.LightingSettings", "aoExponentIndirect"},
                new List<string>(){"UnityEngine.LightingSettings", "aoExponentDirect"},
                new List<string>(){"UnityEngine.LightingSettings", "extractAO"},
                new List<string>(){"UnityEngine.LightingSettings", "directionalityMode"},
                new List<string>(){"UnityEngine.LightingSettings", "exportTrainingData"},
                new List<string>(){"UnityEngine.LightingSettings", "trainingDataDestination"},
                new List<string>(){"UnityEngine.LightingSettings", "indirectResolution"},
                new List<string>(){"UnityEngine.LightingSettings", "finalGather"},
                new List<string>(){"UnityEngine.LightingSettings", "finalGatherRayCount"},
                new List<string>(){"UnityEngine.LightingSettings", "finalGatherFiltering"},
                new List<string>(){"UnityEngine.LightingSettings", "sampling"},
                new List<string>(){"UnityEngine.LightingSettings", "directSampleCount"},
                new List<string>(){"UnityEngine.LightingSettings", "indirectSampleCount"},
                new List<string>(){"UnityEngine.LightingSettings", "maxBounces"},
                new List<string>(){"UnityEngine.LightingSettings", "minBounces"},
                new List<string>(){"UnityEngine.LightingSettings", "prioritizeView"},
                new List<string>(){"UnityEngine.LightingSettings", "filteringMode"},
                new List<string>(){"UnityEngine.LightingSettings", "denoiserTypeDirect"},
                new List<string>(){"UnityEngine.LightingSettings", "denoiserTypeIndirect"},
                new List<string>(){"UnityEngine.LightingSettings", "denoiserTypeAO"},
                new List<string>(){"UnityEngine.LightingSettings", "filterTypeDirect"},
                new List<string>(){"UnityEngine.LightingSettings", "filterTypeIndirect"},
                new List<string>(){"UnityEngine.LightingSettings", "filterTypeAO"},
                new List<string>(){"UnityEngine.LightingSettings", "filteringGaussRadiusDirect"},
                new List<string>(){"UnityEngine.LightingSettings", "filteringGaussRadiusIndirect"},
                new List<string>(){"UnityEngine.LightingSettings", "filteringGaussRadiusAO"},
                new List<string>(){"UnityEngine.LightingSettings", "filteringAtrousPositionSigmaDirect"},
                new List<string>(){"UnityEngine.LightingSettings", "filteringAtrousPositionSigmaIndirect"},
                new List<string>(){"UnityEngine.LightingSettings", "filteringAtrousPositionSigmaAO"},
                new List<string>(){"UnityEngine.LightingSettings", "environmentSampleCount"},
                new List<string>(){"UnityEngine.LightingSettings", "lightProbeSampleCountMultiplier"},
                new List<string>(){"UnityEngine.QualitySettings", "GetAllRenderPipelineAssetsForPlatform", "System.String", "System.Collections.Generic.List`1[UnityEngine.Rendering.RenderPipelineAsset]&"}, // ?
                new List<string>(){"UnityEngine.Rendering.DebugUI+Table", "scroll"},
                new List<string>(){"UnityEngine.Rendering.DebugUI+Table", "Header"},
                new List<string>(){"UnityEngine.Rendering.GraphicsSettings", "videoShadersIncludeMode"},
                new List<string>(){"UnityEngine.Rendering.CoreUtils", "EnsureFolderTreeInAssetFilePath", "System.String"},
                
                // Unity2022
                // new List<string>(){ "UnityEngine.Material", "IsChildOf", "UnityEngine.Material"},
                // new List<string>(){ "UnityEngine.Material", "RevertAllPropertyOverrides"},
                // new List<string>(){ "UnityEngine.Material", "IsPropertyOverriden", "System.Int32"}, // ?
                // new List<string>(){ "UnityEngine.Material", "IsPropertyOverriden", "System.String"}, // ?
                // new List<string>(){ "UnityEngine.Material", "IsPropertyLocked", "System.Int32"}, // ?
                // new List<string>(){ "UnityEngine.Material", "IsPropertyLocked", "System.String"}, // ?
                // new List<string>(){ "UnityEngine.Material", "IsPropertyLockedByAncestor", "System.Int32"}, // ?
                // new List<string>(){ "UnityEngine.Material", "IsPropertyLockedByAncestor", "System.String"}, // ?
                // new List<string>(){ "UnityEngine.Material", "SetPropertyLock", "System.Int32", "System.Boolean"},
                // new List<string>(){ "UnityEngine.Material", "SetPropertyLock", "System.String", "System.Boolean"}, // ?
                // new List<string>(){ "UnityEngine.Material", "ApplyPropertyOverride", "UnityEngine.Material", "System.Int32", "System.Boolean"},
                // new List<string>(){ "UnityEngine.Material", "ApplyPropertyOverride", "UnityEngine.Material", "System.String", "System.Boolean"}, // ?
                // new List<string>(){ "UnityEngine.Material", "RevertPropertyOverride", "System.Int32"},
                // new List<string>(){ "UnityEngine.Material", "RevertPropertyOverride", "System.String"}, // ?
                // new List<string>(){ "UnityEngine.Material", "parent"},
                // new List<string>(){ "UnityEngine.Material", "isVariant"},
                // new List<string>(){ "UnityEngine.QualitySettings", "IsPlatformIncluded", "System.String", "System.Int32"},
                // new List<string>(){ "UnityEngine.QualitySettings", "TryIncludePlatformAt", "System.String", "System.Int32", "System.Exception&" }, // ?
                // new List<string>(){ "UnityEngine.QualitySettings", "TryExcludePlatformAt", "System.String", "System.Int32", "System.Exception&" }, // ?
                // new List<string>(){ "UnityEngine.QualitySettings", "GetActiveQualityLevelsForPlatform", "System.String"},
                // new List<string>(){ "UnityEngine.QualitySettings", "GetActiveQualityLevelsForPlatformCount", "System.String"},
                // new List<string>(){ "UnityEngine.Rendering.VolumeComponent", "TryGetRevertMethodForFieldName", "UnityEditor.SerializedProperty", "System.Action`1[UnityEditor.SerializedProperty]&"},
                // new List<string>(){ "UnityEngine.Rendering.VolumeComponent", "GetSourceTerm"},
                // new List<string>(){ "UnityEngine.Rendering.VolumeComponent", "TryGetApplyMethodForFieldName", "UnityEditor.SerializedProperty", "System.Action`1[UnityEditor.SerializedProperty]&"},
                // new List<string>(){ "UnityEngine.Rendering.VolumeComponent", "GetSourceName", "UnityEngine.Component"},
};
}
