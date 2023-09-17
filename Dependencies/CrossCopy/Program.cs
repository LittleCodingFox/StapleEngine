using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace CrossCopy
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var dllExt = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dll" : "so";

            for(var i = 0; i < args.Length; i++)
            {
                args[i] = args[i]
					.Replace("[DLL]", dllExt)
					.Replace('\\', Path.DirectorySeparatorChar)
					.Replace('/', Path.DirectorySeparatorChar);
            }

            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    var process = new Process();

                    var startInfo = new ProcessStartInfo
                    {
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = "cmd.exe",
                        Arguments = $"/C copy /B /Y {string.Join(" ", args)}",
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                    };

                    Console.WriteLine($"{startInfo.FileName} {startInfo.Arguments}");

                    process.StartInfo = startInfo;

                    process.Start();

                    while(process.StandardOutput.EndOfStream == false)
                    {
                        var str = process.StandardOutput.ReadLine();

                        Console.WriteLine(str);
                    }

                    process.WaitForExit();

                    Environment.Exit(process.ExitCode);
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.ToString());

                    Environment.Exit(1);
                }
            }
            else
            {
                try
                {
                    var process = new Process();

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = $"-lc \"cp -Rf {string.Join(" ", args)}\"",
                        RedirectStandardOutput = true,
                    };

                    Console.WriteLine($"{startInfo.FileName} {startInfo.Arguments}");

                    process.StartInfo = startInfo;

                    process.Start();

                    while(process.StandardOutput.EndOfStream == false)
                    {
                        var str = process.StandardOutput.ReadLine();

                        Console.WriteLine(str);
                    }

                    process.WaitForExit();

                    Environment.Exit(process.ExitCode);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());

                    Environment.Exit(1);
                }
            }
        }
    }
}