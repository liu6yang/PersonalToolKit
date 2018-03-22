using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using SimpleJSON;
using UnityEditor.iOS.Xcode;

namespace common
{
    /// <summary>
    /// Methods will be called with this attribute before build.
    /// Callback order is from 1 -> 49, 51 -> 99.
    /// Callback order 0,50,100 is used inside, do not use it.
    /// </summary>
    public class PreBuildAttribute : Attribute
    {
        public int callbackOrder { get; private set; }

        public PreBuildAttribute(int callbackOrder)
        {
            this.callbackOrder = callbackOrder;
        }
    }

    /// <summary>
    /// Methods will be called with this attribute after build.
    /// Callback order  1 -> 49 is used after eclipse project is created or xcode project is created.
    /// Callback order 51 -> 99 is used after apk is created or nothing on iOS.
    /// Callback order 0,50,100 is used inside, do not use it.
    /// </summary>
    public class PostBuildAttribute : Attribute
    {
        public int callbackOrder { get; private set; }

        public PostBuildAttribute(int callbackOrder)
        {
            this.callbackOrder = callbackOrder;
        }
    }

	internal class BuildChannel : EditorWindow
	{
		private static bool isDebug;
		public static string chBuildList;

		public static void Edit(bool isDebug)
		{
			BuildChannel.isDebug = isDebug;
			EditorWindow.GetWindow(typeof(BuildChannel));
		}

		void OnGUI()
		{
			string[] keys;
			string[][] values;
			string[] chBuildList_;
			Build.ReadChannelConfigs(out keys, out values, out chBuildList_);

			foreach (string[] value in values)
			{
				if (GUILayout.Button(value[0]))
				{
					Close();
					chBuildList = value[0];
					if (isDebug)
						Build.PerformAndroidBuild_Debug_Install();
					else
						Build.PerformAndroidBuild_Install();
				}
			}
		}
	}

    class Build
    {
        static string[] SCENES = FindEnabledEditorScenes();
        static string APP_NAME = Application.productName.Replace(' ', '_');
        static string TARGET_DIR = "target";

		const int CH_CONFIG_INDEX_BUILD_ID = 0;
		const int CH_CONFIG_INDEX_PACKAGE_NAME = 1;
		const int CH_CONFIG_INDEX_CHANNEL_SDKS = 2;
		const int CH_CONFIG_INDEX_ANDROID_TARGET_SDK = 3;
		const int CH_CONFIG_INDEX_APP_NAME = 3;

        static string PerformAndroidBuild_Debug()
        {
            string target = "_" + APP_NAME + ".apk";
            string dir = "./";
            target = dir + target;
            GenericBuild(SCENES, target, BuildTarget.Android, BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler);
            return target;
        }

        [MenuItem("CustomBuild/Build Android And Install (Debug)", priority = 1000)]
        internal static void PerformAndroidBuild_Debug_Install()
        {
            string target = PerformAndroidBuild_Debug();
            InstallApk(target);
        }

        [MenuItem("CustomBuild/Build Android And Install (Release)", priority = 1000)]
        internal static void PerformAndroidBuild_Install()
        {
            string target = "_" + APP_NAME + ".apk";
            string dir = "./";
            target = dir + target;
            GenericBuild(SCENES, target, BuildTarget.Android, BuildOptions.None);
            InstallApk(target);
        }

		[MenuItem("CustomBuild/Build Android And Install (Debug) - Choose channel", priority = 1000)]
        static void PerformAndroidBuild_Debug_Install_WithChannel()
		{
			BuildChannel.Edit(true);
		}

		[MenuItem("CustomBuild/Build Android And Install (Release) - Choose channel", priority = 1000)]
		static void PerformAndroidBuild_Install_WithChannel()
		{
			BuildChannel.Edit(false);
		}

        [MenuItem("CustomBuild/Build Android", priority = 1000)]
        static void PerformAndroidBuild()
        {
            string target = APP_NAME + ".apk";
            string dir = "./" + TARGET_DIR + "/Android/" + DateTime.Now.ToString("MM-dd_HH-mm-ss");
            GenericBuild(SCENES, dir + "/" + target, BuildTarget.Android, BuildOptions.None);
        }

        [MenuItem("CustomBuild/Build iOS", priority = 1000)]
        static void PerformIOSBuild()
        {
            string target = APP_NAME + ".xcode_ios";
            string dir = "./" + TARGET_DIR + "/iOS/" + DateTime.Now.ToString("MM-dd_HH-mm-ss");
            GenericBuild(SCENES, dir + "/" + target, BuildTarget.iOS, BuildOptions.None);
        }

        [MenuItem("CustomBuild/Build iOS (Debug)", priority = 1000)]
        static void PerformIOSBuild_Debug()
        {
            string target = APP_NAME + ".xcode_ios";
            string dir = "./" + TARGET_DIR + "/iOS/" + DateTime.Now.ToString("MM-dd_HH-mm-ss");
            GenericBuild(SCENES, dir + "/" + target, BuildTarget.iOS, BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler);
        }

        [MenuItem("CustomBuild/Build win32", priority = 1000)]
        static void PerformWin32Build()
        {
            string target = APP_NAME;
            string dir = "./" + TARGET_DIR + "/win32/" + DateTime.Now.ToString("MM-dd_HH-mm-ss");
            GenericBuild(SCENES, dir + "/" + target, BuildTarget.StandaloneWindows, BuildOptions.Development);
        }

		static void GenericBuild(string[] scenes, string target, BuildTarget build_target, BuildOptions build_options)
		{
		#if !SERVER_BUILD
			EditorUtils.EmptyEditorLog();
		#endif

		#if !SERVER_BUILD
			if (build_target != EditorUserBuildSettings.activeBuildTarget)
			{
				throw new Exception("You need switch platform to " + build_target + " by your own, in case wrong operation.");
			}
		#endif

		#if UNITY_ANDROID
			string androidSdkPath = Environment.GetEnvironmentVariable("ANDROID_SDK");
			if (!string.IsNullOrEmpty(androidSdkPath))
			{
				Debug.Log("Set android sdk path " + androidSdkPath);
				EditorPrefs.SetString("AndroidSdkRoot", androidSdkPath);
			}
		#endif

			if (System.Environment.GetEnvironmentVariable("BUILD_ARKIT_REMOTE") == "true")
			{
				Debug.Log("Build ArkitRemoteScene.");
				string arkitRemoteScene = "Assets/Plugins/SDKs/UnityARKitPlugin/UnityARKitPlugin/ARKitRemote/UnityARKitRemote.unity";
				scenes = new string[] { arkitRemoteScene };
				build_options |= BuildOptions.Development;
			}

			// Version number
			var gameVersion = CommandLineReader.GetCustomArgument("Game_Version").Trim(new [] {'\r', '\n'});
			if (string.IsNullOrEmpty(gameVersion))
			{
		#if !SERVER_BUILD
				gameVersion = "0.0.1";
		#else
				throw new Exception("missing Game_Version in command line args");
		#endif
            }
			PlayerSettings.bundleVersion = gameVersion;

			string target_ = CommandLineReader.GetCustomArgument("Build_Target");
		    if (!String.IsNullOrEmpty(target_))
			    target = target_;
            string dir = target.Substring(0, target.LastIndexOf("/"));
            Directory.CreateDirectory(dir);

            Debug.Log("GenericBuild pre build.");
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            MethodInfo[] methods = assemblies.SelectMany(a => a.GetTypes())
                .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .Where(m => m.GetCustomAttributes(typeof(PreBuildAttribute), false).Length > 0)
                .OrderBy(m => (m.GetCustomAttributes(typeof(PreBuildAttribute), false)[0] as PreBuildAttribute).callbackOrder)
                .ToArray();
            if (methods.Length == 0)
            {
                throw new Exception("No PreBuild method found?");
            }
            foreach (MethodInfo method in methods)
            {
                method.Invoke(null, new object[] { scenes, target, build_target, build_options });
            }

		#if !RELEASE_VERSION
            build_options |= BuildOptions.ForceEnableAssertions;
		#endif
            string prjTarget = target;
            if (build_target == BuildTarget.Android)
            {
                build_options |= BuildOptions.AcceptExternalModificationsToPlayer;
                prjTarget += "_prj";
		#if SDK_OBBDOWNLOADER || USE_OBB
				PlayerSettings.Android.useAPKExpansionFiles = true;
		#endif
            }
            if (Directory.Exists(prjTarget))
                Directory.Delete(prjTarget, true);

			Debug.Log("Start build " + build_target.ToString() + " with option " + build_options.ToString() + " to " + prjTarget);
			EditorUserBuildSettings.SwitchActiveBuildTarget(build_target);
            string res = BuildPipeline.BuildPlayer(scenes, prjTarget, build_target, build_options);
            if (res.Length > 0)
            {
                throw new Exception("BuildPlayer failure: " + res);
            }

            Debug.Log("GenericBuild post build.");
            assemblies = AppDomain.CurrentDomain.GetAssemblies();
            methods = assemblies.SelectMany(a => a.GetTypes())
                .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .Where(m => m.GetCustomAttributes(typeof(PostBuildAttribute), false).Length > 0)
                .OrderBy(m => (m.GetCustomAttributes(typeof(PostBuildAttribute), false)[0] as PostBuildAttribute).callbackOrder)
                .ToArray();
            if (methods.Length == 0)
            {
                throw new Exception("No PostBuild method found?");
            }
            foreach (MethodInfo method in methods)
            {
				Debug.Log("Invoke method " + method.Name);
				Debug.Log("build_target " + build_target);
				Debug.Log("prjTarget " + prjTarget);
				Debug.Log("target " + target);
				method.Invoke(null, new object[] { build_target, prjTarget, target });
            }
        }


	
		[PreBuild(0)]
		static void PreBuild0(string[] scenes, string target, BuildTarget build_target, BuildOptions build_options)
		{
			Debug.Log("PreBuild 0 is called.");
			string[] keys;
			string[][] lineValues;
			string[] chBuildList;
			ReadChannelConfigs(out keys, out lineValues, out chBuildList);

			if (build_target == BuildTarget.iOS)
			{
				if (WillBuildWithDifferentChannelSdks())
				{
					throw new Exception("Build with different channel sdks at one time is not supported on iOS build.");
				}
			}

			if (chBuildList.Length != 1)
			{
				Debug.Log("No channel is specified, no file need to be overridden.");
			}
			else
			{
				Debug.Log("Copy specific files for channel " + chBuildList[0]);
				string dir = (build_target == BuildTarget.iOS ? EditorEnv.channelsiOS : EditorEnv.channelsAndroid) + "/" + chBuildList[0] + "/_Unity";
				if (Directory.Exists(dir))
				{
					EditorUtils.DirectoryCopy(dir, EditorEnv.dstUnityProjectRoot, true, true);
					AssetDatabase.Refresh();
				}
			}
		}

		[PreBuild(50)]
        static void PreBuild(string[] scenes, string target, BuildTarget build_target, BuildOptions build_options)
        {
            Debug.Log("PreBuild 50 is called.");
            GenerateBuildEnv();

            if (File.Exists("./Assets/StreamingAssets/dlc_local"))
                File.Delete("./Assets/StreamingAssets/dlc_local");
			dlc.DlcConfigEditor.BuildDlcLocal(build_target);

			var gameVersion = CommandLineReader.GetCustomArgument("Game_Version").Trim(new [] {'\r', '\n'});


		#if UNITY_ANDROID
			SdkImporter.PrepareAndroidSdks(true);
            string[] dirs = Directory.GetDirectories(EditorEnv.dstUnityProjectPluginsAndroid);
            foreach (string dir in dirs)
            {
				string manifestFile = dir + "/AndroidManifest.xml";
                if (File.Exists(manifestFile))
                {
                    PreprocessFile(manifestFile, manifestFile);
                }
            }
			AssetDatabase.Refresh();
		#endif

		#if SERVER_BUILD
			Regex reg = new Regex("^\\d+\\.\\d\\.\\d$");
			if (!reg.Match(gameVersion).Success)
				throw new Exception("Game version format is not right.");
			string versionCode = gameVersion.Replace(".", "") 
			#if UNITY_ANDROID
				+ (int)PlayerSettings.Android.targetDevice
			#endif
				;
		#if UNITY_ANDROID
			PlayerSettings.Android.bundleVersionCode = int.Parse(versionCode);
			if ((int)PlayerSettings.Android.targetDevice < 0 || (int)PlayerSettings.Android.targetDevice > 9)
				throw new Exception("CPU_ABI int is out of expected.");
			Debug.Log("versionCode " + PlayerSettings.Android.bundleVersionCode);
		#elif UNITY_IOS
			PlayerSettings.iOS.buildNumber = versionCode;
			Debug.Log("versionCode " + PlayerSettings.iOS.buildNumber);
		#endif
		#endif
		}

		public static void PreprocessFile(string srcFile, string dstFile)
		{
			Debug.Log("Preprocess " + srcFile + " -> " + dstFile);
            string defines = "";
            foreach (string macro in SdkEnabler.instance.allEnabledSdkMacros)
            {
                defines += "#define " + macro + " 1\n";
            }

            defines += "\n";
            foreach (KeyValuePair<string, string> macro in SdkEnabler.instance.allVariableMacros)
            {
                defines += "#define " + macro.Key + " " + macro.Value + "\n";
            }

            defines += "\n";
            defines += "#define PACKAGE_NAME " + PlayerSettings.applicationIdentifier + "\n";
            defines += "#define STR_TO_STRING(NAME) #NAME\n";
            defines += "#define MACRO_TO_STRING(NAME) STR_TO_STRING(NAME)\n";
			defines += "#define STR_CONCAT(X, Y) X##Y\n";
			defines += "#define STR_CONCAT_TO_STRING(X, Y) STR_TO_STRING(X##Y)\n";
			defines += "#define MACRO_CONCAT_TO_STRING(X, Y) STR_CONCAT_TO_STRING(X, Y)\n";
            defines += "\n\n";

            Debug.Log("Defines: " + defines);
            string text = File.ReadAllText(srcFile);
            text = defines + text;
            File.WriteAllText(dstFile + ".tmp", text);

		#if UNITY_EDITOR_OSX
            EditorUtils.ExecuteCmd("clang -x c -E -P " + dstFile + ".tmp -o " + dstFile);
		#else
            EditorUtils.ExecuteCmd("cpp -E -P " + dstFile + ".tmp -o " + dstFile);
		#endif
            File.Delete(dstFile + ".tmp");
		}

        static void GenerateBuildEnv()
        {
            string str = "// Generated at build.\n\n";
            str += "class BuildEnv_Gen\n";
            str += "{\n";

            string whosBuild = System.Environment.UserName;
#if SERVER_BUILD
            whosBuild = "Jenkins";
#endif
            str += "\tpublic const string whosBuild = \"" + whosBuild + "\";\n";

            string buildNumber = "1";
#if SERVER_BUILD
            buildNumber = System.Environment.GetEnvironmentVariable("BUILD_NUMBER");
            if (string.IsNullOrEmpty(buildNumber))
                throw new Exception("Can't get BUILD_NUMBER from env variable.");
#endif
            str += "\tpublic const string buildNumber = \"" + buildNumber + "\";\n";

			str += "}\n";
            File.WriteAllText(EditorEnv.dstUnityProjectPluginsCommon + "/Utils/BuildEnv_Gen.cs", str);
            AssetDatabase.Refresh();
        }

        [PostBuild(50)]
        static void PostBuild(BuildTarget buildTarget, string prjTarget, string target_)
        {


			string gameVersionShellScriptContent = "export APP_NAME=" + APP_NAME + "\n";

			if (buildTarget == BuildTarget.Android)
            {
				string manifestPath = prjTarget + "/" + Application.productName + "/AndroidManifest.xml";
				string[] manifestLines = File.ReadAllLines(manifestPath);
				for (int i = 0; i < manifestLines.Length; ++i)
				{
					string line = manifestLines[i];
					if (line.Contains("android:name=\"com.yuehai.GameActivity\""))
					{
						manifestLines[i] = line.Replace("android:launchMode=\"singleTask\"", "android:launchMode=\"singleTop\"");
						File.WriteAllLines(manifestPath, manifestLines);
						Debug.Log("Changed GameActivity launchMode to singleTop in AndroidManifest.xml");
						break;
					}
				}

                BuildFromEclipsePrj(prjTarget, target_);
			}
            else if (buildTarget == BuildTarget.iOS)
            {
                Debug.Log("Modifying project.pbxproj");
                string prjPath = prjTarget + "/Unity-iPhone.xcodeproj/project.pbxproj";
                UnityEditor.iOS.Xcode.PBXProject prj = new UnityEditor.iOS.Xcode.PBXProject();
                prj.ReadFromString(File.ReadAllText(prjPath));
                string target = prj.TargetGuidByName("Unity-iPhone");
    
                // SDKs macro
                foreach (string macro in SdkEnabler.instance.allEnabledSdkMacros)
                {
                    Debug.Log("Add sdk macro " + macro + " to pbxproj.");
                    prj.AddBuildProperty(target, "OTHER_CFLAGS", "-D" + macro + "=1");
                }

				// SDKs configs
				foreach (KeyValuePair<string, string> macro in SdkEnabler.instance.allVariableMacros)
				{
					Debug.Log("Add sdk config macro " + macro + " to pbxproj.");
					prj.AddBuildProperty(target, "OTHER_CFLAGS", "-D" + macro.Key + "=" + macro.Value);
				}

				//prj.AddBuildProperty(target, "OTHER_CFLAGS", "-DPACKAGE_NAME=" + PlayerSettings.bundleIdentifier);
				prj.AddBuildProperty(target, "OTHER_CFLAGS", "-DSTR_TO_STRING(NAME)=#NAME");
				prj.AddBuildProperty(target, "OTHER_CFLAGS", "-DMACRO_TO_STRING(NAME)=STR_TO_STRING(NAME)");

				// comtavie_debug_ios.json
				File.Copy(EditorEnv.dstUnityProjectPluginsiOS + "/comtavie_ios.json", prjTarget + "/Libraries/comtavie_ios.json");
				string guid = prj.AddFile("Libraries/comtavie_ios.json", "Libraries/comtavie_ios.json");
				prj.AddFileToBuild(target, guid);

				// channel_minor_splash.jpg
				string minorSplashPath = EditorEnv.dstUnityProjectPluginsiOS + "/channel_minor_splash.jpg";
				if (File.Exists(minorSplashPath))
				{
					File.Copy(minorSplashPath, prjTarget + "/Libraries/channel_minor_splash.jpg");
					guid = prj.AddFile("Libraries/channel_minor_splash.jpg", "Libraries/channel_minor_splash.jpg");
					prj.AddFileToBuild(target, guid);
				}

                // Keychain sharing entitlements file
                //File.Copy(EditorEnv.dstUnityProjectPluginsCommon + "/iOS/common.entitlements", prjTarget + "/Unity-iPhone/common.entitlements");
                //prj.AddFile("Unity-iPhone/common.entitlements", "common.entitlements");
                //prj.SetBuildProperty(target, "CODE_SIGN_ENTITLEMENTS", "Unity-iPhone/common.entitlements");

				// Enable modules
				prj.SetBuildProperty(target, "CLANG_ENABLE_MODULES", "YES");
                
				if (WillBuildChannelSdk("GameDreamer"))
				{
					prj.SetBuildProperty(target, "ENABLE_BITCODE", "NO");
				}
                
                ScriptingImplementation backend = (ScriptingImplementation) PlayerSettings.GetPropertyInt("ScriptingBackend", BuildTargetGroup.iOS);
                if (backend == ScriptingImplementation.IL2CPP)
                {
                    int arch = PlayerSettings.GetPropertyInt("Architecture", BuildTargetGroup.iOS);
                    if (arch == 0)
                    {
                        prj.SetBuildProperty(target, "VALID_ARCHS", "armv7");
                    }
                    else if (arch == 1)
                    {
                        prj.SetBuildProperty(target, "VALID_ARCHS", "arm64");
                    }
                }

                prj.AddFrameworkToProject(target, "AdSupport.framework", true);
				prj.AddBuildProperty(target, "OTHER_LDFLAGS", "-lz");
				prj.AddBuildProperty(target, "OTHER_LDFLAGS", "-ObjC");
                string str = prj.WriteToString();

                // Keychain sharing on, Push notification on
                //Regex reg = new Regex("(\\w+) /\\* Unity-iPhone \\*/ = {");
                /*Match match = reg.Match(str);
                if (!match.Success) throw new Exception("Can't find guid of Unity-iPhone in xcode prj.");
                guid = match.Groups[1].Value;
                string attributes = "TargetAttributes = {\n";
                attributes += "\t\t\t\t\t" + guid + " = {\n";
                attributes += "\t\t\t\t\t\tDevelopmentTeam = 45889RSW42;\n";
                attributes += "\t\t\t\t\t\tSystemCapabilities = {\n";
                attributes += "\t\t\t\t\t\t\tcom.apple.Keychain = {\n";
                attributes += "\t\t\t\t\t\t\t\tenabled = 1;\n";
                attributes += "\t\t\t\t\t\t\t};\n";
				attributes += "\t\t\t\t\t\t\tcom.apple.Push = {\n";
                attributes += "\t\t\t\t\t\t\t\tenabled = 1;\n";
                attributes += "\t\t\t\t\t\t\t};\n";
                attributes += "\t\t\t\t\t\t};\n";
                attributes += "\t\t\t\t\t};\n";
                str = str.Replace("TargetAttributes = {\n", attributes);*/

#if UNITY_EDITOR_WIN
                str = str.Replace("\\\\", "/");
#endif
                File.WriteAllText(prjPath, str);
                Debug.Log("Modify project.pbxproj end.");

				// Add "UIFileSharingEnabled" to Info.plist
				PlistDocument plist = new PlistDocument();
				string plistPath = prjTarget + "/Info.plist";
				plist.ReadFromFile(plistPath);
				plist.root.SetString("CFBundleIdentifier", "PACKAGE_NAME");
				plist.root.SetString("ChannelSdk", "CHANNEL_SDKS");
				plist.root.SetBoolean("UIFileSharingEnabled", 
				#if RELEASE_VERSION
					false
				#else
					true
				#endif
					);
				plist.root.SetString("NSCameraUsageDescription", "Unity requires access to the camera");
				plist.root.SetString("NSPhotoLibraryUsageDescription", "Unity requires access to the photo library");
				plist.root.SetString("NSLocationWhenInUseUsageDescription", "Unity requires access to the location");
				plist.root.SetString("NSCalendarsUsageDescription", "Unity requires access to the calendar library");
				plist.WriteToFile(plistPath);
				Debug.Log("Modify Info.plist end.");
				File.Copy(plistPath, plistPath + ".original");

			#if UNITY_5
				// Fix keyboard
				File.Copy(EditorEnv.sharedLibrariesLibs + "/iOS_fix/Keyboard.mm", prjTarget + "/Classes/UI/Keyboard.mm", true);
			#endif

				// Start build xcode prj for multi channels
				File.Copy(prjTarget + "/Libraries/comtavie_ios.json", prjTarget + "/Libraries/comtavie_ios.json.original");
			#if SERVER_BUILD
				string shareRootDir = CommandLineReader.GetCustomArgument("ShareRootDir");
				if (!Directory.Exists(shareRootDir))
					throw new Exception("ShareRootDir doesn't exist, " + shareRootDir);
			#else
				string shareRootDir = null;
			#endif
				Debug.Log("ShareRootDir " + shareRootDir);
				BuildMultiXCodePrj(prjTarget, shareRootDir);
            }


			File.WriteAllText(
				EditorEnv.dstUnityProjectRoot +	"/_GameVersion.sh",
				gameVersionShellScriptContent);
		}

		static void AddLocalizationToXcodePrj(string pathToBuiltProject, string chBuildId)
		{
			Debug.Log("AddLocalizationToXcodePrj");
			string langDir = EditorEnv.channelsiOS + "/" + chBuildId + "/Localization";
			if (!Directory.Exists(langDir))
			{
				Debug.LogWarning(langDir + " doesn't exist.");
				return;
			}
			EditorUtils.DirectoryCopy(langDir, pathToBuiltProject, false, true);

			string[] dirs = Directory.GetDirectories(langDir);
			int langCount = dirs.Length;
			string[] uuids = new string[langCount + 2];

			string prjPath = pathToBuiltProject + "/Unity-iPhone.xcodeproj/project.pbxproj";
			string str = File.ReadAllText(prjPath);
			Regex reg = new Regex("[^\\w](\\w{24})[^\\w]");
			MatchCollection matches = reg.Matches(str);
			for (int i = 0; i < uuids.Length; ++i)
			{
				uuids[i] = XCodeUtils.NewGuid();
				for (int j = 0; j < i; ++j)
				{
					if (uuids[i] == uuids[j])
						throw new Exception("Impossible! We generated same uuid " + uuids[i]);
				}
				foreach (Match match in matches)
				{
					if (match.Groups[1].Value == uuids[i])
						throw new Exception("Impossible! Our generated uuid " + match.Value + " is already in pbxproj.");
				}
			}

			string buildUUID = uuids[langCount];
			string refUUID = uuids[langCount + 1];
			string buildList = "/* Begin PBXBuildFile section */\n";
			buildList += "\t\t" + buildUUID + " /* InfoPlist.strings in Resources */ = {isa = PBXBuildFile; fileRef = " + refUUID + " /* InfoPlist.strings */; };\n";
			str = str.Replace("/* Begin PBXBuildFile section */\n", buildList);

			string refList = "/* Begin PBXFileReference section */\n";
			for (int i = 0; i < langCount; ++i)
			{
				string dir = dirs[i];
				if (!dir.EndsWith(".lproj"))
					throw new Exception("Dir " + dir + " is not a localization dir.");
				string lang = dir.Replace(langDir + "/", "").Replace(".lproj", "");
				refList += "\t\t" + uuids[i] + " /* " + lang + " */ = {isa = PBXFileReference; lastKnownFileType = text.plist.strings; name = " + lang + "; path = " + lang + ".lproj/InfoPlist.strings; sourceTree = \"<group>\"; };\n";
			}
			str = str.Replace("/* Begin PBXFileReference section */\n", refList);

			string customTemplateStart = "/* CustomTemplate */ = {\n\t\t\tisa = PBXGroup;\n\t\t\tchildren = (\n";
			string customTemplate = customTemplateStart;
			customTemplate += "\t\t\t\t" + refUUID + " /* InfoPlist.strings */,\n";
			str = str.Replace(customTemplateStart, customTemplate);

			string knownRegionsStart = "knownRegions = (\n\t\t\t\tEnglish,\n\t\t\t\tJapanese,\n\t\t\t\tFrench,\n\t\t\t\tGerman,\n\t\t\t\ten,\n";
			string knownRegion = knownRegionsStart;
			foreach (string dir in dirs)
			{
				string lang = dir.Replace(langDir + "/", "").Replace(".lproj", "");
				if (lang == "en")
					continue;
				knownRegion += "\t\t\t\t" + lang + ",\n";
			}
			str = str.Replace(knownRegionsStart, knownRegion);

			string resListStart = "/* Data in Resources */,\n";
			string resList = resListStart;
			resList += "\t\t\t\t" + buildUUID + " /* InfoPlist.strings in Resources */,\n";
			str = str.Replace(resListStart, resList);

			string variantEnd = "/* End PBXVariantGroup section */\n";
			string variant = "\t\t" + refUUID + " /* InfoPlist.strings */ = {\n";
			variant += "\t\t\tisa = PBXVariantGroup;\n";
			variant += "\t\t\tchildren = (\n";
			for (int i = 0; i < langCount; ++i)
			{
				string dir = dirs[i];
				string lang = dir.Replace(langDir + "/", "").Replace(".lproj", "");
				variant += "\t\t\t\t" + uuids[i] + " /* " + lang + " */,\n";
			}
			variant += "\t\t\t);\n";
			variant += "\t\t\tname = InfoPlist.strings;\n";
			variant += "\t\t\tsourceTree = \"<group>\";\n";
			variant += "\t\t};\n";
			variant += variantEnd;
			str = str.Replace(variantEnd, variant);

			File.WriteAllText(prjPath, str);
		}

		static void BuildMultiXCodePrj(string prjTarget, string shareRootDir)
		{
			string[] keys;
			string[][] lineValues;
			string[] chBuildList;
			ReadChannelConfigs(out keys, out lineValues, out chBuildList);

			foreach (string[] values in lineValues)
			{
				string chBuildId = values[CH_CONFIG_INDEX_BUILD_ID];
				string packageName = values[CH_CONFIG_INDEX_PACKAGE_NAME];
				string[] channelSdks = values[CH_CONFIG_INDEX_CHANNEL_SDKS].Split(',');
				if (!chBuildList.Contains(chBuildId))
					continue;

				Debug.Log("Build XCode prj: " + chBuildId);
				string envs = "Set channel env: \n";
				for (int i = 1; i < values.Length; ++i)
				{
					envs += keys[i] + " -> " + values[i] + "\n";
				}
				Debug.Log(envs);

				ReplaceXCodePrjKeyValues(prjTarget, keys, values);

				string prjPath = prjTarget + "/Unity-iPhone.xcodeproj/project.pbxproj";
                UnityEditor.iOS.Xcode.PBXProject prj = new UnityEditor.iOS.Xcode.PBXProject();
                prj.ReadFromString(File.ReadAllText(prjPath));
                string target = prj.TargetGuidByName("Unity-iPhone");

				// Package name in macro
				string prjContent = prj.WriteToString();
				/*Regex reg = new Regex("\"(\\-DPACKAGE_NAME=[^\"]+)\"");
				Match match = reg.Match(prjContent);
				if (!match.Success)
					throw new Exception("Can't find macro PACKAGE_NAME in xcode prj.");
				prj.UpdateBuildProperty(target, "OTHER_CFLAGS", new[]{ "-DPACKAGE_NAME=" + packageName }, new[]{ match.Groups[1].Value });*/

				// ChannelAppController
				int hasChannelAppController = 0;
				foreach (string sdk in channelSdks)
				{
					if (File.Exists(EditorEnv.sharedLibrariesSdks + "/" + sdk + "/iOS/ChannelAppController.mm")
						|| File.Exists(EditorEnv.dstUnityProjectPluginsiOS + "/ChannelAppController.mm"))
					{
						hasChannelAppController = 1;
						break;
					}
				}
				prj.UpdateBuildProperty(target, "OTHER_CFLAGS", new[] { "-DSDK_HAS_CHANNEL_APP_CONTROLLER=" + hasChannelAppController }, null);

				// Sign
				foreach (string configName in new string[] { "Debug", "Release" })
				{
					// Also use release sign for debug, resign for debug later.
					string certName;
					InstallP12ForChannel(chBuildId, "Release", out certName);
					InstallCertificateForChannel(chBuildId, configName);
					
					string channelProvisioning = EditorEnv.channelsiOS + "/" + chBuildId + "/mobileprovision/Release.mobileprovision";
					if (!File.Exists(channelProvisioning))
						throw new Exception("Can't find channel release provisioning " + channelProvisioning);
					string config = prj.BuildConfigByName(target, configName);
					string provisioningId, provisioningName, teamId;
					InstallProvisioningProfile(channelProvisioning, packageName, out provisioningId, out provisioningName, out teamId);
					prj.SetBuildPropertyForConfig(config, "PROVISIONING_PROFILE", provisioningId);
					prj.SetBuildPropertyForConfig(config, "CODE_SIGN_IDENTITY", certName);
					prj.SetBuildPropertyForConfig(config, "CODE_SIGN_IDENTITY[sdk=iphoneos*]", certName);
				}

				File.WriteAllText(prjPath, prj.WriteToString());

				// Channel xcode prj files
				string channelXCodePrjDir = EditorEnv.channelsiOS + "/" + chBuildId + "/xcode_prj";
				if (Directory.Exists(channelXCodePrjDir))
				{
					Debug.Log("Copy channel xcode prj files, " + channelXCodePrjDir + " -> " + prjTarget);
					EditorUtils.DirectoryCopy(channelXCodePrjDir, prjTarget, false, true);
				}

				// Localization
				AddLocalizationToXcodePrj(prjTarget, chBuildId);

			#if SERVER_BUILD
				if (Environment.GetEnvironmentVariable("EXPORT_XCODE_PRJ_ONLY") == "true")
				{
					Debug.Log("EXPORT_XCODE_PRJ_ONLY is true, exit.");
					return;
				}
			#endif
			#if UNITY_EDITOR_WIN
				throw new Exception("Abort on xcodebuild, we are on Windows.");
			#endif

				BuildXCodePrj(prjTarget, "Unity-iPhone.xcodeproj", shareRootDir, chBuildId, packageName);
			}
		}

		static void BuildXCodePrjs()
		{
			string number_ = Environment.GetEnvironmentVariable("BUILD_NUMBER_XCODE_PRJS");
			int number;
			if (!int.TryParse(number_, out number))
				throw new Exception("Invalid BUILD_NUMBER_XCODE_PRJS " + number_);
			Debug.Log("BUILD_NUMBER_XCODE_PRJS " + number);

			string shareRootDir = CommandLineReader.GetCustomArgument("ShareRootDir");
			string shareDir = CommandLineReader.GetCustomArgument("ShareDir");
			if (!Directory.Exists(shareRootDir) || !Directory.Exists(shareDir))
				throw new Exception("ShareRootDir doesn't exist, " + shareRootDir);
			Debug.Log("ShareRootDir " + shareRootDir);

			string[] dirs = Directory.GetDirectories(shareRootDir);
			string dir = dirs.First(d => Path.GetFileName(d).StartsWith("[" + number + "]"));
			if (dir == null)
				throw new Exception("Can't find dir with build number " + number);
			Debug.Log("Use dir " + dir);

			dirs = Directory.GetDirectories(dir);
			dir = dirs.First(d => d.EndsWith(".xcode_ios"));
			if (dir == null)
				throw new Exception("Can't find .xcode_ios with build number " + number);
			Debug.Log("Use dir " + dir);
			string prjTarget = dir;

			string[] keys;
			string[][] lineValues;
			string[] chBuildList;
			ReadChannelConfigs(out keys, out lineValues, out chBuildList);

			string[] prjs = Directory.GetDirectories(prjTarget, "*.xcodeproj", SearchOption.TopDirectoryOnly);
			Debug.Log("BuildXCodePrjs with " + prjs.Length + " prjs.");
			foreach (string prj in prjs)
			{
				string name = Path.GetFileName(prj);
				PlistDocument plist = new PlistDocument();
				string plistPath = prjTarget + "/Info.plist";
				plist.ReadFromFile(plistPath);
				string packageName = plist.root["CFBundleIdentifier"].AsString();

				Regex reg = new Regex(@"^Unity-iPhone(.*)\.xcodeproj$");
				Match match = reg.Match(name);
				if (match.Success)
				{
					string minorChannel = match.Groups[1].Value;
					if (minorChannel.Length > 0)
					{
						string channelMinorXCodeDir = EditorEnv.channelsiOS + "/" + chBuildList[0] + "/minor/" + minorChannel + "/xcode_prj";
						if (Directory.Exists(channelMinorXCodeDir))
						{
							Debug.Log("Copy minor xcode prj " + channelMinorXCodeDir);
							EditorUtils.DirectoryCopy(channelMinorXCodeDir, prjTarget, withMeta:false, withoutClean:true);
						}
					}
				}

				BuildXCodePrj(prjTarget, name, shareDir, chBuildList[0], packageName);
			}
		}

		static void BuildXCodePrj(string prjTarget, string name, string shareRootDir, string chBuildId, string packageName)
		{
			Debug.Log("Xcode building, " + name);
			EditorUtils.ExecuteCmd("xcodebuild clean -project " + name + " -configuration Release", prjTarget, ignoreOutputError:true);
			EditorUtils.ExecuteCmd("security unlock-keychain -p 123 /Users/Shared/Jenkins/Library/Keychains/login.keychain && " 
				+ "xcodebuild -project " + name + " -configuration Release", prjTarget, ignoreOutputError:true);

			Debug.Log("Packing ipa...");
			string prjPath = prjTarget + "/" + name + "/project.pbxproj";
			string prjContent = File.ReadAllText(prjPath);
			Regex reg = new Regex("PRODUCT_NAME = ([^;]+);");
			Match match = reg.Match(prjContent);
			if (!match.Success)
			{
				reg = new Regex(@"<key>PRODUCT_NAME</key>[\s\t\r\n]*<string>([^<]+)</string>");
				match = reg.Match(prjContent);
			}
			if (!match.Success)
			{
				throw new Exception("Can't get PRODUCT_NAME from xcode prj.");
			}
			string productNameForPath = match.Groups[1].Value;

			reg = new Regex(@"^Unity-iPhone(.*)\.xcodeproj$");
			match = reg.Match(name);
			string minorChannel = "";
			if (match.Success)
				minorChannel = match.Groups[1].Value;
			else
				throw new Exception("Unexpected xcode prj name " + name);

			string appPath = prjTarget + "/build/Release-iphoneos/" + productNameForPath + ".app";
			string ipaPath = shareRootDir + "/" 
				+ productNameForPath + "_" + chBuildId + "_v" + PlayerSettings.bundleVersion + "_[" 
			#if SERVER_BUILD
				+ System.Environment.GetEnvironmentVariable("BUILD_NUMBER")
			#endif
				+ "]" 
				+ (minorChannel.Length > 0 ? ("_" + minorChannel) : "")
			#if RELEASE_VERSION
				+ "_Release"
			#endif
				+ ".ipa";
			EditorUtils.ExecuteCmd("xcrun -sdk iphoneos PackageApplication -v \"" + appPath + "\" -o \"" + ipaPath + "\"", prjTarget);

			Debug.Log("Resigning ipa...");
			string debugCertName;
			InstallP12ForChannel(chBuildId, "Debug", out debugCertName);
			string debugProvisioning = EditorEnv.channelsiOS + "/" + chBuildId + "/mobileprovision/Debug.mobileprovision";
			if (!File.Exists(debugProvisioning))
				throw new Exception("Can't find debug provisioning " + debugProvisioning);
			string debugProvisioningId, debugProvisioningName, debugTeamId;
			InstallProvisioningProfile(debugProvisioning, null, out debugProvisioningId, out debugProvisioningName, out debugTeamId);
			string resignedIpaPath = ipaPath.Substring(0, ipaPath.LastIndexOf('.')) + "_Resigned.ipa";
			EditorUtils.ExecuteCmd("security unlock-keychain -p 123 /Users/Shared/Jenkins/Library/Keychains/login.keychain && " 
				+ "AppResign -v -c \"" + debugCertName + "\" -p \"" + debugProvisioningName + " (" + debugTeamId + ")\" -n \"*\" \"" 
				+ ipaPath + "\" \"" + resignedIpaPath + "\"", shareRootDir);

			Debug.Log("Move dsym");
			File.Move(appPath + ".dSYM", ipaPath + ".dSYM");
		}

		static void BuildFromEclipsePrj(string prjTarget, string target)
        {
#if USE_CUSTOM_MONO
            string[] monoVersions = Directory.GetDirectories(EditorEnv.sharedLibrariesRoot + "/Libs/Mono");
            for (int i = 0; i < monoVersions.Length; ++i)
            {
                monoVersions[i] = Path.GetFileName(monoVersions[i]);
            }
            if (!monoVersions.Contains(Application.unityVersion))
            {
                throw new Exception("No " + Application.unityVersion + " custom mono can be found.");
            }

            Debug.Log("Copy custom mono " + Application.unityVersion);
            switch (PlayerSettings.Android.targetDevice)
            {
            case AndroidTargetDevice.ARMv7:
                File.Copy(EditorEnv.sharedLibrariesRoot + "/Libs/Mono/" + Application.unityVersion + "/armeabi-v7a/libmono.so", prjTarget + "/" + Application.productName + "/libs/armeabi-v7a/libmono.so", true);
                break;
            case AndroidTargetDevice.x86:
                File.Copy(EditorEnv.sharedLibrariesRoot + "/Libs/Mono/" + Application.unityVersion + "/x86/libmono.so", prjTarget + "/" + Application.productName + "/libs/x86/libmono.so", true);
                break;
            case AndroidTargetDevice.FAT:
                File.Copy(EditorEnv.sharedLibrariesRoot + "/Libs/Mono/" + Application.unityVersion + "/armeabi-v7a/libmono.so", prjTarget + "/" + Application.productName + "/libs/armeabi-v7a/libmono.so", true);
                File.Copy(EditorEnv.sharedLibrariesRoot + "/Libs/Mono/" + Application.unityVersion + "/x86/libmono.so", prjTarget + "/" + Application.productName + "/libs/x86/libmono.so", true);
                break;
            default:
                throw new Exception("New Android CPU ABI? " + PlayerSettings.Android.targetDevice);
            }

            Debug.Log("Encrypt Assembly-CSharp.dll");
            if (Application.platform == RuntimePlatform.OSXEditor)
                EditorUtils.ExecuteCmd("chmod 777 " + EditorEnv.sharedLibrariesRoot + "/Libs/Mono-enc/enc_dll");
            string dll = Path.GetFullPath(prjTarget + "/" + Application.productName + "/assets/bin/Data/Managed/Assembly-CSharp.dll");
            File.Copy(dll, prjTarget + "/../Assembly-CSharp.dll.bak");
            EditorUtils.ExecuteCmd(EditorEnv.sharedLibrariesRoot + "/Libs/Mono-enc/enc_dll" + " \"" + dll + "\"");
#endif

            string sdk = EditorPrefs.GetString("AndroidSdkRoot");
            string android = sdk + "/tools/android";
            if (string.IsNullOrEmpty(sdk) || !Directory.Exists(sdk))
            {
                throw new Exception("Can't find Android sdk.");
            }

            Debug.Log("Update Android prj.");
            string[] androidPrjs = Directory.GetDirectories(prjTarget);
            foreach (string prj in androidPrjs)
            {
                if (!Directory.Exists(prj + "/src"))
                {
                    Directory.CreateDirectory(prj + "/src");
                }
                if (Path.GetFileName(prj) != Application.productName)
                {
					if (!Directory.Exists(prj + "/libs"))
                        Directory.CreateDirectory(prj + "/libs");
                    File.Copy(prjTarget + "/" + Application.productName + "/libs/unity-classes.jar", prj + "/libs/unity-classes.jar");

                    if (Directory.Exists(prj + "/assets"))
                    {
                        string[] assetsDirs = Directory.GetDirectories(prj + "/assets");
                        foreach (string dir in assetsDirs)
                        {
                            Directory.Move(dir, prjTarget + "/" + Application.productName + "/assets/" + Path.GetFileName(dir));
                        }
                        string[] files = Directory.GetFiles(prj + "/assets");
                        foreach (string file in files)
                        {
                            File.Move(file, prjTarget + "/" + Application.productName + "/assets/" + Path.GetFileName(file));
                        }
                    }
                    EditorUtils.ExecuteCmd(android + " update lib-project --path \"" + prj + "\"");
                }
                else
                {
                    EditorUtils.ExecuteCmd(android + " update project --path \"" + prj + "\" --name Game");
                }

				string manifestPath = prj + "/AndroidManifest.xml";
				File.Copy(manifestPath, manifestPath + ".original");
            }

			Debug.Log("Merge manifests."); // Unity 5.3.5p4, (746248) - Android: Buildpipe - Don't merge manifests if exporting project.
            foreach (string prj in androidPrjs.Where(p => Path.GetFileName(p) != Application.productName))
			{
				MergeManifest(prjTarget + "/" + Application.productName + "/AndroidManifest.xml.original", prj);
			}

			Debug.Log("Add build_fixed.xml into build.xml");
			string lines = File.ReadAllText(prjTarget + "/" + Application.productName + "/build.xml");
			string fixedLines = File.ReadAllText(EditorEnv.sharedLibrariesLibs + "/build_fixed/build_fixed.xml");
			int pos = lines.LastIndexOf("</project>");
			Debug.AssertThrowException(pos > 0, "Can't find \"</project>\" in build.xml");
			lines = lines.Insert(pos, fixedLines);
			File.WriteAllText(prjTarget + "/" + Application.productName + "/build.xml", lines);

			Debug.Log("Add fixed_dex.py");
			File.Copy(EditorEnv.sharedLibrariesLibs + "/build_fixed/fixed_dex.py", prjTarget + "/" + Application.productName + "/fixed_dex.py");

			if (Environment.GetEnvironmentVariable("MINIMAL_MAIN_DEX") == "true")
			{
				Debug.Log("Add min_main_dex_list_starts_with.txt");
				File.Copy(EditorEnv.sharedLibrariesLibs + "/build_fixed/main_dex_list_starts_with.txt", prjTarget + "/" + Application.productName + "/min_main_dex_list_starts_with.txt");
			}
			else
			{
				Debug.Log("Add main_dex_list_starts_with.txt");
				File.Copy(EditorEnv.sharedLibrariesLibs + "/build_fixed/main_dex_list_starts_with.txt", prjTarget + "/" + Application.productName + "/main_dex_list_starts_with.txt");
			}

			// ActivityLifeCallbacks
			List<string> packageNames = new List<string>();
			foreach (string prj in androidPrjs)
			{
				string path = prj + "/src/ActivityLifeCallbacks.java";
				if (Path.GetFileName(prj) != Application.productName
					&& Path.GetFileName(prj) != "AndroidCommon"
					&& File.Exists(path))
				{
					Match match = Utils.FindRegexInFile(path, @"package ([^;]+);", true);
					packageNames.Add(match.Groups[1].Value);
				}
			}
			Debug.Log("ActivityLifeCallbacks: " + ("\n\t".Join(packageNames)));
			string pathGameActivity = prjTarget + "/AndroidCommon/src/GameActivity.java";
			string addLifeCallbacks = "\n".Join(packageNames.Select(name => "addLifeCallbacks((LifeCallbacks) Class.forName(\"" + name + ".ActivityLifeCallbacks\").newInstance());"));
			Utils.ReplaceFileText(pathGameActivity, "__ADD_LIFECALLBACKS__", addLifeCallbacks, true);
			
			BuildMultiApkFromEclipsePrj(prjTarget, target);
        }

		static void BuildOneApkFromEclipsePrj(string prjTarget, string target, string chBuildId, string packageName)
		{
			Directory.GetFiles(prjTarget, "*", SearchOption.AllDirectories).Where(f => f.EndsWith(".bat")).ToList().ForEach(f => {
				Debug.Log("Remove " + f);
				File.Delete(f);
			});

			Debug.Log("Ant build.");
		#if UNITY_EDITOR_WIN
            EditorUtils.ExecuteCmd("subst /D X: & subst X: " + prjTarget);
            EditorUtils.ExecuteCmd("ant release -Djava.source=7 -Djava.target=7", "X:/" + Application.productName);
            EditorUtils.ExecuteCmd("subst /D X:");
		#else
            EditorUtils.ExecuteCmd("ant release -Djava.source=7 -Djava.target=7", prjTarget + "/" + Application.productName);
		#endif

			string keystore = EditorEnv.sharedLibrariesRoot + "/Libs/keystore/android.keystore";
			string storepass = "123456";
			string alias = "android.keystore";
			string channelKeystoreDir = EditorEnv.channelsAndroid + "/" + chBuildId + "/_keystore";
			Regex reg = new Regex(@"(.+)\.keystore");
			Match match;
			string channelKeystore = EditorUtils.FindFileByRegex(channelKeystoreDir, reg, out match);
			if (channelKeystore != null)
			{
				keystore = channelKeystore;
				storepass = match.Groups[1].Value;
				alias = null;
				List<string> outputList = EditorUtils.ExecuteCmd("keytool -list -keystore \"" + keystore + "\" -storepass " + storepass);
				foreach (string output in outputList)
				{
					reg = new Regex(@"^([^,]+), \d+\-\d+\-\d+, PrivateKeyEntry,");
					match = reg.Match(output);
					if (match.Success)
					{
						alias = match.Groups[1].Value;
						break;
					}
				}
				if (alias == null)
				{
					throw new Exception("Can't get alias form keystore " + keystore);
				}
			}
            Debug.Log("Sign with keystore " + keystore);
            EditorUtils.ExecuteCmd("jarsigner -verbose -digestalg SHA1 -sigalg MD5withRSA -keystore \"" 
				+ keystore
				+ "\" -storepass " + storepass + " -signedjar Game-release-unaligned.apk Game-release-unsigned.apk " + alias, 
                prjTarget + "/" + Application.productName + "/bin");

            Debug.Log("Zip align.");
			string sdk = EditorPrefs.GetString("AndroidSdkRoot");
            string[] dirs = Directory.GetDirectories(sdk + "/build-tools");
            string zipalign = dirs[dirs.Length - 1] + "/zipalign";
            EditorUtils.ExecuteCmd(zipalign + " -f -v 4 Game-release-unaligned.apk Game-release.apk", prjTarget + "/" + Application.productName + "/bin");

            File.Copy(prjTarget + "/" + Application.productName + "/bin/Game-release.apk", 
			#if SERVER_BUILD
				target.Substring(0, target.Length - 4) + "_" + chBuildId + ".apk"
			#else // set apk name without CH_BUILD_ID in local build, for installation later.
				target
			#endif
				, true);
            Debug.Log("Output apk Game-release.apk");

		#if SDK_OBBDOWNLOADER || USE_OBB
			var obbFileName = Application.productName + ".main.obb";
			var correctObbName = "main."+ PlayerSettings.Android.bundleVersionCode.ToString() + "." + packageName + ".obb";
			File.Copy(prjTarget + "/" + obbFileName, prjTarget + "/../" + correctObbName);
			//gameVersionShellScriptContent += 
			//	"export OBB_NAME=" + correctObbName + "\n";
			Debug.Log("Output obb " + correctObbName);
		#endif
		}

		internal static void ReadChannelConfigs(out string[] keys, out string[][] lineValues, out string[] chBuildList)
		{
			Excel.ReadAllValues(EditorEnv.rootDir + "/ChannelConfigs" + 
			#if UNITY_IOS
				"_iOS" +
			#endif
				".xlsx", out keys, out lineValues);
			Debug.AssertThrowException(keys[CH_CONFIG_INDEX_BUILD_ID] == "CH_BUILD_ID", "Expected first key is CH_BUILD_ID, but it's " + keys[CH_CONFIG_INDEX_BUILD_ID]);
			Debug.AssertThrowException(keys[CH_CONFIG_INDEX_PACKAGE_NAME] == "PACKAGE_NAME", "Expected second key is PACKAGE_NAME, but it's " + keys[CH_CONFIG_INDEX_PACKAGE_NAME]);
			keys.ToList().ForEach(k => Debug.AssertThrowException(Regex.Match(k, "[A-Za-z\\d_]+").Success, "Invalid key " + k));
			lineValues.ToList().ForEach(values => Debug.AssertThrowException(Regex.Match(values[CH_CONFIG_INDEX_BUILD_ID], "[A-Za-z\\d_]+").Success, "Invalid CH_BUILD_ID " + values[CH_CONFIG_INDEX_BUILD_ID]));

			string chBuildList_ = 
			#if SERVER_BUILD
				Environment.GetEnvironmentVariable("CH_BUILD_LIST");
			#else
				BuildChannel.chBuildList;
			#endif
			if (string.IsNullOrEmpty(chBuildList_))
			{
				Debug.Log("No CH_BUILD_LIST set, will build an apk from first row of ChannelConfigs.");
				chBuildList_ = lineValues[0][CH_CONFIG_INDEX_BUILD_ID];
			}
			chBuildList = chBuildList_.Split(',');
		}

		internal static bool WillBuildChannelSdk(string sdk)
		{
			string[] keys;
			string[][] lineValues;
			string[] chBuildList;
			ReadChannelConfigs(out keys, out lineValues, out chBuildList);

			foreach (string[] values in lineValues)
			{
				if (chBuildList.Contains(values[CH_CONFIG_INDEX_BUILD_ID]))
				{
					if (values[CH_CONFIG_INDEX_CHANNEL_SDKS].Split(',').Contains(sdk))
					{
						return true;
					}
				}
			}
			return false;
		}

		internal static bool WillBuildWithDifferentChannelSdks()
		{
			string[] keys;
			string[][] lineValues;
			string[] chBuildList;
			ReadChannelConfigs(out keys, out lineValues, out chBuildList);

			string sdks = null;
			foreach (string[] values in lineValues)
			{
				if (chBuildList.Contains(values[CH_CONFIG_INDEX_BUILD_ID]))
				{
					if (sdks != null)
					{
						if (sdks != values[CH_CONFIG_INDEX_CHANNEL_SDKS])
							return true;
					}
					else
					{
						sdks = values[CH_CONFIG_INDEX_CHANNEL_SDKS];
					}
				}
			}
			return false;
		}

		static void MergeManifest(string mainPrj, string libPrj)
		{
			string android = EditorPrefs.GetString("AndroidSdkRoot") + "/tools/android";
			string manifestMerger = "java -cp " + EditorPrefs.GetString("AndroidSdkRoot") + "/tools/lib/manifest-merger.jar com.android.manifmerger.Merger";

			string mainManifest = File.Exists(mainPrj) ? mainPrj : mainPrj + "/AndroidManifest.xml";
			string libManifest = File.Exists(libPrj) ? libPrj : libPrj + "/AndroidManifest.xml";
			Debug.Log("MergeManifest " + mainManifest + ", " + libManifest);
			
			string mainManifestTmp = mainManifest + ".tmp";
			if (File.Exists(mainManifestTmp))
				File.Delete(mainManifestTmp);
			File.Move(mainManifest, mainManifestTmp);
			string cmd = manifestMerger + " --main \"" + mainManifestTmp + "\"";
			cmd += " --libs \"" + libManifest + "\"";
			cmd += " --out \"" + mainManifest + "\" --log INFO";
			EditorUtils.ExecuteCmd(cmd);
			File.Delete(mainManifestTmp);
		}

		static void ReplaceManifestKeyValues(string prj, string[] keys, string[] values, bool replacePackageName)
		{
			Debug.Log("ReplaceManifestKeyValues " + prj);
			string manifestPath = prj + "/AndroidManifest.xml";
			string manifestContent = File.ReadAllText(manifestPath + ".original");
			for (int i = 1; i < values.Length; ++i)
			{
				string key = keys[i];
				string value = values[i];
				if (i != CH_CONFIG_INDEX_PACKAGE_NAME) // Replace key without quote only for PACKAGE_NAME
				{
					key = "\"" + key + "\"";
					value = "\"" + value + "\"";
				}

				manifestContent = manifestContent.Replace(key, value);
				if (replacePackageName && i == CH_CONFIG_INDEX_PACKAGE_NAME) // PACKAGE_NAME
				{
					manifestContent = manifestContent.Replace("package=\"" + PlayerSettings.applicationIdentifier + "\"", "package=\"" + values[i] + "\"");
				}

				string androidTargetSdk = values[CH_CONFIG_INDEX_ANDROID_TARGET_SDK];
				if (!string.IsNullOrEmpty(androidTargetSdk) && i == CH_CONFIG_INDEX_ANDROID_TARGET_SDK) // ANDROID_TARGET_SDK
				{
					Regex reg = new Regex("android:targetSdkVersion=\"\\d+\"");
					manifestContent = reg.Replace(manifestContent, "android:targetSdkVersion=\"" + androidTargetSdk + "\"");
				}
			}
			File.WriteAllText(manifestPath, manifestContent);
		}

		static void ReplaceXCodePrjKeyValues(string prj, string[] keys, string[] values)
		{
			Debug.Log("ReplaceXCodePrjKeyValues " + prj);
			string configPath = prj + "/Libraries/comtavie_ios.json";
			string configContent = File.ReadAllText(configPath+ ".original");
			for (int i = 1; i < values.Length; ++i)
			{
				string key = "\"" + keys[i] + "\"";
				string value = "\"" + values[i] + "\"";
				configContent = configContent.Replace(key, value);
			}
			File.WriteAllText(configPath, configContent);

			string plistPath = prj + "/Info.plist";
			string plistContent = File.ReadAllText(plistPath+ ".original");
			for (int i = 1; i < values.Length; ++i)
			{
				string key = ">" + keys[i] + "<";
				string value = ">" + values[i] + "<";
				plistContent = plistContent.Replace(key, value);
			}
			File.WriteAllText(plistPath, plistContent);

			if (values[CH_CONFIG_INDEX_APP_NAME] != "")
			{
				PlistDocument plist = new PlistDocument();
				plist.ReadFromFile(plistPath);
				plist.root.SetString("CFBundleDisplayName", values[CH_CONFIG_INDEX_APP_NAME]);
				plist.WriteToFile(plistPath);
			}
		}

		static void BuildMultiApkFromEclipsePrj(string prjTarget, string target)
		{
			Debug.Log("Build multi apk.");
			string[] keys;
			string[][] lineValues;
			string[] chBuildList;
			ReadChannelConfigs(out keys, out lineValues, out chBuildList);

			string android = EditorPrefs.GetString("AndroidSdkRoot") + "/tools/android";
			string mainPrj = prjTarget + "/" + Application.productName;
			string channelSdkPrj = prjTarget + "/ChannelSdk";

			foreach (string[] values in lineValues)
			{
				string chBuildId = values[CH_CONFIG_INDEX_BUILD_ID];
				if (!chBuildList.Contains(chBuildId))
					continue;

				Debug.Log("Build apk: " + chBuildId);
				string envs = "Set channel env: \n";
				for (int i = 1; i < values.Length; ++i)
				{
					envs += keys[i] + " -> " + values[i] + "\n";
				}
				Debug.Log(envs);

				// Remove previous sdks
				string[] dirs = Directory.GetDirectories(prjTarget);
				foreach (string dir in dirs)
				{
					string sdk = Path.GetFileName(dir);
					if (sdk.StartsWith("SdkCH-"))
					{
						string assetsDir = dir + "/assets";
						if (Directory.Exists(assetsDir))
						{
							Debug.Log("Remove assets of previous sdk " + sdk + " from main prj.");
							Directory.GetFiles(assetsDir, "*", SearchOption.AllDirectories).ToList().ForEach(f => 
							{
								string dst = mainPrj + "/assets" + f.Substring(assetsDir.Length);
								if (File.Exists(dst))
									File.Delete(dst);
							});
						}

						Debug.Log("Remove previous sdk " + sdk);
						Directory.Delete(dir, true);
					}
				}

				// Remove previous sdk references
				string[] lines = File.ReadAllLines(channelSdkPrj + "/project.properties");
				List<string> newLines = new List<string>();
				foreach (string line in lines)
				{
					Regex reg = new Regex("android\\.library\\.reference\\.(\\d+)=\\.\\./(.+)");
					Match match = reg.Match(line);
					if (match.Success && match.Groups[2].Value.StartsWith("SdkCH-"))
					{
						Debug.Log("Remove ref " + match.Groups[2].Value + " from manifest.");
						continue;
					}
					else
					{
						newLines.Add(line); // sdk reference should only be appended to the end of file, so we don't need to care ref id.
					}
				}
				File.WriteAllLines(channelSdkPrj + "/project.properties", newLines.ToArray());

				// Delete ChannelActivity.java and ChannelApplication.java in ChannelSdk
				if (File.Exists(channelSdkPrj + "/src/ChannelActivity.java"))
					File.Delete(channelSdkPrj + "/src/ChannelActivity.java");
				if (File.Exists(channelSdkPrj + "/src/ChannelApplication.java"))
					File.Delete(channelSdkPrj + "/src/ChannelApplication.java");
				// Delete bin, or old ChannelActivity.class and ChannelApplication.class will be packed
				if (Directory.Exists(channelSdkPrj + "/bin"))
					Directory.Delete(channelSdkPrj + "/bin", true);

				// Restore backed up files (channel specific files for eclipse project)
				Debug.Log("Restore backed up eclipse files.");
				dirs = Directory.GetDirectories(prjTarget);
				foreach (string dir in dirs)
				{
					string dstDir = dir;
					string bakDir = dstDir + "/_bak";
					if (!Directory.Exists(bakDir))
					{
						continue;
					}

					string[] files = Directory.GetFiles(bakDir, "*", SearchOption.AllDirectories);
					foreach (string file in files)
					{
						string dstFile = dstDir + file.Substring(bakDir.Length);
						string bakFile = file;
						Debug.Log(bakFile + " -> " + dstFile);
						File.Copy(bakFile, dstFile, true);
						EditorUtils.ExecuteCmd("touch \"" + dstFile + "\"");
					}
				}


				// Add sdks
				string[] sdks = values[CH_CONFIG_INDEX_CHANNEL_SDKS] != "" ? values[CH_CONFIG_INDEX_CHANNEL_SDKS].Split(',') : new string[0];
			#if !SDK_CHANNELSDK
				Debug.AssertThrowException(sdks.Length == 0, "Channel sdk is not enabled, but CHANNEL_SDKS is configured in " + values[CH_CONFIG_INDEX_BUILD_ID]);
			#endif
				dirs = Directory.GetDirectories(prjTarget).Select(d => Path.GetFileName(d)).ToArray();
				foreach (string sdk in sdks)
				{
					if (dirs.Contains(sdk))
					{
						Debug.Log("Sdk " + sdk + " is already in eclipse prj.");
					}
					else
					{
						Debug.Log("Sdk " + sdk + " copy.");
						string sdkDir = EditorEnv.sharedLibrariesSdksAndroid + "/" + sdk;
						Debug.AssertThrowException(Directory.Exists(sdkDir), "Sdk " + sdk + " doesn't exist, " + sdkDir);
						//Debug.AssertThrowException(!Directory.Exists(EditorEnv.sharedLibrariesSdks + "/" + sdk), 
						//	"Sdk " + sdk + " can't be added to eclipse prj, " + EditorEnv.sharedLibrariesSdks + "/" + sdk);

						string libPrj = prjTarget + "/SdkCH-" + sdk;
						EditorUtils.DirectoryCopy(sdkDir, libPrj, false);
						if (!File.Exists(libPrj + "/src/ChannelActivity.java"))
							File.Copy(EditorEnv.sharedLibrariesSdks + "/ChannelSdk/Editor/ChannelActivity.java", libPrj + "/src/ChannelActivity.java");
						if (!File.Exists(libPrj + "/src/ChannelApplication.java"))
							File.Copy(EditorEnv.sharedLibrariesSdks + "/ChannelSdk/Editor/ChannelApplication.java", libPrj + "/src/ChannelApplication.java");

						EditorUtils.ExecuteCmd(android + " update lib-project --path \"" + libPrj + "\"");
						if (SdkDeps.instance.depDict.ContainsKey(sdk))
						{
							var dep = SdkDeps.instance.depDict[sdk];
							dep.deps.ForEach(d => 
							{
								EditorUtils.ExecuteCmd(android + " update project --path \"" + libPrj + "\" --library ../" + d);
							});
						}

						EditorUtils.ExecuteCmd(android + " update project --path \"" + channelSdkPrj + "\" --library ../SdkCH-" + sdk);
						File.Move(libPrj + "/AndroidManifest.xml", libPrj + "/AndroidManifest.xml.original");

						if (Directory.Exists(libPrj + "/assets"))
						{
							Debug.Log("Copy assets of " + sdk + " to main prj.");
							EditorUtils.DirectoryCopy(libPrj + "/assets", mainPrj + "/assets", false, true);
						}
					}
				}

				// Channel specific files for eclipse project, such as icon.
				if (Directory.Exists(EditorEnv.channelsAndroid + "/" + chBuildId))
				{
					Debug.Log("Replace specific eclipse files from " + EditorEnv.channelsAndroid + "/" + chBuildId);
					dirs = Directory.GetDirectories(EditorEnv.channelsAndroid + "/" + chBuildId);
					foreach (string dir in dirs)
					{
						if (Path.GetFileName(dir) != "_main" && Path.GetFileName(dir).StartsWith("_"))
						{
							continue;
						}

						string srcDir = dir;
						string dstDir = prjTarget + "/" + (Path.GetFileName(dir) == "_main" ? Application.productName : Path.GetFileName(dir));
						string bakDir = dstDir + "/_bak";

						string[] files = Directory.GetFiles(srcDir, "*", SearchOption.AllDirectories);
						foreach (string file in files)
						{
							string srcFile = file;
							string dstFile = dstDir + srcFile.Substring(srcDir.Length);
							string bakFile = bakDir + srcFile.Substring(srcDir.Length);

							if (!File.Exists(bakFile) && File.Exists(dstFile))
							{
								Directory.CreateDirectory(Path.GetDirectoryName(bakFile));
								File.Copy(dstFile, bakFile);
							}

							Debug.Log(srcFile + " -> " + dstFile);
							Directory.CreateDirectory(Path.GetDirectoryName(dstFile));
							File.Copy(srcFile, dstFile, true);
							EditorUtils.ExecuteCmd("touch \"" + dstFile + "\"");
						}
					}
				}
				else
				{
					Debug.Log("No specific eclipse files.");
				}

				// If CHANNEL_SDKS is empty.
				if (sdks.Length == 0)
				{
					File.Copy(EditorEnv.sharedLibrariesSdks + "/ChannelSdk/Editor/ChannelActivity.java", channelSdkPrj + "/src/ChannelActivity.java");
					File.Copy(EditorEnv.sharedLibrariesSdks + "/ChannelSdk/Editor/ChannelApplication.java", channelSdkPrj + "/src/ChannelApplication.java");
				}

				// Generate ChannelSdkGen.java
				if (Directory.Exists(prjTarget + "/ChannelSdk"))
				{
					string gen = "package net.comtavie.channelsdk;\n";
					gen += "import net.comtavie.channelsdkinterface.AbstractSdk;\n";
					gen += "public abstract class ChannelSdkGen extends ChannelSdkBase {\n";
					gen += "    @Override\n";
					gen += "    protected void registerSdks() {\n";
					gen += "        try {\n";
					foreach (string sdk in sdks)
					{
					gen += "            registerSdk((Class<? extends AbstractSdk>) Class.forName(\"net.comtavie.channel" + sdk.ToLowerInvariant() + "." + sdk + "\"));\n";
					}
					gen += "        } catch (Exception e) {\n";
					gen += "            throw new RuntimeException(\"Can't register sdk, \" + e.getMessage());\n";
					gen += "        }\n";
					gen += "    }\n";
					gen += "}";
					File.WriteAllText(prjTarget + "/ChannelSdk/src/ChannelSdkGen.java", gen);
				}

				// Generate SdkProxyGen.java
				if (Directory.Exists(prjTarget + "/ChannelSdk"))
				{
					string tmpDir = EditorEnv.sharedLibrariesSdks + "/ChannelSdk/Editor/tmp";
					if (Directory.Exists(tmpDir))
						Directory.Delete(tmpDir, true);
					Directory.CreateDirectory(tmpDir);

					EditorUtils.DirectoryCopy(EditorEnv.sharedLibrariesSdks + "/ChannelSdk/Editor/deps", EditorEnv.sharedLibrariesSdks + "/ChannelSdk/Editor/tmp", false)
						.ForEach(f => EditorUtils.ExecuteCmd("javac -d . " + Path.GetFileName(f), tmpDir));
					File.Copy(EditorEnv.sharedLibrariesSdks + "/ChannelSdk/Editor/SdkProxyGenerator.java", tmpDir + "/SdkProxyGenerator.java");
					File.Copy(EditorEnv.sharedLibrariesSdksAndroid + "/channelsdkinterface/src/SdkInterface.java", tmpDir + "/SdkInterface.java");
					File.Copy(EditorEnv.sharedLibrariesSdksAndroid + "/channelsdkinterface/src/InvokeOnUiThread.java", tmpDir + "/InvokeOnUiThread.java");
					EditorUtils.ExecuteCmd("javac -d . InvokeOnUiThread.java", tmpDir);
					EditorUtils.ExecuteCmd("javac -d . SdkInterface.java", tmpDir);
					EditorUtils.ExecuteCmd("javac SdkProxyGenerator.java", tmpDir);
					EditorUtils.ExecuteCmd("java -cp . SdkProxyGenerator", tmpDir);
					File.Copy(tmpDir + "/SdkProxyGen.java", prjTarget + "/ChannelSdk/src/SdkProxyGen.java", overwrite: true);
					File.Delete(tmpDir + "/SdkProxyGen.java");
				}

				string[] prjs = Directory.GetDirectories(prjTarget);
				foreach (string prj in prjs)
				{
					ReplaceManifestKeyValues(prj, keys, values, Path.GetFileName(prj) == Application.productName);
				}

				// Merge manifest
				dirs = Directory.GetDirectories(prjTarget).Select(d => Path.GetFileName(d)).Where(d => d.StartsWith("SdkCH-")).ToArray();
				if (dirs.Length > 0)
				{
					foreach (string dir in dirs)
					{
						MergeManifest(mainPrj, prjTarget + "/" + dir);
					}
				}

				BuildOneApkFromEclipsePrj(prjTarget, target, chBuildId, values[1]);
			}
			Debug.Log("Build multi apk end.");

			var existList = lineValues.Select(values => values[CH_CONFIG_INDEX_BUILD_ID]);
			chBuildList.ToList().ForEach(ch => Debug.AssertThrowException(existList.Contains(ch), "Channel " + ch + " is not in ChannelConfigs.xlsx"));
		}

		static void BuildApkFromOtherEclipsePrj()
		{
			Debug.Log("BuildApkFromOtherEclipsePrj");
			string number_ = Environment.GetEnvironmentVariable("ECLIPSE_PRJ_NUMBER");
			int number;
			if (!int.TryParse(number_, out number))
				throw new Exception("Invalid ECLIPSE_PRJ_NUMBER " + number_);
			Debug.Log("ECLIPSE_PRJ_NUMBER " + number);

			string shareRootDir = CommandLineReader.GetCustomArgument("ShareRootDir");
			if (!Directory.Exists(shareRootDir))
				throw new Exception("ShareRootDir doesn't exist, " + shareRootDir);
			Debug.Log("ShareRootDir " + shareRootDir);

			string[] dirs = Directory.GetDirectories(shareRootDir);
			string dir = dirs.First(d => Path.GetFileName(d).StartsWith("[" + number + "]"));
			if (dir == null)
				throw new Exception("Can't find dir with build number " + number);
			Debug.Log("Use dir " + dir);

			dirs = Directory.GetDirectories(dir);
			dir = dirs.First(d => d.EndsWith(".apk_prj"));
			if (dir == null)
				throw new Exception("Can't find .apk_prj with build number " + number);
			Debug.Log("Use dir " + dir);

			string target_ = CommandLineReader.GetCustomArgument("Build_Target");
			if (string.IsNullOrEmpty(target_))
				throw new Exception("Build_Target is empty.");
			BuildMultiApkFromEclipsePrj(dir, target_);
		}

		static void BuildChBuildList()
		{
			Debug.Log("BuildChBuildList");
			string path = CommandLineReader.GetCustomArgument("CH_BUILD_LIST_PATH");
			if (string.IsNullOrEmpty(path))
				throw new Exception("No CH_BUILD_LIST_PATH set.");
			Debug.Log("CH_BUILD_LIST_PATH: " + path);

			string[] keys;
			string[][] lineValues;
			string[] chBuildList;
			ReadChannelConfigs(out keys, out lineValues, out chBuildList);

			string result = "CH_BUILD_LIST=";
			for (int i = 0; i < lineValues.Length; ++i)
			{
				if (i != 0)
					result += ",";
				result += lineValues[i][CH_CONFIG_INDEX_BUILD_ID];
			}
			File.WriteAllText(path, result);
			Debug.Log("BuildChBuildList end, " + result);
		}

        public static void InstallApk(string target)
        {
            string sdk = EditorPrefs.GetString("AndroidSdkRoot");
            string adb = sdk + "/platform-tools/adb.exe";
            string adbParam = "install -r " + target;
            System.Diagnostics.Process.Start("cmd.exe", "/C echo adb " + adbParam + " & " + adb + " " + adbParam + " & pause");
        }

        static string[] FindEnabledEditorScenes()
        {
            List<string> EditorScenes = new List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled) continue;
                EditorScenes.Add(scene.path);
            }
            return EditorScenes.ToArray();
        }

		static void InstallP12ForChannel(string chBuildId, string config, out string certName)
		{
			string dir = EditorEnv.channelsiOS + "/" + chBuildId + "/mobileprovision";
			string regStr = "^" + config + @"_(.*)\.p12$";
			string file = EditorUtils.FindFileByRegex(dir, regStr);
			if (file == null)
				throw new Exception("Can't find p12 file by reg " + regStr + " in dir " + dir);

			string passPhrase = new Regex(regStr).Match(Path.GetFileName(file)).Groups[1].Value;
			InstallP12(file, passPhrase, config, out certName);
		}

		static void InstallP12(string p12Path, string passPhrase, string config, out string certName)
		{
			if (!File.Exists(p12Path))
				throw new Exception("P12 file doesn't exist, " + p12Path);

			List<string> outputList = EditorUtils.ExecuteCmd("openssl pkcs12 -in \"" + p12Path + "\" -nodes -passin pass:\"" + passPhrase + "\"", ignoreOutputError:true);
			string output = '\n'.Join(outputList);
			Regex reg = new Regex(@"friendlyName: (iPhone \b(" + (config != "Debug" ? "Distribution" : "Developer") + @")\b: [^\(]+ \([A-Z0-9]+\))");
			Match match = reg.Match(output);
			if (!match.Success)
			{
				// Try dev if dis doesn't exist, some debug build use dev p12 for release build.
				reg = new Regex(@"friendlyName: (iPhone \b(" + "Developer" + @")\b: [^\(]+ \([A-Z0-9]+\))");
				match = reg.Match(output);
			}
			if (!match.Success)
			{
				throw new Exception("Can't get friendlyName from p12 file, " + p12Path);
			}
			 
			certName = match.Groups[1].Value;
			Debug.Log("certName " + certName);
		#if !UNITY_EDITOR_WIN
			EditorUtils.ExecuteCmd("security unlock-keychain -p 123 /Users/Shared/Jenkins/Library/Keychains/login.keychain && "
				+ "security import \"" + p12Path + "\" -k /Users/Shared/Jenkins/Library/Keychains/login.keychain -P \"" + passPhrase + "\" -T /usr/bin/codesign && "
				+ "security set-key-partition-list -S apple-tool:,apple: -s -k 123 /Users/Shared/Jenkins/Library/Keychains/login.keychain");
		#endif
		}

		static void InstallCertificateForChannel(string chBuildId, string config)
		{
			string dir = EditorEnv.channelsiOS + "/" + chBuildId + "/mobileprovision";
			string file = dir + "/" + config + ".cer";
			if (File.Exists(file))
			{
				EditorUtils.ExecuteCmd("security unlock-keychain -p 123 /Users/Shared/Jenkins/Library/Keychains/login.keychain && "
				+ "security add-certificates -k /Users/Shared/Jenkins/Library/Keychains/login.keychain \"" + file + "\"", ignoreError:true, ignoreOutputError:true); // TODO: check failed
			}
			else
			{
				Debug.LogWarning(".cer file doesn't exist, ignore.");
			}
		}

		// packageName can be empty with resign
        static void InstallProvisioningProfile(string provisioningPath, string packageName, out string provisioningId, out string provisioningName, out string teamId)
        {
		#if !UNITY_EDITOR_WIN
            string dir = Environment.GetEnvironmentVariable("PROVISIONING_DIR");
            if (string.IsNullOrEmpty(dir))
                throw new Exception("Please set PROVISIONING_DIR in environment.");
		#endif
            string filepath = provisioningPath;
            if (!File.Exists(filepath))
                throw new Exception("File doesn't exist, " + filepath);

            string text = File.ReadAllText(filepath);
            Regex reg = new Regex("<key>application-identifier</key>\n\t\t<string>[^\\.]+\\.([^<]+)</string>", RegexOptions.Singleline);
            Match match = reg.Match(text);
            if (!match.Success)
                throw new Exception("Can't get bundle id from provisioning file, " + filepath);
			string bundleIdInProvisioning = match.Groups[1].Value;
			if (!string.IsNullOrEmpty(packageName) && bundleIdInProvisioning != packageName && !packageName.StartsWith(bundleIdInProvisioning.TrimEnd('*')))
				throw new Exception("bundleIdInProvisioning is not match bundleIdInPlayerSettings, " + bundleIdInProvisioning + ", " + packageName);

			reg = new Regex("<key>UUID</key>\n\t<string>([^<]+)</string>", RegexOptions.Singleline); 
            match = reg.Match(text);
            if (!match.Success)
                throw new Exception("Can't find UUID from provisioning file, " + filepath);
            provisioningId = match.Groups[1].Value;
			Debug.Log("provisioningId " + provisioningId);

			reg = new Regex("<key>Name</key>\n\t<string>([^<]+)</string>", RegexOptions.Singleline);
			match = reg.Match(text);
            if (!match.Success)
                throw new Exception("Can't find name from provisioning file, " + filepath);
			provisioningName = match.Groups[1].Value;
			Debug.Log("provisioningName " + provisioningName);

			reg = new Regex("<key>com.apple.developer.team-identifier</key>\n\t\t<string>([^<]+)</string>", RegexOptions.Singleline);
			match = reg.Match(text);
            if (!match.Success)
                throw new Exception("Can't find name from provisioning file, " + filepath);
			teamId = match.Groups[1].Value;
			Debug.Log("teamId " + teamId);

		#if !UNITY_EDITOR_WIN
			string homeDir = Environment.GetEnvironmentVariable("HOME");
            File.Copy(filepath, homeDir + "/Library/MobileDevice/Provisioning Profiles/" + provisioningId + ".mobileprovision", true);
		#endif
        }
    }
}