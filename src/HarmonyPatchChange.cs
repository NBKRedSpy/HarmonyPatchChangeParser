using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonyPatchChangeParser
{
    /// <summary>
    /// Indicates a single patch in a file.
    /// </summary>
    internal class HarmonyPatchChange
    {
        /// <summary>
        /// The full name to the file for this patch.
        /// </summary>
        public required string FileName { get; set; }

        /// <summary>
        /// The line of code that the harmony patch is from.
        /// </summary>
        public required string HarmonyPatchLine { get; set; }


        /// <summary>
        /// The typeof(foo) value of the patch.  May be empty.
        /// </summary>
        public required string PatchObjectType { get; set; }


        /// <summary>
        /// Indicates if the harmony line was changed, a copy warning, or no change.
        /// </summary>
        public required ChangeType ChangeType { get; set; }  
    }
}



