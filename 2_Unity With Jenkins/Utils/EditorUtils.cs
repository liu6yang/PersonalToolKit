using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace common
{
    public static class EditorUtils
    {
        public static List<string> DirectoryCopy(string src, string dst, bool withMeta, bool withoutClean = false)
        {
            if (withMeta && (Path.GetFullPath(src).StartsWith(EditorEnv.dstUnityProjectAssets) && Path.GetFullPath(dst).StartsWith(EditorEnv.dstUnityProjectAssets)))
                throw new Exception("You'd better not to copy whole directory with meta files at both folders under ./Assets/ path.");

            if (!withoutClean && Directory.Exists(dst))
                Directory.Delete(dst, true);
			if (!Directory.Exists(dst))
				Directory.CreateDirectory(dst);

			List<string> allFiles = new List<string>();
            string[] dirs = Directory.GetDirectories(src);
            foreach (string dir in dirs)
            {
                var files_ = DirectoryCopy(dir, dst + "/" + Path.GetFileName(dir), withMeta, withoutClean);
				allFiles.AddRange(files_);
            }

            string[] files = Directory.GetFiles(src);
            foreach (string file in files)
            {
                if (file.EndsWith(".meta") && !withMeta)
                    continue;
                File.Copy(file, dst + "/" + Path.GetFileName(file));
				allFiles.Add(file);
            }
			return allFiles;
        }

        public static void DirectoryMove(string src, string dst)
        {
            Debug.Log("Start move dir " + src + " to " + dst);
            string[] srcFiles = Directory.GetFiles(src, "*", SearchOption.AllDirectories);
            string[] dstFiles = Directory.GetFiles(dst, "*", SearchOption.AllDirectories);
            srcFiles = srcFiles.Select(f => f.Replace("\\", "/").Replace(src, "")).ToArray();
            dstFiles = dstFiles.Select(f => f.Replace("\\", "/").Replace(dst, "")).ToArray();
            foreach (string file in srcFiles)
            {
                if (dstFiles.Contains(file))
                {
                    if (!file.EndsWith(".meta"))
                        throw new Exception("File already exist " + file);
                    string path = file.Substring(0, file.Length - 5);
                    if (!Directory.Exists(dst + "/" + path))
                        throw new Exception("File already exist " + file);
                }
            }

            foreach (string file in srcFiles)
            {
                string dir = Path.GetDirectoryName(dst + "/" + file);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                if (!File.Exists(dst + "/" + file))
                    File.Move(src + "/" + file, dst + "/" + file);
            }
            Debug.Log("End move dir " + src + " to " + dst);
        }

        public static void ExecuteCmd(string cmd, string workingDir = null, bool ignoreError = false)
        {
            Debug.Log("\nExecute cmd: " + cmd);
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            bool isOutputEnd = false;
			bool isErrorEnd  = false;
            bool isError = false;

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                p.StartInfo.FileName = "cmd";
                p.StartInfo.Arguments = "/C \"" + cmd + "\"";
                p.StartInfo.EnvironmentVariables["PATH"] = "C:\\Windows\\system32;" + p.StartInfo.EnvironmentVariables["PATH"];
            }
            else
            {
                p.StartInfo.FileName = "bash";
                p.StartInfo.Arguments = "-c '" + cmd + "'";
            }
            if (!string.IsNullOrEmpty(workingDir))
			{
				p.StartInfo.WorkingDirectory = workingDir;
			}
			p.StartInfo.CreateNoWindow = true;
            p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.UseShellExecute = false;

			List<string> outputList = new List<string>();
            p.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    //Debug.Log("[CMD Output] " + e.Data);
					outputList.Add(e.Data);
                }
				else
				{
					Debug.Log("isOutputEnd true");
					isOutputEnd = true;
				}
            };
            p.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    //Debug.LogError("[CMD Error] " + e.Data);
                    outputList.Add(e.Data);
					outputList.Add("cmd has error, " + cmd);
                    isError = true;
                }
				else
				{
					Debug.Log("isErrorEnd true");
					isErrorEnd = true;
				}
            };

            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

			Debug.Log("wait cmd end.");
			if (Application.platform == RuntimePlatform.WindowsEditor)
			{
				while (!isOutputEnd && !isErrorEnd)
				{
					Thread.Sleep(100);
				}

				Debug.Log("check pid exist.");
				int pid = 0;
				try
				{
					pid = p.Id;
					Debug.Log("pid is " + pid);
				}
				catch (InvalidOperationException)
				{
					Debug.Log("Can't get pid, means process is end.");
				}

				if (pid > 0)
				{
					while (true)
					{
						System.Diagnostics.Process[] processes = System.Diagnostics.Process.GetProcesses();
						bool exist = false;
						foreach (var process in processes)
						{
							try
							{
								if (pid == process.Id)
								{
									exist = true;
								}
							}
							catch (InvalidOperationException e)
							{
								continue;
							}
						}
						if (!exist)
						{
							Debug.Log("pid " + pid + " doesn't exist anymore.");
							break;
						}
						Thread.Sleep(100);
					}
				}
			}
			else
			{
				p.WaitForExit();
			}

            Debug.Log("cmd process is end.");
            if (p.ExitCode != 0)
			{
				isError = true;
				outputList.Add("process exit error, " + cmd);
			}
				
            int waitCount = 0;
            while (!isOutputEnd || !isErrorEnd)
            {
				outputList.Add("cmd output is not end " + waitCount);
                Thread.Sleep(500);
                ++waitCount;
                if (waitCount >= 4)
				{
					outputList.Add("isEnd is not set, force end.");
					break;
				}
            }
			p.Close();

			string output = string.Join(",", outputList.ToArray());
			output = "=== CMD Output begin, " + cmd + "\n"
				+ output + "\n"
				+ "=== CMD Output end, " + cmd;
			if (isError)
				if (ignoreError)
					Debug.LogWarning(output);
				else
					Debug.LogError(output);
			else
				Debug.Log(output);
            Debug.Log("Execute cmd end: " + cmd + "\n" + " ,isError = " + isError);
            if (isError)
			{
				if (ignoreError)
					Debug.LogWarning("cmd has error, but ignored.");
				else
					throw new Exception("cmd has error, " + cmd);
			}
        }
    }

    class EditorUtilsTest
    {
        [MenuItem("Test/Directory Copy")]
        static void DirectoryCopyTest()
        {
            EditorUtils.DirectoryCopy("C:/Users/lilingfeng/t/t1", "C:/Users/lilingfeng/t/t2", true);
        }

        [MenuItem("Test/Directory Move")]
        static void DirectoryMoveTest()
        {
            EditorUtils.DirectoryMove("./Assets/DLC_Local", "./Assets/Resources");
        }

		[MenuItem("Test/Cmd Test")]
		static void CmdTest()
		{
			EditorUtils.ExecuteCmd("rmdir /S /Q res", "D:/DummyProject/DummyProject/Assets/Plugins/Android");
		}
    }
}
