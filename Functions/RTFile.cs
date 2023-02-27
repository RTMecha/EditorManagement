using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace EditorManagement.Functions
{
	public static class RTFile
	{
		public static string GetApplicationDirectory()
		{
			return Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("/")) + "/";
		}

		public static bool FileExists(string _filePath)
		{
			return !string.IsNullOrEmpty(_filePath) && File.Exists(_filePath);
		}

		public static bool DirectoryExists(string _directoryPath)
		{
			return !string.IsNullOrEmpty(_directoryPath) && Directory.Exists(_directoryPath);
		}

		public static void WriteToFile(string path, string json)
		{
			StreamWriter streamWriter = new StreamWriter(path);
			streamWriter.Write(json);
			streamWriter.Flush();
			streamWriter.Close();
		}

		public static class OpenInFileBrowser
		{
			public static bool IsInMacOS
			{
				get
				{
					return SystemInfo.operatingSystem.IndexOf("Mac OS") != -1;
				}
			}

			public static bool IsInWinOS
			{
				get
				{
					return SystemInfo.operatingSystem.IndexOf("Windows") != -1;
				}
			}

			public static void OpenInMac(string path)
			{
				bool flag = false;
				string text = path.Replace("\\", "/");
				if (Directory.Exists(text))
				{
					flag = true;
				}
				if (!text.StartsWith("\""))
				{
					text = "\"" + text;
				}
				if (!text.EndsWith("\""))
				{
					text += "\"";
				}
				string arguments = (flag ? "" : "-R ") + text;
				try
				{
					Process.Start("open", arguments);
				}
				catch (Win32Exception ex)
				{
					ex.HelpLink = "";
				}
			}

			public static void OpenInWin(string path)
			{
				bool flag = false;
				string text = path.Replace("/", "\\");
				if (Directory.Exists(text))
				{
					flag = true;
				}
				try
				{
					Process.Start("explorer.exe", (flag ? "/root," : "/select,") + text);
				}
				catch (Win32Exception ex)
				{
					ex.HelpLink = "";
				}
			}

			public static void Open(string path)
			{
				if (IsInWinOS)
				{
					OpenInWin(path);
					return;
				}
				if (IsInMacOS)
				{
					OpenInMac(path);
					return;
				}
			}
		}
	}
}
