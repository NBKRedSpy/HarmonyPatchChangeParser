using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace HarmonyPatchChangeParser
{
    internal class CommandLineOptions
    {
        [Option('a', "git-commit-a", Required = true, HelpText = "The first Git commit hash or reference.")]
        public string GitCommitA { get; set; } = "";

        [Option('b', "git-commit-b", Required = true, HelpText = "The second Git commit hash or reference.")]
        public string GitCommitB { get; set; } = "";

        [Option('s', "game-source-path", Required = true, HelpText = "Path to the game source directory.")]
        public string GameSourcePath { get; set; } = "";

        [Option('m', "harmony-mods-path", Required = true, HelpText = "Path to the Harmony mods directory.")]
        public string HarmonyModsPath { get; set; } = "";

        [Option('h', "harmony-output-file", HelpText = "The path to the output file for the harmony patches report.  Use - to output to the console.  Use '' to not export.", Default = "HarmonyReport.tsv")]
        public string HarmonyReportFilePath { get; set; } = "";

        [Option('f', "game-file-changes", Required = false, HelpText = "The path to output the files that were changed in the git commits. Use - to output to the console.  Use '' to not export.", Default = "GameFileChanges.tsv")]
        public string GameFileChanges { get; set; } = "";

        [Option('c', "include-copy-warnings", Required = false, HelpText = "If set, will include any lines which contain the text 'copy' to " +
            "try to find any copy and replace patches.  The word 'copy' is by convention.", Default = true)]
        public bool? IncludeCopyWarnings { get; set; } = true;

        [Option('t', "include-harmony-text-matches",Required = false, HelpText = "Includes the simple 'Harmony' text match in the file changes")]
        public bool? IncludeHarmonyTextMatches { get; set; }

        [Option('g', "git-path", Required = false, HelpText = "The path to the git executable. Use '' to require git to be in the path.", Default = "")]
        public string GitPath { get; set; } = "";
    }
}
