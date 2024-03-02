﻿using MessagePack;
using Newtonsoft.Json;
using Staple;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Baker;

static partial class Program
{
    private static void ProcessAudio(AppPlatform platform, string inputPath, string outputPath)
    {
        var audioFiles = new List<string>();

        foreach (var extension in AssetSerialization.AudioExtensions)
        {
            try
            {
                audioFiles.AddRange(Directory.GetFiles(inputPath, $"*.{extension}.meta", SearchOption.AllDirectories));
            }
            catch (Exception)
            {
            }
        }

        Console.WriteLine($"Processing {audioFiles.Count} audio files...");

        for (var i = 0; i < audioFiles.Count; i++)
        {
            var audioFileName = audioFiles[i];

            Console.WriteLine($"\t{audioFileName}");

            try
            {
                if (File.Exists(audioFileName) == false)
                {
                    Console.WriteLine($"\t\tError: {audioFileName} doesn't exist");

                    continue;
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"\t\tError: {audioFileName} doesn't exist");

                continue;
            }

            var guid = FindGuid<AudioClip>(audioFileName);

            var directory = Path.GetRelativePath(inputPath, Path.GetDirectoryName(audioFileName));
            var file = Path.GetFileName(audioFileName).Replace(".meta", "");
            var outputFile = Path.Combine(outputPath == "." ? "" : outputPath, directory, file);

            var index = outputFile.IndexOf(inputPath);

            if (index >= 0 && index < outputFile.Length)
            {
                outputFile = outputFile.Substring(0, index) + outputFile.Substring(index + inputPath.Length + 1);
            }

            WorkScheduler.Dispatch(() =>
            {
                Console.WriteLine($"\t\t -> {outputFile}");

                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
                }
                catch (Exception)
                {
                }

                try
                {
                    Directory.CreateDirectory(outputPath);
                }
                catch (Exception)
                {
                }

                try
                {
                    File.Delete(outputFile);
                }
                catch (Exception)
                {
                }

                bool shouldCopy = true;

                try
                {
                    shouldCopy = File.GetLastWriteTime(audioFileName.Replace(".meta", "")) > File.GetLastWriteTime($"{outputFile}.sbin");
                }
                catch (Exception)
                {
                }

                if (shouldCopy)
                {
                    Console.WriteLine($"\t\t\tCopying file as it is newer...");

                    try
                    {
                        File.Copy(audioFileName.Replace(".meta", ""), $"{outputFile}.sbin", true);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"\t\tError: Failed to save asset: {e}");
                    }
                }

                try
                {
                    var json = File.ReadAllText(audioFileName);

                    var metadata = JsonConvert.DeserializeObject<AudioClipMetadata>(json);

                    metadata.guid = guid;

                    var audioClip = new SerializableAudioClip()
                    {
                        metadata = metadata,
                    };

                    var header = new SerializableAudioClipHeader();

                    using (var stream = File.OpenWrite(outputFile))
                    {
                        using (var writer = new BinaryWriter(stream))
                        {
                            var encoded = MessagePackSerializer.Serialize(header)
                                .Concat(MessagePackSerializer.Serialize(audioClip));

                            writer.Write(encoded.ToArray());
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\t\tError: Failed to save audio: {e}");

                    try
                    {
                        File.Delete(outputFile);
                        File.Delete($"{outputFile}.sbin");
                    }
                    catch (Exception)
                    {
                    }
                }
            });
        }
    }
}
