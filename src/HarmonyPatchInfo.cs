namespace HarmonyPatchChangeParser
{
    internal sealed class HarmonyPatchInfo
    {
        public string FilePath { get; init; } = "";
        public string AttributeName { get; init; } = "";
        public string AttributeText { get; init; } = "";
        public List<string> PositionalArguments { get; init; } = new();
        public Dictionary<string, string> NamedArguments { get; init; } = new(StringComparer.Ordinal);
        public string TargetType { get; init; } = "";
        public string TargetMember { get; init; } = "";

        public string FullTargetName => $"{TargetType}.{TargetMember}";
    }
}

