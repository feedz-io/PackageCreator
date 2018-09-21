using System;
using System.IO;
using Feedz.Util;
using Feedz.Util.Processes;

namespace Feedz.PackageCreator
{
    public class PackageCreator
    {
        static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine("Usage: PackageCreator <name> <version> <sizeKb>");
                return 1;
            }

            var name = args[0];
            var version = args[1];
            var size = args[2];

            Create(Environment.CurrentDirectory, name, version, long.Parse(size));
            return 0;
        }
        
        public static string Create(string outDir, string name, string version, long size)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                return Create(tempDir, outDir, name, version, size * 1024);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        private static string Create(string tempDir, string outDir, string name, string version, long size)
        {
            Console.WriteLine();
            var nuspec = $@"<?xml version=""1.0""?>
                                    <package xmlns=""http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"">
                                        <metadata>
                                            <id>{name}</id>
                                            <version>{version}</version>
        <authors>Someone</authors>
        <owners>Someone</owners>
        <description>Core utility functions for web applications</description>
    </metadata>
    <files>
        <file src=""*"" target="""" />
    </files>
</package>";

            File.WriteAllText(Path.Combine(tempDir, $"{name}.{version}.nuspec"), nuspec);

            var rnd = new Random();

            void Write(int fileSize)
            {
                var contents = new byte[fileSize];
                while (size > fileSize)
                {
                    rnd.NextBytes(contents);
                    File.WriteAllBytes(Path.Combine(tempDir, Guid.NewGuid().ToString("N")), contents);
                    size -= fileSize;
                }
            }

            Write(100_000_000);
            Write(10_000_000);
            Write(1_000_000);
            Write(100_000);
            Write(10_000);
            Write(1_000);
            Write(100);
            Write(10);
            Write(1);

            var nugetExe = Path.Combine(Path.GetTempFileName() + ".exe");

            using (var stream = typeof(PackageCreator).Assembly.GetManifestResourceStream($"{typeof(PackageCreator).Namespace}.nuget.exe"))
            using (var fs = File.OpenWrite(nugetExe))
                stream.CopyTo(fs);

            try
            {
                if (outDir.EndsWith("\\"))
                    outDir += "\\";

                var command = $"pack -OutputDirectory \"{outDir}\"";
                var result = ProcessRunner.Execute(nugetExe, command, workingDirectory: tempDir);
                if (result.ExitCode != 0)
                    throw new Exception("Failed to create package: " + result.Error);

                return Path.Combine(outDir, $"{name}.{version}.nupkg");
            }
            finally
            {
                File.Delete(nugetExe);
            }
        }

    }
}