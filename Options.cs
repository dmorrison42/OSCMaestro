using CommandLine;

public class Options
{
    [Option('s', "skip-wing", Required = false, HelpText = "Skips connecting to the wing")]
    public bool SkipWing { get; set; }
    [Option('v', "verbose", Required = false, HelpText = "Outputs additional log messages")]
    public bool Verbose { get; set; }
    [Option("verbose-wing", Required = false, HelpText = "Outputs additional log messages (+wing messages)")]
    public bool VerboseWing { get; set; }
}