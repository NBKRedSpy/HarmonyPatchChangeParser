using CsvHelper;
using CsvHelper.Configuration;
using ParseDiff;
using System.Text.RegularExpressions;

namespace HarmonyPatchChangeParser
{
    /// <summary>
    /// A simple processor that uses text searches to match mod lines with "HarmonyPatch" 
    /// to git changes for a game.
    /// </summary>
    internal class HarmonySourceProcessor
    {


        /// <summary>
        /// Processes all of the mods' patches and compares them to the git changes.
        /// </summary>
        /// <param name="args">The settings</param>
        /// 
        /// <returns>The HarmonyPatchChanges in tab separated format </returns>
        /// <param name="gameFileChanges">The output of the game's file changes.
        /// This is identical to 'git diff --name-only'</param>
        public string Process(CommandLineOptions args, out string gameFileChanges)
        {

            //---- Create git changes file name lookup
            // Git file changes with the MGSC/ prefix removed, and the .cs suffix removed.  This is a crude convention
            //  to match the class name to the mod harmony patches.  

            // Lookup of file names that have changed.
            List<string> fileChanges = GetGitFilenameChanges(args.GameSourcePath, args.GitPath, args.GitCommitA, args.GitCommitB);
            gameFileChanges = string.Join("\n", fileChanges);


            Regex mgscRegex = new ("^MGSC/");
            Regex csExtensionRegex = new(@"\.cs$");

            // TODO: Expecting a MGSC prefix is quasimorph specific. Leaving for now since that is what this tool is for.
            // HACK: assumes there are no sub directories in the MGSC directory.
            HashSet<string> gameGitChanges = fileChanges
                .Where(x => x.StartsWith("MGSC/"))
                .Select(x =>
                {
                    string result;
                    result = mgscRegex.Replace(x, "");
                    result = csExtensionRegex.Replace(result, "");
                    return result;
                })
                .ToHashSet();

            List<HarmonyPatchChange> harmonyPatchMatches = new();  //Contains the patches from all sources.

            //---- "Harmony" text based matches
            if (args.IncludeHarmonyTextMatches ?? true)
            {
                //Get the list of mod patches that contain the text "HarmonyPatch".  Joined with the game's files that have changed.
                //  This is primarily a legacy match, but it can still find some patches that the C# parser misses.
                List<HarmonyPatchChange> modPatches = GetHarmonyPatches(args.HarmonyModsPath);

                modPatches.ForEach(patch =>
                {
                    patch.ChangeType = gameGitChanges.Contains(patch.PatchObjectType) ? ChangeType.HarmonyPatchTextMatchChange : ChangeType.None;
                });

                harmonyPatchMatches.AddRange(modPatches);


            }

            //--- Changes using the C# Parser
            // Uses a C# parser to get the HarmonyPatch attributes and match them to the game's changes.
            // This is more accurate than the text match above.
            List<HarmonyPatchInfo> patchCodeParseChanges = new GameChangeCodeProcessor().ProcessChanges(args.GameSourcePath, args.HarmonyModsPath, args.GitPath,
                args.GitCommitA, args.GitCommitB);

            harmonyPatchMatches.AddRange(
                patchCodeParseChanges.Select(x => new HarmonyPatchChange
                {
                    ChangeType = ChangeType.HarmonyPatchParsedMatch,
                    FileName = x.FilePath,
                    HarmonyPatchLine = x.AttributeText,
                    PatchObjectType = x.FullTargetName
                }
                ));

            //--- Copy warnings
            if (args.IncludeCopyWarnings ?? true)
            {
                // Add any lines which have the word "copy" in them, and the the game's file has changed.
                // This is a convention where the mod code will use the word "copy" to indicate a full copy and replace of a function.
                //  For example:  "//COPY: This is a full copy and replace of the Foo.Bar method."

                List<HarmonyPatchChange> copyWarningLines = CopyWarningLines(args.HarmonyModsPath, gameGitChanges);
                harmonyPatchMatches.AddRange(copyWarningLines);
            }

            //---- Create the report
            string harmonyPatchReportTsv = CreateCsvReport(harmonyPatchMatches);

            return harmonyPatchReportTsv;
        }

        /// <summary>
        /// Writes the CSV report
        /// </summary>
        /// <param name="harmonyPatchTextMatchChanges"></param>
        /// <returns></returns>
        private static string CreateCsvReport(List<HarmonyPatchChange> harmonyPatchTextMatchChanges)
        {
            string harmonyPatchReportTsv = "";

            //Avoids the need to create a CsvHelper map; note that CsvHelper does not use the CsvHelper Index attribute for writing.
            var columnOrderedPatches = harmonyPatchTextMatchChanges
                .Select(x => new
                {
                    x.ChangeType,
                    x.PatchObjectType,
                    x.FileName,
                    HarmonyPatchLine = x.HarmonyPatchLine.ReplaceLineEndings(" "),
                })
                .OrderBy(x => x.FileName)
                .ThenBy(x => x.ChangeType)
                .ThenBy(x => x.PatchObjectType);

            CsvConfiguration config = new CsvConfiguration(System.Globalization.CultureInfo.CurrentCulture)
            {
                Delimiter = "\t",
                HasHeaderRecord = true,
                ShouldQuote = x => true,
            };

            using (var writer = new StringWriter())
            using (var csv = new CsvWriter(writer, config))
            {
                csv.WriteRecords(columnOrderedPatches);
                harmonyPatchReportTsv = writer.ToString();
            }

            return harmonyPatchReportTsv;
        }

        /// <summary>
        /// Returns the git diff between the two commits as a list of FileDiff objects.
        /// Uses a unified context of 0 to minimize the amount of data returned.
        /// </summary>
        /// <param name="gameSourcePath"></param>
        /// <param name="gitExePath"></param>
        /// <param name="commitA"></param>
        /// <param name="commitB"></param>
        /// <returns></returns>
        private List<FileDiff> GetGameMethodChanges(string gameSourcePath, string gitExePath, string commitA, string commitB)
        {
            string patchText = Utils.ExecuteGitCommand(gameSourcePath, gitExePath, commitA, commitB, "--unified=0");


            return Diff.Parse(patchText).ToList();
        }


        /// <summary>
        /// Gets the list of filenames that have changed between the two commits.
        /// </summary>
        /// <param name="gameSourcePath"></param>
        /// <param name="gitExePath"></param>
        /// <param name="commitA"></param>
        /// <param name="commitB"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private List<string> GetGitFilenameChanges(string gameSourcePath, string gitExePath, string commitA, string commitB)
        {
            string output = Utils.ExecuteGitCommand(gameSourcePath, gitExePath, commitA, commitB, "--name-only");

            //Adding a distinct just in case, but they should be unique.
            List<string> changedFiles = Regex.Split(output, "\r?\n").Distinct()
                .Order()
                .ToList();

            changedFiles.Remove("");    //The git output is adding a ""
            return changedFiles;
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
