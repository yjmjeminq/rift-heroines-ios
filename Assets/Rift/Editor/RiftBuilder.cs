#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RiftHeroines.EditorTools
{
    public static class RiftBuilder
    {
        const string Scene="Assets/Rift/Scenes/Moonbridge.unity";
        [Serializable] sealed class BuildResultData {public string result,unity,version;public int errors,warnings;public ulong bytes;public double seconds;}
        [MenuItem("Tools/Rift Heroines/Validate and build Windows")]
        public static void PrepareAndBuild()
        {
            Directory.CreateDirectory("BuildLogs");Configure();ImportArt();CreateScene();
            if(!RiftValidation.Run())throw new BuildFailedException("Combat validation failed; see BuildLogs/model-validation.json");
            RiftPlaytest.Capture();
            RiftExperienceAudit.Capture();
            RiftWorkshopPlaytest.Capture();
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{Scene},locationPathName="Builds/Windows/RiftHeroines.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText("BuildLogs/build-result.json",JsonUtility.ToJson(new BuildResultData {result=report.summary.result.ToString(),unity=Application.unityVersion,version=RiftData.Version,errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,bytes=report.summary.totalSize,seconds=report.summary.totalTime.TotalSeconds},true));
            if(report.summary.result!=BuildResult.Succeeded)throw new BuildFailedException("Rift build failed");
        }
        [MenuItem("Tools/Rift Heroines/Validate combat")]
        public static void ValidateOnly(){if(!RiftValidation.Run())throw new Exception("Rift combat validation failed");}
        [MenuItem("Tools/Rift Heroines/Export iPhone Xcode project")]
        public static void ExportIOS()
        {
            Directory.CreateDirectory("BuildLogs");Configure();ImportArt();CreateScene();
            if(!RiftValidation.Run())throw new BuildFailedException("Combat validation failed");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS,"com.moonbridge.riftheroines");
            PlayerSettings.iOS.targetOSVersionString="15.0";PlayerSettings.iOS.sdkVersion=iOSSdkVersion.DeviceSDK;
            PlayerSettings.iOS.targetDevice=iOSTargetDevice.iPhoneAndiPad;PlayerSettings.iOS.buildNumber="1";
            PlayerSettings.iOS.appleEnableAutomaticSigning=false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS,ScriptingImplementation.IL2CPP);
            PlayerSettings.SetArchitecture(NamedBuildTarget.iOS,1);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.iOS,ManagedStrippingLevel.Low);
            PlayerSettings.stripEngineCode=true;EditorUserBuildSettings.development=false;
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{Scene},locationPathName="Builds/iOS-Xcode",target=BuildTarget.iOS,options=BuildOptions.CompressWithLz4HC});
            File.WriteAllText("BuildLogs/build-ios.json",JsonUtility.ToJson(new BuildResultData {result=report.summary.result.ToString(),unity=Application.unityVersion,version=RiftData.Version,errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,bytes=report.summary.totalSize,seconds=report.summary.totalTime.TotalSeconds},true));
            if(report.summary.result!=BuildResult.Succeeded||report.summary.totalErrors!=0)throw new BuildFailedException("iOS export failed");
        }
        static void Configure()
        {
            PlayerSettings.companyName="Moonbridge Studio";PlayerSettings.productName="Rift Heroines";PlayerSettings.bundleVersion=RiftData.Version;
            PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;PlayerSettings.colorSpace=ColorSpace.Gamma;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone,"com.moonbridge.riftheroines");
            var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input=settings.FindProperty("activeInputHandler");if(input!=null){input.intValue=0;settings.ApplyModifiedPropertiesWithoutUndo();}
            QualitySettings.vSyncCount=0;QualitySettings.antiAliasing=0;PlayerSettings.defaultInterfaceOrientation=UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft=true;PlayerSettings.allowedAutorotateToLandscapeRight=true;
            PlayerSettings.allowedAutorotateToPortrait=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
            EditorUserBuildSettings.development=true;
        }
        static void ImportArt()
        {
            foreach(string path in Directory.GetFiles("Assets/Rift/Resources/Rift","*.png",SearchOption.AllDirectories))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
                importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;importer.alphaSource=TextureImporterAlphaSource.FromInput;
                importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;
                importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
            }
        }
        static void CreateScene()
        {
            Directory.CreateDirectory("Assets/Rift/Scenes");var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("Rift Heroines").AddComponent<RiftGame>();EditorSceneManager.SaveScene(scene,Scene);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scene,true)};AssetDatabase.SaveAssets();
        }
    }
}
#endif
