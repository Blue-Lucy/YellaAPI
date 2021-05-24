using CommandLine;
using System;

namespace BlamCli
{
    [Verb("new", HelpText = "Create a new BLAM 3 BLidget")]
    class NewOptions
    {
        [Option('n', "name", Required = true, HelpText = "The name of the new BLidget.")]
        public string Name { get; set; }
    }

    [Verb("publish", HelpText = "Publish the application as a blidget file.")]
    class PublishOptions
    {
        [Option('s', "source",  Required = true, HelpText = "The source folder containing the manifest.xml file.")]
        public string Source { get; set; }

        [Option('d', "destination", Required = true, HelpText = "The destination folder to output the BLidget file to.")]
        public string Destination { get; set; }
    }

    class Program
    {
        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<NewOptions, PublishOptions>(args)
                .MapResult(
                    (NewOptions opts) => ExecuteNew(opts),
                    (PublishOptions opts) => ExecutePublish(opts),
                    errs => 1);
        }

        static int ExecuteNew(NewOptions options)
        {
            Console.WriteLine("New!");
            return 0;
        }

        static int ExecutePublish(PublishOptions options)
        {
            var publisher = new Publisher(options);
            var exportedFilePath = publisher.CreateBlFile();
            Console.WriteLine($"Published to {exportedFilePath}");
            return 0;
        }
    }
}
