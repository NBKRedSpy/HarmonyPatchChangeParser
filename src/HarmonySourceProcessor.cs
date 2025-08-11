using System;
using System.Collections.Generic;
using SD = System.Diagnostics;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Security.Principal;
using CsvHelper;
using CsvHelper.Configuration;
using System.Xml;

namespace HarmonyPatchChangeParser
{
    internal class HarmonySourceProcessor
    {
        private List<string> GetGitFilenameChanges(string gameSourcePath, string gitExePath, string commitA, string commitB)
        {

            //TODO: .exe is windows specific.
            string exeFile = Path.Join(gitExePath, "git.exe");


            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = exeFile,
                Arguments = $"-P diff \"{commitA}\" \"{commitB}\" --name-only",
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

            //Adding a distinct just in case, but they should be unique.
            List<string> changedFiles = Regex.Split(output, "\r?\n") .Distinct()
                .Order()
                .ToList();

            changedFiles.Remove("");    //The git output is adding a ""
            return changedFiles;
        }

        /// <summary>
        /// Processes all of the mods' patches and compares them to the git changes.
        /// </summary>
        /// <param name="args">The settings</param>
        /// 
        /// <returns>The HarmonyPatchChanges in tab separated format </returns>
        /// <param name="gameFileChanges">The list of file files that have changed in the game's git diff.</param>
        public string Process(CommandLineOptions args,out string gameFileChanges)
        {
            List<HarmonyPatchChange> harmonyPatchChanges = GetModPatches(args.GameSourcePath, args.GitPath, args.GitCommitA, args.GitCommitB, args.HarmonyModsPath, out HashSet<string> gameGitChanges);

            CsvConfiguration config = new CsvConfiguration(System.Globalization.CultureInfo.CurrentCulture)
            {
                Delimiter = "\t",
                HasHeaderRecord = true,
            };

            gameFileChanges = string.Join("\n", gameGitChanges);

            // Output the copy warning lines
            //Looping through the files again, but not a big deal.

            if (args.IncludeCopyWarnings)
            {
                List<HarmonyPatchChange> copyWarningLines = CopyWarningLines(args.HarmonyModsPath, gameGitChanges);
                harmonyPatchChanges.AddRange(copyWarningLines);

            }

            string harmonyPatchReportTsv = "";

            //Avoids needing to create a CsvHelper map since oddly,
            //  CSV Helper does use CsvHelper Index attribute for writing.
            var columnOrderedPatches = harmonyPatchChanges
                .Select(x => new
                {
                    x.ChangeType,
                    x.PatchObjectType,
                    x.FileName,
                    x.HarmonyPatchLine,
                })
                .OrderBy(x => x.FileName)
                .ThenBy(x => x.PatchObjectType)
                .ThenBy(x => x.ChangeType.SortOrder());


            using (var writer = new StringWriter())
            using (var csv = new CsvWriter(writer, config))
            {
                csv.WriteRecords(columnOrderedPatches);
                harmonyPatchReportTsv = writer.ToString();
            }

            return harmonyPatchReportTsv;

        }


        /// <summary>
        /// returns the lines which contain the text "copy".  By convention this generally means the patch
        /// Is a completely copy and replace of the original code.
        /// </summary>
        /// <param name="harmonyModsPath"></param>
        /// <param name="gameGitChanges"></param>
        /// <returns>The HarmonyPatchChanges that have a copy warning</returns>
        private List<HarmonyPatchChange> CopyWarningLines(
                    string harmonyModsPath,
                    HashSet<string> gameGitChanges)
        {

            var copyPatches = new List<HarmonyPatchChange>();


            foreach (string file in Directory.GetFiles(harmonyModsPath, "*.cs", SearchOption.AllDirectories))
            {
                //Copy that is whole word only
                Regex regex = new Regex(@"\bcopy\b", RegexOptions.IgnoreCase);

                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];

                    bool hasCopy = regex.IsMatch(line);

                    if (hasCopy)
                    {
                        var patch = new HarmonyPatchChange
                        {
                            FileName = file,
                            HarmonyPatchLine = line.Trim(),
                            PatchObjectType = "",
                            ChangeType = ChangeType.CopyWarning
                        };

                        copyPatches.Add(patch);
                    }
                }
            }

            return copyPatches;
        }

        private  List<HarmonyPatchChange> GetModPatches(
            string gameSourcePath,
            string gitPath,
            string gitCommitA,
            string gitCommitB,
            string harmonyModsPath,
            out HashSet<string> gameGitChanges
        )
        {
            // Lookup of file names that have changed.
            List<string> fileChanges = GetGitFilenameChanges(gameSourcePath, gitPath, gitCommitA, gitCommitB);

            // TODO: this is quasimorph specific. Leaving for now since that is what this tool is for.
            // HACK: assumes there are no sub directories in the MGSC directory.
            gameGitChanges = fileChanges.Where(x => x.StartsWith("MGSC/"))
                .Select(x =>
                {
                    string result;
                    result = Regex.Replace(x, "^MGSC/", "");
                    result = Regex.Replace(result, @"\.cs$", "");
                    return result;
                })
                .ToHashSet();

            List<HarmonyPatchChange> modPatches = GetHarmonyPatches(harmonyModsPath);

            // Join the patches to the git changes
            HashSet<string> tempGitChanges = gameGitChanges;

            modPatches.ForEach(patch =>
            {
                patch.ChangeType = tempGitChanges.Contains(patch.PatchObjectType) ? ChangeType.HarmonyPatchChange : ChangeType.None;
            });


            return modPatches;
        }

      
        /// <summary>
        /// Returns the Patch information for each file.
        /// </summary>
        /// <param name="harmonyModsPath"></param>
        /// <returns></returns>
        private List<HarmonyPatchChange> GetHarmonyPatches(string harmonyModsPath)
        {
            // Regex to match [HarmonyPatch(typeof(Foo.Bar))]
            Regex regex = new Regex(@"\[HarmonyPatch\s*\(\s*typeof\(([^\)]+)\s*\)");

            List<HarmonyPatchChange> patches = new List<HarmonyPatchChange>();

            foreach (string file in Directory.GetFiles(harmonyModsPath, "*.cs", SearchOption.AllDirectories))
            {
                string[] lines = File.ReadAllLines(file);

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];

                    if (!line.Contains("HarmonyPatch") && !line.Contains("HarmonyMethod")) continue;

                    //--Get the typeof() value if present.
                    Match match = regex.Match(line);
                    string objectType = "";
                    
                    if (match.Success) 
                    {
                        objectType = match.Groups[1].Value.Trim();
                    }

                    var patch = new HarmonyPatchChange
                    {
                        FileName = file,
                        HarmonyPatchLine = line.Trim(),
                        PatchObjectType = objectType,
                        ChangeType = ChangeType.Invalid,       //this will be set later.
                    };

                    patches.Add(patch);
                }
            }

            return patches;
        }
    }
}
