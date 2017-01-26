using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Gmich.AOP
{

    internal class Weave
    {

        private static Configuration LoadConfiguration()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fileName = "weaving";
            var path = new Uri(assembly.CodeBase).AbsolutePath;
            var directoryName = Path.GetDirectoryName(path);
            var map = new ExeConfigurationFileMap();
            map.ExeConfigFilename = directoryName + $"\\{fileName}.config";
            return ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
        }

        private static StringBuilder logger = new StringBuilder();
        private static void Log(string verbosity, string msg) => logger.AppendLine($"[{verbosity}] [{DateTime.UtcNow}] - {msg}");
        internal static void LogInfo(string msg) => Log("Info", msg);
        internal static void LogError(string msg) => Log("Error", msg);
        internal static void FlushLog(string fileName) => File.WriteAllText(fileName, logger.ToString());

        private static string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
        {
            string[] searchPatterns = searchPattern.Split('|');
            var files = new List<string>();
            foreach (string sp in searchPatterns)
                files.AddRange(Directory.GetFiles(path, sp, searchOption));
            files.Sort();
            return files.ToArray();
        }

        static int Main(string[] args)
        {
            try
            {
                var assemblyPrefix = LoadConfiguration().AppSettings.Settings["AssemblyPrefix"]?.Value ?? "Gmich.";
                string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                var weavers = GetFiles(path, "*.exe|*.dll", SearchOption.TopDirectoryOnly)
                    .Select(file =>
                    {
                        LogInfo($"Checking {file}");
                        return Path.GetFileName(file);
                    })
                    .Where(file =>
                        file.StartsWith(assemblyPrefix)
                        && !file.Contains("Gmich.AOP.exe")
                        && !file.Contains("vshost"))
                    .Select(assemblyPath =>
                    {
                        LogInfo($"Processing {assemblyPath}");
                        return new ILCodeWeaver(assemblyPath);
                    }).ToList();
            }
            catch (Exception ex)
            {
                LogError($"Weaving failure. {ex.ToString()}");
                FlushLog("Gmich.AOP.log");
                return 1;
            }
            FlushLog("Gmich.AOP.log");
            return 0;
        }
    }
}
