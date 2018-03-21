using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
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

    class Build
    {
        static string[] SCENES = FindEnabledEditorScenes();
        static string APP_NAME = Application.productName.Replace(' ', '_');
        static string TARGET_DIR = "target";

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
            GenericBuild(SCENES, dir + "/" + target, BuildTarget.iOS, BuildOptions.Development /*| BuildOptions.AllowDebugging*/ | BuildOptions.ConnectWithProfiler);
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
			if (build_target != EditorUserBuildSettings.activeBuildTarget)
			{
				throw new Exception("You need switch platform to " + build_target + " by your own, in case wrong operation.");
			}
		#endif

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
		#if SDK_OBBDOWNLOADER
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
                method.Invoke(null, new object[] { build_target, prjTarget, target });
            }

        }


	

		[PreBuild(50)]
        static void PreBuild(string[] scenes, string target, BuildTarget build_target, BuildOptions build_options)
        {

		}

		public static void PreprocessFile(string srcFile, string dstFile)
		{

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
#if SDK_OBBDOWNLOADER
				var obbFileName = Application.productName + ".main.obb";
				var correctObbName = "main."+ PlayerSettings.Android.bundleVersionCode.ToString() +"."+Application.bundleIdentifier+".obb";
				File.Copy(prjTarget + "/" + obbFileName, prjTarget + "/../" + correctObbName);
				gameVersionShellScriptContent += 
					"export OBB_NAME=" + correctObbName + "\n";
#endif
			}
            else if (buildTarget == BuildTarget.iOS)
            {
               
            }


			File.WriteAllText(
				EditorEnv.dstUnityProjectRoot +	"/_GameVersion.sh",
				gameVersionShellScriptContent);
		}

		static void AddLocalizationToXcodePrj(string pathToBuiltProject)
		{
			Debug.Log("AddLocalizationToXcodePrj");
			string langDir = EditorEnv.rootDir + "/Channels/iOS/_default/Localization";
			if (!Directory.Exists(langDir))
			{
				Debug.LogWarning(langDir + " doesn't exist.");
				return;
			}

			string[] localizationFiles = Directory.GetDirectories(langDir, "*", SearchOption.AllDirectories);
			foreach (string file in localizationFiles)
			{
				Directory.CreateDirectory(pathToBuiltProject + file.Replace(langDir, ""));
			}
			localizationFiles = Directory.GetFiles(langDir, "*", SearchOption.AllDirectories);
			foreach (string file in localizationFiles)
			{
				if (file.EndsWith(".meta"))
					continue;
				File.Copy(file, pathToBuiltProject + file.Replace(langDir, ""));
			}

			string[] dirs = Directory.GetDirectories(langDir);
			int langCount = dirs.Length;
			string[] uuids = new string[langCount + 2];

			string prjPath = pathToBuiltProject + "/Unity-iPhone.xcodeproj/project.pbxproj";
			string str = File.ReadAllText(prjPath);
			Regex reg = new Regex("[^\\w](\\w{24})[^\\w]");
			MatchCollection matches = reg.Matches(str);
			for (int i = 0; i < uuids.Length; ++i)
			{
				uuids[i] = string.Join("", Guid.NewGuid().ToString().Split('-').Skip(1).ToArray()).ToUpper();
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

            BuildOneApkFromEclipsePrj(prjTarget, target, "");
        }

		static void BuildOneApkFromEclipsePrj(string prjTarget, string target, string chBuildId)
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

            Debug.Log("Sign.");
            //EditorUtils.ExecuteCmd("jarsigner -verbose -digestalg SHA1 -sigalg MD5withRSA -keystore \""
            //    + EditorEnv.sharedLibrariesRoot + "/Libs/keystore/android.keystore\""
            //    + " -storepass 123456 -signedjar Game-release-unaligned.apk Game-release-unsigned.apk android.keystore",
            //    prjTarget + "/" + Application.productName + "/bin");

            Debug.Log("Zip align.");
			string sdk = EditorPrefs.GetString("AndroidSdkRoot");
            string[] dirs = Directory.GetDirectories(sdk + "/build-tools");
            string zipalign = dirs[dirs.Length - 1] + "/zipalign";
            EditorUtils.ExecuteCmd(zipalign + " -f -v 4 Game-release-unsigned.apk Game-release.apk", prjTarget + "/" + Application.productName + "/bin");

            File.Copy(prjTarget + "/" + Application.productName + "/bin/Game-release.apk", 
			#if SERVER_BUILD
				target.Substring(0, target.Length - 4) + "_" + chBuildId + ".apk"
			#else // set apk name without CH_BUILD_ID in local build, for installation later.
				target
			#endif
				, true);
            Debug.Log("Output apk Game-release.apk");
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
				if (i != 1) // Replace key without quote only for PACKAGE_NAME
				{
					key = "\"" + key + "\"";
					value = "\"" + value + "\"";
				}
				manifestContent = manifestContent.Replace(key, value);
				if (replacePackageName && i == 1) // PACKAGE_NAME
				{
					manifestContent = manifestContent.Replace("package=\"" + PlayerSettings.applicationIdentifier + "\"", "package=\"" + values[i] + "\"");
				}
			}
			File.WriteAllText(manifestPath, manifestContent);
		}

		static void BuildMultiApkFromEclipsePrj(string prjTarget, string target)
		{
			
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

        static string GetProvisioningProfileId(string filename)
        {
            string dir = Environment.GetEnvironmentVariable("PROVISIONING_DIR");
            if (string.IsNullOrEmpty(dir))
                throw new Exception("Please set PROVISIONING_DIR in environment.");
            string filepath = dir + "/" + filename;
            if (!File.Exists(filepath))
                throw new Exception("File doesn't exist, " + filepath);
            string text = File.ReadAllText(filepath);
            Regex reg = new Regex(string.Format("<key>application-identifier</key>\n\t\t<string>[^\\.]+.{0}</string>", PlayerSettings.applicationIdentifier), RegexOptions.Singleline);
            Match match = reg.Match(text);
            if (!match.Success)
                throw new Exception("Provisioning file doesn't match bundle id " + PlayerSettings.applicationIdentifier);
            reg = new Regex("<key>UUID</key>\n\t<string>([^<]+)</string>", RegexOptions.Singleline);
            match = reg.Match(text);
            if (!match.Success)
                throw new Exception("Can't find UUID from provisioning file.");
            string id = match.Groups[1].Value;
            string homeDir = Environment.GetEnvironmentVariable("HOME");
            File.Copy(filepath, homeDir + "/Library/MobileDevice/Provisioning Profiles/" + id + ".mobileprovision", true);
            return id;
        }
    }
}