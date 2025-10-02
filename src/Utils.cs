using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonyPatchChangeParser
{
    internal static class Utils
    {
        /// <summary>
        /// Executes the git diff command and returns the output.
        /// </summary>
        /// <param name="gameSourcePath"></param>
        /// <param name="gitExePath"></param>
        /// <param name="commitA"></param>
        /// <param name="commitB"></param>
        /// <param name="gitAdditionalArguments"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal static string ExecuteGitCommand(string gameSourcePath, string gitExePath, string commitA, string commitB, string gitAdditionalArguments = "")
        {
            //TODO: .exe is windows specific.
            string exeFile = Path.Join(gitExePath, "git.exe");


            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = exeFile,
                Arguments = $"diff {gitAdditionalArguments} \"{commitA}\" \"{commitB}\"",
                WorkingDirectory = gameSourcePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process process = new Process();
            process.StartInfo = startInfo;

            process.Start();
            //process.BeginOutputReadLine();
            string output = process.StandardOutput.ReadToEnd();
            string errors = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Git process failed with exit code {process.ExitCode}.\nOutput: {errors}");
            }

            return output;
        }
    }
}
