using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace jClipCornLink
{
	static class Program
	{
		private static readonly string PATH_APPDATA = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		private static readonly string PATH_CONFIG = Path.Combine(PATH_APPDATA, "jClipCorn", "jClipCornLink.cfg");
		private static readonly string PATH_LOG = Path.Combine(PATH_APPDATA, "jClipCorn", "jClipCornLink.log");

		private static readonly Regex REGEX_DRIVENAME   = new Regex(@"<\?vLabel=""(?<param>[^\""]+?)"">");
		private static readonly Regex REGEX_DRIVELETTER = new Regex(@"<\?vLetter=""(?<param>[A-Z])"">");
		private static readonly Regex REGEX_SELFPATH    = new Regex(@"<\?self>");
		private static readonly Regex REGEX_SELFDRIVE   = new Regex(@"<\?self\[dir\]>");

		private static readonly string SELF_PATH = Directory.GetCurrentDirectory();
		private static readonly string SELF_DRIVE = Directory.GetDirectoryRoot(Directory.GetCurrentDirectory());
		private static readonly List<DriveInfo> DRIVES = DriveInfo.GetDrives().Where(p => p.IsReady).ToList();



		[STAThread]
		static void Main()
		{
			try
			{
				Run();
			}
			catch (Exception e)
			{
				WriteLogError(string.Format("jClipCornLink encountered an exception: {0}:\r\n{1}", e, e.StackTrace));
			}
		}

		private static void Run()
		{
			Directory.CreateDirectory(Directory.GetParent(PATH_CONFIG).FullName);
			if (!File.Exists(PATH_CONFIG)) File.WriteAllText(PATH_CONFIG, string.Empty);

			Directory.CreateDirectory(Directory.GetParent(PATH_LOG).FullName);
			if (!File.Exists(PATH_LOG)) File.WriteAllText(PATH_LOG, string.Empty);

			var lines = File.ReadAllLines(PATH_CONFIG)
				.Where(p => !string.IsNullOrWhiteSpace(p))
				.Where(p => ! p.StartsWith("#"))
				.ToList();

			if (!lines.Any())
			{
				WriteLogError("jClipCornLink.cfg is empty - exiting");
				return;
			}

			foreach (var path in lines)
			{
				string file = ResolvePath(path);

				if (File.Exists(file))
				{
					if (file.EndsWith(".jar"))
					{
						Process.Start(new ProcessStartInfo("java.exe", "-jar \"" + file + "\"")
						{
							CreateNoWindow = true,
							UseShellExecute = false,

							WorkingDirectory = Directory.GetParent(file).FullName,
						});

						WriteLogInfo(string.Format("Start jClipCorn (jar): '{0}' (configured path: '{1}')", file, path));
						return;
					}
					if (file.EndsWith(".exe"))
					{
						Process.Start(new ProcessStartInfo(file)
						{
							WorkingDirectory = Directory.GetParent(file).FullName,
						});

						WriteLogInfo(string.Format("Start jClipCorn (exe): '{0}' (configured path: '{1}')", file, path));
						return;
					}

					WriteLogError(string.Format("Unknown extension: '{0}' (configured path: '{1}')", file, path));
				}
				else
				{
					WriteLogError(string.Format("File not found: '{0}' (configured path: '{1}')", file, path));
				}

			}

			WriteLogError("Unable to locate jClipCorn - exiting");
		}

		private static void WriteLogError(string logline)
		{
			if (!File.Exists(PATH_LOG)) File.WriteAllText(PATH_LOG, string.Empty);

			File.AppendAllText(PATH_LOG, string.Format("\r\n\r\n[ERROR] [{0:yyyy-MM-dd HH:mm:ss}] {1}", DateTime.Now, logline), Encoding.UTF8);
		}

		private static void WriteLogInfo(string logline)
		{
			if (!File.Exists(PATH_LOG)) File.WriteAllText(PATH_LOG, string.Empty);

			File.AppendAllText(PATH_LOG, string.Format("\r\n\r\n[INFO]  [{0:yyyy-MM-dd HH:mm:ss}] {1}", DateTime.Now, logline), Encoding.UTF8);
		}

		private static string ResolvePath(string relPath)
		{
			relPath = REGEX_SELFDRIVE.Replace(relPath, SELF_DRIVE);
			relPath = REGEX_SELFPATH.Replace(relPath, SELF_PATH + @"\");

			relPath = REGEX_DRIVELETTER.Replace(relPath, m => m.Groups["param"].Value + @":\");
			relPath = REGEX_DRIVENAME.Replace(relPath, m => DRIVES.FirstOrDefault(p => p.VolumeLabel.ToLower() == m.Groups["param"].Value.ToLower())?.Name ?? "(?ERR DRIVE NOT FOUND?)");

			return relPath;
		}
	}
}
