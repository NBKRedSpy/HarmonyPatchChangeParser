namespace HarmonyPatchChangeParser
{
    internal static class Extensions {

        internal static int SortOrder(this ChangeType change)
        {
            return change switch
            {
                ChangeType.HarmonyPatchChange => 0,
                ChangeType.CopyWarning => 1,
                ChangeType.None => 1,
                _ => 3, // Invalid or unknown
            };
        }
    }

}



