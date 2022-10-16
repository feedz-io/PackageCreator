using System;
using System.IO;
using Feedz.Util.Processes;
using SharpCompress.Common;
using SharpCompress.Writers.Zip;

namespace Feedz.PackageCreator
{
    public class PackageCreator
    {
        static int Main(string[] args)
        {
            if (args.Length != 3 && args.Length != 4)
            {
                Console.Error.WriteLine("Usage: PackageCreator <name> <version> <sizeKb> [--zip]");
                return 1;
            }

            var name = args[0];
            var version = args[1];
            var size = args[2];
            var zip = args.Length >= 4 && args[3].ToLower() == "--zip";

            if (zip)
                CreateZip(Environment.CurrentDirectory, name, version, long.Parse(size) * 1024);
            else
                CreateNuGet(Environment.CurrentDirectory, name, version, long.Parse(size) * 1024);
            
            return 0;
        }

        public static string CreateZip(string outDir, string name, string version, long size)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                var outfile = Path.Combine(outDir, $"{name}.{version}.zip");

                using (var fs = File.OpenWrite(outfile))
                using (var zip = new ZipWriter(fs, new ZipWriterOptions(CompressionType.Deflate)))
                {
                    var rnd = new Random();

                    void Write(int fileSize)
                    {
                        var contents = new byte[fileSize];
                        while (size > 0)
                        {
                            rnd.NextBytes(contents);
                            using (var s = zip.WriteToStream(Guid.NewGuid().ToString("N"), new ZipWriterEntryOptions()))
                                s.Write(contents, 0, contents.Length);
                            size -= fileSize;
                        }
                    }

                    Write(10_000_000);
                    Write(1_000_000);
                    Write(100_000);
                    Write(10_000);
                    Write(1_000);
                    Write(100);
                    Write(10);
                    Write(1);
                }

                return outfile;
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        public static string CreateNuGet(string outDir, string name, string version, long size)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                return CreateNuGet(tempDir, outDir, name, version, size);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        private static string CreateNuGet(string tempDir, string outDir, string name, string version, long size)
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

            long size1 = size;
            var rnd = new Random();

            void Write(int fileSize)
            {
                var contents = new byte[fileSize];
                while (size1 > fileSize)
                {
                    rnd.NextBytes(contents);
                    File.WriteAllBytes(Path.Combine(tempDir, Guid.NewGuid().ToString("N")), contents);
                    size1 -= fileSize;
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