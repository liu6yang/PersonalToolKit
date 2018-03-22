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
    class Build
    {
        static string[] SCENES = FindEnabledEditorScenes();
        static string APP_NAME = Application.productName.Replace(' ', '_');
        static string TARGET_DIR = "target";


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

        //static string PerformAndroidBuild_Debug()
        //{
        //    string target = "_" + APP_NAME + ".apk";
        //    string dir = "./";
        //    target = dir + target;
        //    GenericBuild(SCENES, target, BuildTarget.Android, BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler);
        //    return target;
        //}

        //[MenuItem("CustomBuild/Build Android And Install (Debug)", priority = 1000)]
        //internal static void PerformAndroidBuild_Debug_Install()
        //{
        //    string target = PerformAndroidBuild_Debug();
        //    InstallApk(target);
        //}

        //[MenuItem("CustomBuild/Build Android And Install (Release)", priority = 1000)]
        //internal static void PerformAndroidBuild_Install()
        //{
        //    string target = "_" + APP_NAME + ".apk";
        //    string dir = "./";
        //    target = dir + target;
        //    GenericBuild(SCENES, target, BuildTarget.Android, BuildOptions.None);
        //    InstallApk(target);
        //}

        [MenuItem("CustomBuild/Build Android", priority = 1000)]
        static void PerformAndroidBuild()
        {
            string target = APP_NAME + ".apk";
            string dir = "./" + TARGET_DIR + "/Android/" + DateTime.Now.ToString("MM-dd_HH-mm-ss");
            GenericBuild(SCENES, dir + "/" + target, BuildTarget.Android, BuildOptions.None);
        }

		static void GenericBuild(string[] scenes, string target, BuildTarget build_target, BuildOptions build_options)
		{
			if (build_target != EditorUserBuildSettings.activeBuildTarget)
			{
				throw new Exception("You need switch platform to " + build_target + " by your own, in case wrong operation.");
			}

			// Version number
			/*var gameVersion = CommandLineReader.GetCustomArgument("Game_Version").Trim(new [] {'\r', '\n'});
			if (string.IsNullOrEmpty(gameVersion))
			{
		#if !SERVER_BUILD
				gameVersion = "0.0.1";
		#else
				throw new Exception("missing Game_Version in command line args");
		#endif
            }
			PlayerSettings.bundleVersion = gameVersion;*/

			/*string target_ = CommandLineReader.GetCustomArgument("Build_Target");
		    if (!String.IsNullOrEmpty(target_))
			    target = target_;
            string dir = target.Substring(0, target.LastIndexOf("/"));
            Directory.CreateDirectory(dir);*/

            string prjTarget = target;
            if (Directory.Exists(prjTarget))
                Directory.Delete(prjTarget, true);

			Debug.Log("Start build " + build_target.ToString() + " with option " + build_options.ToString() + " to " + prjTarget);

            string res = BuildPipeline.BuildPlayer(scenes, prjTarget, build_target, build_options);
            if (res.Length > 0)
            {
                throw new Exception("BuildPlayer failure: " + res);
            }
        }
    }
}