using CommandLine;

namespace HarmonyPatchChangeParser
{
    /// <summary>
    /// Handles processing the command line arguments.
    /// </summary>
    internal class CommandLineProcessor
    {
        public CommandLineProcessor()
        {
        }

        public int Execute(string[] args)
        {
            CommandLineOptions? parsedOptions = null;

            try
            {

                ParserResult<CommandLineOptions>? parseResult = Parser.Default.ParseArguments<CommandLineOptions>(args)
                    .WithParsed<CommandLineOptions>(options =>
                    {
                        parsedOptions = options;

                        var processor = new HarmonySourceProcessor();

                        string tsv = processor.Process(options, out string gameFileChanges);

                        OutputData(options.HarmonyReportFilePath, tsv);
                        OutputData(options.GameFileChanges, gameFileChanges);

                    });

                return parseResult.Errors.Any() ? 1 : 0;

            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred:");
                Console.WriteLine(ex);
                return 1;
            }


        }

        /// <summary>
        /// Outputs the data based on the option.  '' means do not process, '-' means output to console,
        /// otherwise it writes to the file.
        /// </summary>
        /// <param name="filePathInfo"></param>
        /// <param name="text"></param>
        private static void OutputData(string filePathInfo, string text)
        {
            if (!string.IsNullOrEmpty(filePathInfo))
            {
                if (filePathInfo == "-")
                {
                    Console.WriteLine(text);
                }
                else
                {
                    File.WriteAllText(filePathInfo, text);
                }
            }
        }
    }
}