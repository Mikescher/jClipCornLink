using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace jClipCornLink
{
	static class Program
    {
        private static readonly string SELF_PATH = Directory.GetCurrentDirectory();

		private static readonly string PATH_CONFIG = Path.Combine(SELF_PATH, "jClipCornLink.cfg");
		private static readonly string PATH_LOG = Path.Combine(SELF_PATH, "jClipCornLink.log");

		private static readonly Regex REGEX_DRIVENAME   = new Regex(@"<\?vLabel=""(?<param>[^\""]+?)"">");
		private static readonly Regex REGEX_DRIVELETTER = new Regex(@"<\?vLetter=""(?<param>[A-Z])"">");
		private static readonly Regex REGEX_SELFPATH    = new Regex(@"<\?self>");
		private static readonly Regex REGEX_SELFDRIVE   = new Regex(@"<\?self\[dir\]>");
		private static readonly Regex REGEX_DYN_VERSION = new Regex(@"^\{VERSION\-(?<index>[0-9]+)\}$");
		private static readonly Regex REGEX_DYNAMIC     = new Regex(@"\{.*?\}");

		private static readonly string SELF_DRIVE = Directory.GetDirectoryRoot(Directory.GetCurrentDirectory());
		private static readonly List<DriveInfo> DRIVES = DriveInfo.GetDrives().Where(p => p.IsReady).ToList();

		private static bool ShowErrorBoxes = false;

		[STAThread]
		static void Main()
        {
            Console.Out.WriteLine("[STARTUP]");
            Console.Out.WriteLine($"[CONFIG] SELF_PATH   := {SELF_PATH}");
            Console.Out.WriteLine($"[CONFIG] SELF_DRIVE  := {SELF_DRIVE}");
            Console.Out.WriteLine($"[CONFIG] PATH_CONFIG := {PATH_CONFIG}");
            Console.Out.WriteLine($"[CONFIG] PATH_LOG    := {PATH_LOG}");

            try
			{
				Run();
			}
			catch (Exception e)
			{
				WriteLogError($"jClipCornLink encountered an exception: {e}:\r\n{e.StackTrace}");
			}
			
			Console.Out.WriteLine("[DONE]");
		}

		private static void Run()
		{
			Directory.CreateDirectory(Directory.GetParent(PATH_CONFIG).FullName);
			if (!File.Exists(PATH_CONFIG))
			{
				Console.Out.WriteLine($"[DEBUG] Create config file '{PATH_CONFIG}'");
				File.WriteAllText(PATH_CONFIG, string.Empty);
			}

			Directory.CreateDirectory(Directory.GetParent(PATH_LOG).FullName);
			if (!File.Exists(PATH_LOG))
			{
				Console.Out.WriteLine($"[DEBUG] Create log file '{PATH_LOG}'");
				File.WriteAllText(PATH_LOG, string.Empty);
			}

			Console.Out.WriteLine($"[DEBUG] Read config file '{PATH_CONFIG}'");
			var lines = File.ReadAllLines(PATH_CONFIG).ToList();
			
			Console.Out.WriteLine($"[DEBUG] Read {lines.Count} lines from config");

			if (!lines.Any())
			{
				WriteLogError("jClipCornLink.cfg is empty - exiting");
				return;
			}

            var map = new Dictionary<string, string>
            {
                ["java"]      = "java.exe",
                ["showerror"] = "false",
				["net_use"]   = "",
            };

            try
			{
				ReadOptions(lines, map);

                ShowErrorBoxes = (map["showerror"] == "true");

                Console.Out.WriteLine($"[DEBUG] Options:\n{string.Join("\n", map.Select(p => p.Key + "  =>  " + p.Value))}");

				if (!string.IsNullOrWhiteSpace(map["net_use"]))
				{
					Console.Out.WriteLine($"[DEBUG] Run net_use: '{map["net_use"]}'");

					ExecuteNetUse(map["net_use"].Split('\t'));
				}

				var file = FindPath(lines, out var rule);

                if (file != null)
                {
	                Console.Out.WriteLine($"[DEBUG] Found path '{file}' in rule '{rule}'");

                    if (file.EndsWith(".jar"))
                    {
	                    Console.Out.WriteLine($"[DEBUG] Execute {map["java"]} -jar \"{file}\"");

                        Process.Start(new ProcessStartInfo(map["java"], "-jar \"" + file + "\"")
                        {
                            CreateNoWindow = true,
                            UseShellExecute = false,

                            WorkingDirectory = Directory.GetParent(file).FullName,
                        });

                        WriteLogInfo($"Start jClipCorn (jar): '{file}' (configured path: '{rule}')");
                        return;
                    }
                    if (file.EndsWith(".exe"))
                    {
	                    Console.Out.WriteLine($"[DEBUG] Execute \"{file}\"");

                        Process.Start(new ProcessStartInfo(file)
                        {
                            WorkingDirectory = Directory.GetParent(file).FullName,
                        });

                        WriteLogInfo($"Start jClipCorn (exe): '{file}' (configured path: '{rule}')");
                        return;
                    }

                    WriteLogError($"Unknown extension: '{file}' (configured path: '{rule}')");
                }
                else
                {
	                Console.Out.WriteLine($"[DEBUG] Found no path");
                }

                WriteLogError("Unable to locate jClipCorn - exiting");
			}
            catch (Exception e)
			{
				WriteLogError($"jClipCornLink encountered an exception: {e}:\r\n{e.StackTrace}");
				return;
			}
		}

		private static void ExecuteNetUse(string[] uncs)
		{
			foreach (var uncpath in uncs)
			{
				Console.Out.WriteLine($"[DEBUG] Execute net use \"{uncpath}\"");
				
				var output = ProcessHelper.ProcExecute("net", $"use \"{uncpath}\"");

				if (output.ExitCode != 0) WriteLogError($"net use failed with exit code [{output.ExitCode}]: \r\n{output.StdCombined}");
			}
		}

		private static void ReadOptions(List<string> lines, Dictionary<string, string> map)
		{
            foreach (var refline in lines)
            {
                var line = refline.Trim();
                
                Console.Out.WriteLine($"[DEBUG] Parse line '{line}'");

				if (!line.StartsWith("#[")) continue;
                if (!line.EndsWith("]")) continue;

				line = line.Substring(2, line.Length - 3);

				var append = false;
				if (line.StartsWith("[") && line.EndsWith("]"))
				{
					line = line.Substring(2, line.Length - 3);
					append = true;
				}

				var eqidx = line.IndexOf('=');

				var key = line.Substring(0, eqidx).Trim().ToLower();
				var val = line.Substring(eqidx + 1).Trim();

				if (val.StartsWith("\"") && val.EndsWith("\"")) val = val.Substring(1, val.Length-2);

				if (append)
				{
					Console.Out.WriteLine($"[DEBUG] Append config '{key}' with '{val}'");
					
					if (map.ContainsKey(key) && map[key] != "") map[key] += "\t" + val;
					else map[key] = val;
				}
				else
				{
					Console.Out.WriteLine($"[DEBUG] Set config '{key}' to '{val}'");

					map[key] = val;
				}
			}
		}

		private static void WriteLogError(string logline, bool noshow = false)
		{
            Console.Out.WriteLine("[ERROR-LOG] " + logline);

			if (!File.Exists(PATH_LOG)) File.WriteAllText(PATH_LOG, string.Empty);

			File.AppendAllText(PATH_LOG, $"\r\n\r\n[ERROR] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {logline}", Encoding.UTF8);

			if (ShowErrorBoxes && !noshow) ShowExtMessage("Error", logline);
		}

        private static void ShowExtMessage(string title, string msg)
        {
            try
			{
				var path = Path.GetTempFileName();
                File.WriteAllText(path, msg);

                Console.Out.WriteLine($"[DEBUG] Execute SimpleMessagePresenter \"{title.Replace('"', '\'')}\" \"{path}\"");
                
                var p = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "SimpleMessagePresenter.exe",
                        Arguments = $"\"{title.Replace('"', '\'')}\" \"{path}\"",
                    }
                };

                p.Start();
			}
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }

		private static void WriteLogInfo(string logline)
		{
			Console.Out.WriteLine("[INFO-LOG] " + logline);

			if (!File.Exists(PATH_LOG)) File.WriteAllText(PATH_LOG, string.Empty);

			File.AppendAllText(PATH_LOG, $"\r\n\r\n[INFO]  [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {logline}", Encoding.UTF8);
		}

		private static string ResolvePath(string relPath)
		{
			relPath = REGEX_SELFDRIVE.Replace(relPath, SELF_DRIVE);
			relPath = REGEX_SELFPATH.Replace(relPath, SELF_PATH + @"\");

			relPath = REGEX_DRIVELETTER.Replace(relPath, m => m.Groups["param"].Value + @":\");
			relPath = REGEX_DRIVENAME.Replace(relPath, m => DRIVES.FirstOrDefault(p => p.VolumeLabel.ToLower() == m.Groups["param"].Value.ToLower())?.Name ?? "(?ERR DRIVE NOT FOUND?)");

			return relPath;
		}

		private static string ResolveDynamicsAndCheckFile(string file)
		{
			int idxS = file.LastIndexOf('\\');
			int idxE = file.LastIndexOf('.');

			if (idxS == -1 || idxE == -1 || idxE <= idxS) return null;

			string path = file.Substring(0, idxS + 1);
			string filename = file.Substring(idxS + 1, idxE - idxS - 1);
			string extension = file.Substring(idxE + 1);
			
			if (filename.Contains("{") && filename.Contains("}"))
			{
				var dynamics = ListDynamicInString(filename);

				string pattern = CreateDynamicsPattern(filename, extension, dynamics);
				var rex = CreateDynamicsRegex(filename, extension, dynamics);

				var foundTuples = new List<Tuple<string, string>>();

				try
				{
					foreach (var foundFile in Directory.EnumerateFiles(path, pattern))
					{
						string fn = Path.GetFileName(foundFile);

						if (fn == null) continue;

						var match = rex.Item1.Match(fn);

						if (!match.Success) continue;

						string v = string.Join(":", dynamics.Select(d => match.Groups[rex.Item2[d]].Value).Select(int.Parse).Select(p => $"{p:00000000}"));

						foundTuples.Add(Tuple.Create(foundFile, v));
					}
				}
				catch (ArgumentException e)
				{
					WriteLogError($"path: Directory.EnumerateFiles('{path}', '{pattern}'):\r\n" + e.ToString(), true);
					return null;
				}
				catch (DirectoryNotFoundException e)
				{
					WriteLogError($"path: Directory.EnumerateFiles('{path}', '{pattern}'):\r\n" + e.ToString(), true);
					return null;
				}

				if (! foundTuples.Any()) return null;

				return foundTuples.OrderByDescending(p => p.Item2).First().Item1;
			}
			else
			{
				if (File.Exists(file)) return file;

				return null;
			}
		}

		private static Tuple<Regex, Dictionary<string, string>> CreateDynamicsRegex(string filename, string extension, List<string> dynamics)
		{
			var dict = new Dictionary<string, string>();

			filename = filename.Replace(".", "\\.");

			int idxId = 1;
			foreach (var dnmic in dynamics)
			{
				string g = "GROUP_" + (idxId++);
				filename = filename.Replace(dnmic, $"(?<{g}>[0-9]+)");

				dict.Add(dnmic, g);
			}

			var rex = "^" + filename + "\\." + extension + "$";

			try
			{
				return Tuple.Create(new Regex(rex), dict);
			}
			catch (Exception)
			{
				WriteLogError("Cannot create Regex: " + rex);
				return null;
			}
		}

		private static string CreateDynamicsPattern(string filename, string extension, List<string> dynamics)
		{
			return dynamics.Aggregate(filename, (current, dnmic) => current.Replace(dnmic, "*")) + "." + extension;
		}

		private static List<string> ListDynamicInString(string str)
		{
			List<string> result = new List<string>();

			foreach (Match match in REGEX_DYNAMIC.Matches(str))
			{
				string dmic = match.Value;

				if (!REGEX_DYN_VERSION.IsMatch(dmic))
				{
					WriteLogError("Unknown dynamic component: " + dmic);
				}
				else
				{
					result.Add(dmic);
				}
			}

			return result.OrderByDescending(p => int.Parse(REGEX_DYN_VERSION.Match(p).Groups["index"].Value)).ToList();
		} 

		private static string FindPath(List<string> rules, out string foundrule)
		{
			foreach (var rule in rules)
			{
				if (string.IsNullOrWhiteSpace(rule)) continue;

				if (rule.Trim().StartsWith("#") || rule.Trim().StartsWith("//") || rule.Trim().StartsWith(";")) continue;

				try
				{
					Console.Out.WriteLine($"[DEBUG] Evaluate path-rule '{rule}'");
					
					var file = ResolvePath(rule);
					var priorityFile = ResolveDynamicsAndCheckFile(file);

					Console.Out.WriteLine($"[DEBUG] Rule '{rule}' evaluated to {{file: '{file}', priority: '{priorityFile}'}}");

					if (priorityFile != null)
					{
						Console.Out.WriteLine($"[DEBUG] Chosen Path '{priorityFile}' (priorityFile)");

						foundrule = rule;
						return priorityFile;
					}
					else
					{
						WriteLogError($"File not found: '{file}' (configured path: '{rule}')", true);
					}
				}
				catch (Exception e)
				{
					WriteLogError($"Error in resolve rule: '{rule}': {e.Message}\r\n{e.StackTrace}");
					continue;
				}
			}

			Console.Out.WriteLine($"[DEBUG] Found no matching files");

			foundrule = null;
			return null;
		}
	}
}
