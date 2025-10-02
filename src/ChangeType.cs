namespace HarmonyPatchChangeParser
{
    /// <summary>
    /// The type of Harmony patch change detected.
    /// </summary>
    /// <remarks>This is order by preferred output order.</remarks>
    internal enum ChangeType
    {
        Invalid = 0,

        /// <summary>
        /// A more detailed change detection which uses a C# parser to map game changes to mod HarmonyPatch attribute classes.
        /// </summary>
        HarmonyPatchParsedMatch,

        /// <summary>
        /// A simple mapping which searches for the text "Harmony" in the mod files and the file names in the game that were changed.
        /// Assumes the game class name is the same as the file name.
        /// </summary>
        HarmonyPatchTextMatchChange,

        /// <summary>
        /// A warning of a possible copy or replace of an existing function.
        /// Overly broad as it currently only looks for the text copy in the mod files. 
        /// </summary>
        CopyWarning,

        /// <summary>
        /// The patch had no changes.  Used for informational purposes.
        /// </summary>
        None
    }

}



