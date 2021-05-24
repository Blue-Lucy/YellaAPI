using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace BlamCli
{
    class Publisher
    {
        private readonly PublishOptions _options;

        public Publisher(PublishOptions options)
        {
            _options = options;
        }

        public string CreateBlFile()
        {
            var zipFileName = Path.GetFileName(Path.GetDirectoryName(Path.GetFullPath(_options.Source)));

            if (!ManifestExists(_options.Source)) { throw new Exception($"No manifest found in source folder '{Path.GetFullPath(_options.Source)}'"); }

            var builder = new DotnetBuilder(Path.GetFullPath(_options.Source));
            var builtSource = builder.Build();

            if (!ManifestExists(builtSource))
            {
                throw new Exception($"No manifest found in publish folder '{Path.GetFullPath(builtSource)}' (ensure that manifest.xml is set to be copied to output folder)");
            }
            
            var dest = Path.Combine(_options.Destination, zipFileName + ".bl");
            DeleteIfExists(dest);

            ZipFile.CreateFromDirectory(builtSource, dest);

            Directory.Delete(builtSource, true);

            return Path.GetFullPath(dest);
        }

        private static bool ManifestExists(string sourceFolder) => File.Exists(Path.Combine(sourceFolder, "manifest.xml"));

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) { File.Delete(path); }
        }
    }
}
