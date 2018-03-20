using UnityEngine;
using System.Collections;
using System.IO;

namespace common
{ 
    public class EditorEnv
    {
		/// <summary>
        /// /
        /// </summary>
		public static string rootDir
		{
			get
			{
				return Path.GetFullPath(dstUnityProjectRoot + "/..");
			}
		}

        /// <summary>
        /// /SharedLibraries
        /// </summary>
        public static string sharedLibrariesRoot
        {
            get
            {
                return Path.GetFullPath(dstUnityProjectRoot + "/../SharedLibraries").Replace('\\', '/');
            }
        }

        /// <summary>
        /// /[YourUnityProject]
        /// </summary>
        public static string dstUnityProjectRoot
        {
            get
            {
                return Path.GetFullPath(Application.dataPath + "/..").Replace('\\', '/');
            }
        }


        /// <summary>
        /// /SharedLibraries/Libs
        /// </summary>
        public static string sharedLibrariesLibs
        {
            get
            {
                return sharedLibrariesRoot + "/Libs";
            }
        }

        /// <summary>
        /// /SharedLibraries/SDKs
        /// </summary>
        public static string sharedLibrariesSdks
        {
            get
            {
                return sharedLibrariesRoot + "/SDKs";
            }
        }

        /// <summary>
        /// /SharedLibraries/SDKs/_Android
        /// </summary>
        public static string sharedLibrariesSdksAndroid
        {
            get
            {
                return sharedLibrariesSdks + "/_Android";
            }
        }

        /// <summary>
        /// /[YourUnityProject]/Assets
        /// </summary>
        public static string dstUnityProjectAssets
        {
            get
            {
                return dstUnityProjectRoot + "/Assets";
            }
        }


		public static string CheckAndCreateDir(string path)
		{
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
			return path;
		}

		/// <summary>
		/// /[YourUnityProject]/Assets/_Generated. Create if not exists.
		/// </summary>
		public static string dstUnityProjectGenerated
        {
            get
            {
				return CheckAndCreateDir(dstUnityProjectAssets + "/_Generated");
			}
        }

		///	<summary>
		///	/[YourUnityProject]/Assets/_Generated/Script. Create if not exists.
		/// </summary>
		public static string dstUnityProjectGeneratedScript
		{
			get
			{
				return CheckAndCreateDir(dstUnityProjectGenerated + "/Script");
			}
		}

		/// <summary>
		/// /[YourUnityProject]/Assets/_Generated/Resources. Create if not exists.
		/// </summary>
		public static string dstUnityProjectGeneratedResources
		{
			get
			{
				return CheckAndCreateDir(dstUnityProjectGenerated + "/Resources");
			}
		}

		/// <summary>
		/// /[YourUnityProject]/Assets/_Generated/Res. Create if not exists.
		/// </summary>
		public static string dstUnityProjectGeneratedRes
		{
			get
			{
				return CheckAndCreateDir(dstUnityProjectGenerated + "/Res");
			}
		}



		/// <summary>
		/// /[YourUnityProject]/Assets/_Generated/Resources/Bin_unenc. Create if not exists.
		/// </summary>
		public static string dstUnityProjectGeneratedResourcesBinUnenc
		{
			get
			{
				return CheckAndCreateDir(dstUnityProjectGeneratedResources + "/Bin_unenc");
			}
		}


		/// <summary>
		/// /[YourUnityProject]/Assets/Plugins
		/// </summary>
		public static string dstUnityProjectPlugins
        {
            get
            {
                return dstUnityProjectAssets + "/Plugins";
            }
        }

        /// <summary>
        /// /[YourUnityProject]/Assets/Plugins/Common, subrepo
        /// </summary>
        public static string dstUnityProjectPluginsCommon
        {
            get
            {
                return dstUnityProjectPlugins + "/Common";
            }
        }

        /// <summary>
        /// /[YourUnityProject]/Assets/Plugins/Android, enabled sdks(eclipse project) link to this folder.
        /// </summary>
        public static string dstUnityProjectPluginsAndroid
        {
            get
            {
                return dstUnityProjectPlugins + "/Android";
            }
        }

		/// <summary>
        /// /[YourUnityProject]/Assets/Plugins/iOS
        /// </summary>
        public static string dstUnityProjectPluginsiOS
        {
            get
            {
                return dstUnityProjectPlugins + "/iOS";
            }
        }

        /// <summary>
        /// /[YourUnityProject]/Assets/Plugins/SDKs, enalbed sdks link to this folder.
        /// </summary>
        public static string dstUnityProjectPluginsSDKs
        {
            get
            {
                return dstUnityProjectPlugins + "/SDKs";
            }
        }

		public static string GetRelativePath(string path, string relativeTo)
		{
			if (path.ToLower().StartsWith(relativeTo.ToLower()))
			{
				var p = path.Substring(relativeTo.Length, path.Length - relativeTo.Length);
				if (p.StartsWith("/")) p = p.Substring(1, p.Length - 1);
				return p;
			}
			return path;
		}

	}
}