using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Tni.Helper.Extensions
{
    /// <summary>
    /// Contains various utilities for strings
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Returns single string with all the list elements joined by separator.
        /// Default: ,
        /// </summary>
        /// <param name="source">List of strings</param>
        /// <param name="separator">Separator (optional)</param>
        /// <returns></returns>
        public static string JoinStr(this List<string> source, string separator = ",")
        {
            return string.Join(separator, source);
        }

        /// <summary>
        /// Runs a provided string as process command.
        /// </summary>
        /// <param name="command">Command to be executed</param>
        /// <param name="waitForOutput">If should await for the output (optional). Default true</param>
        /// <returns></returns>
        public static string RunAsProcessCommand(this string command, bool waitForOutput = true)
        {
            string output = string.Empty;
            var startInfo = new ProcessStartInfo()
            {
                WindowStyle = ProcessWindowStyle.Normal,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                FileName = "Powershell.exe",
                Arguments = command,
                CreateNoWindow = true
            };
            var process = new Process();
            process.StartInfo = startInfo;
            process.Start();
            if (waitForOutput)
            {
                output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }
            return output;
        }

        /// <summary>
        /// To server better deserialization of json to objects
        /// remove any potential comments from body value about to be deserialized.
        /// </summary>
        /// <param name="source">Body of the json</param>
        /// <returns></returns>
        public static string RemoveComments(this string source)
        {
            source = Regex.Replace(source, @"^\s*//.*$", "", RegexOptions.Multiline);
            source = Regex.Replace(source, @"^\s*/\*(\s|\S)*?\*/\s*$", "", RegexOptions.Multiline);

            return source;
        }

        /// <summary>
        /// Compress a source string.
        /// </summary>
        /// <param name="source">String to be compressed.</param>
        /// <returns></returns>
        public static string Compress(this string source)
        {
            var bytes = Encoding.Unicode.GetBytes(source);
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                    msi.CopyTo(gs);

                return Convert.ToBase64String(mso.ToArray());
            }
        }

        /// <summary>
        /// Decompress a compressed string.
        /// </summary>
        /// <param name="source">String to be decompressed</param>
        /// <returns></returns>
        public static string Decompress(this string source)
        {
            var bytes = Convert.FromBase64String(source);
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    gs.CopyTo(mso);
                }
                return Encoding.Unicode.GetString(mso.ToArray());
            }
        }

        /// <summary>
        /// Checks an provided string for a json structure and throws error
        /// if deserialization fails.
        /// </summary>
        /// <param name="json">Json string test value.</param>
        /// <exception cref="Exception"></exception>
        public static void JsonValid(this string json)
        {
            var error = "Invalid json";
            if (string.IsNullOrWhiteSpace(json))
                throw new Exception(error);

            try
            {
                JsonElement jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
            }            
            catch (Exception ex)
            {
                throw new Exception($"{error}: {ex.Message}");
            }
        }
    }
}
