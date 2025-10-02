using HarmonyPatchChangeParser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ParseDiff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HarmonyPatchChangeParser
{
    /// <summary>
    /// Handles processing Harmony patches relative to the changes in the game code, using 
    /// the Roslyn C# parser.
    /// </summary>
    internal class GameChangeCodeProcessor
    {

        /// <summary>
        /// Matches game methods that have changed that are a target of a mod's HarmonyPatch.
        /// </summary>
        /// <param name="gameSourcePath">The directory of the decompiled game.</param>
        /// <param name="modsBasePath">The root folder for all mods.</param>
        /// <param name="gitExePath">The gitExePath.  Leave blank to use the system's path.</param>
        /// <param name="commitA">Git commit a</param>
        /// <param name="commitB">Git commit b</param>
        public List<HarmonyPatchInfo> ProcessChanges(string gameSourcePath, string modsBasePath, string gitExePath, string commitA, string commitB)
        {
            //Get a "class.method" list of changed methods in the game code between the two commits
            HashSet<string> gitChangedMethods = GetGitToGameMethodChanges(gameSourcePath, gitExePath, commitA, commitB);

            //Get the HarmonyPatch attributes from the mod files.  
            List<HarmonyPatchInfo> harmonyPatchMethods = new HarmonyPatchCodeParser().GetModsHarmonyPatches(modsBasePath);
            var matchedPatches = new List<HarmonyPatchInfo>();  

            //Match changed game methods to HarmonyPatch attributes
            foreach (HarmonyPatchInfo patch in harmonyPatchMethods)
            {
                if (gitChangedMethods.Contains(patch.FullTargetName))
                {
                    matchedPatches.Add(patch);
                }
            }

            return matchedPatches;
        }

        /// <summary>
        /// Invokes git diff to get the game's changes.  Returns the game's changed methods in "Class.Method" format.
        /// </summary>
        /// <param name="gameSourcePath"></param>
        /// <param name="gitExePath"></param>
        /// <param name="commitA"></param>
        /// <param name="commitB"></param>
        /// <returns></returns>
        private static HashSet<string> GetGitToGameMethodChanges(string gameSourcePath, string gitExePath, string commitA, string commitB)
        {
            //Get git diff output and parse to FileDiffs
            string gitDiffOutput = Utils.ExecuteGitCommand(gameSourcePath, gitExePath, commitA, commitB, "--unified=0");
            IEnumerable<FileDiff> diffs = Diff.Parse(gitDiffOutput);

            //Extract changed method/class names from diffs using Roslyn
            var changedGameMethods = new HashSet<string>();
            foreach (var diff in diffs)
            {
                if (!diff.To.EndsWith(".cs")) continue;



                string gameClassPath = Path.Combine(gameSourcePath, diff.To);

                if (!File.Exists(gameClassPath)) continue;
                string sourceText = File.ReadAllText(gameClassPath);
                SyntaxTree tree = CSharpSyntaxTree.ParseText(sourceText);
                SyntaxNode root = tree.GetRoot();


                List<MethodDeclarationSyntax> methods = root.DescendantNodes()
                            .OfType<MethodDeclarationSyntax>().ToList();

                // Find changed lines and map to method/class names
                foreach (var chunk in diff.Chunks)
                {
                    foreach (var lineDiff in chunk.Changes)
                    {
                        var gameMethod = methods.FirstOrDefault(x =>
                        {
                            FileLinePositionSpan methodLineSpan = x.GetLocation().GetLineSpan();

                            return (lineDiff.Index >= methodLineSpan.StartLinePosition.Line + 1 &&
                            lineDiff.Index <= methodLineSpan.EndLinePosition.Line + 1);
                        }
                        );

                        if (gameMethod == null) continue;


                        if (gameMethod.Parent is not ClassDeclarationSyntax classNode) continue;    //This shouldn't happen, but just in case.

                        string methodFullName = String.Join(".", classNode.Identifier.Text, gameMethod.Identifier.Text);

                        changedGameMethods.Add(methodFullName);
                    }
                }




            }

            return changedGameMethods;

        }
    }
}

