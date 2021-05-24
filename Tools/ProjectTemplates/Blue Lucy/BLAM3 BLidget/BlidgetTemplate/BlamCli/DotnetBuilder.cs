using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace BlamCli
{
    class DotnetBuilder
    {
        private readonly string _projectPath;
        private const string outputFolderName = "blamcli_publish";

        public DotnetBuilder(string projectPath)
        {
            _projectPath = projectPath.TrimEnd('\\');
        }

        public string Build()
        {
            ClearBuildArtifacts();

            var start = new ProcessStartInfo
            {
                WorkingDirectory = _projectPath,
                FileName = "dotnet",
                Arguments = $"publish \"{_projectPath}\" -c Release -o \"{outputFolderName}\"",
                CreateNoWindow = false
            };
            using (var process = Process.Start(start))
            {
                process.WaitForExit();
                if (process.ExitCode != 0) { throw new Exception("Build failed"); }
            }

            return Path.Combine(_projectPath, outputFolderName);
        }

        public void ClearBuildArtifacts()
        {
            var binDirectory = Path.Combine(_projectPath, "bin");
            var objDirectory = Path.Combine(_projectPath, "obj");

            if (Directory.Exists(binDirectory))
                Directory.Delete(binDirectory, true);

            if (Directory.Exists(objDirectory))
                Directory.Delete(objDirectory, true);
        }
    }
}
