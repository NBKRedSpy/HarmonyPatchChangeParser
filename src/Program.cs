using CommandLine;

namespace HarmonyPatchChangeParser
{
    internal class Program
    {

        static int Main(string[] args)
        {
            return new CommandLineProcessor().Execute(args);
        }
    }
}
