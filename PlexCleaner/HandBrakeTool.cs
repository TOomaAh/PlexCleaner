﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using InsaneGenius.Utilities;
using Newtonsoft.Json.Linq;
using Serilog;

// https://handbrake.fr/docs/en/latest/cli/command-line-reference.html

namespace PlexCleaner;

public class HandBrakeTool : MediaTool
{
    public override ToolFamily GetToolFamily()
    {
        return ToolFamily.HandBrake;
    }

    public override ToolType GetToolType()
    {
        return ToolType.HandBrake;
    }

    protected override string GetToolNameWindows()
    {
        return "HandBrakeCLI.exe";
    }

    protected override string GetToolNameLinux()
    {
        return "HandBrakeCLI";
    }

    public override bool GetInstalledVersion(out MediaToolInfo mediaToolInfo)
    {
        // Initialize            
        mediaToolInfo = new MediaToolInfo(this);

        // Get version
        const string commandline = "--version";
        int exitCode = Command(commandline, out string output);
        if (exitCode != 0)
        {
            return false;
        }

        // First line as version
        // E.g. Windows : "HandBrake 1.3.3"
        // E.g. Linux : "HandBrake 1.3.3"
        string[] lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        // Extract the short version number
        const string pattern = @"HandBrake\ (?<version>.*)";
        Regex regex = new(pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
        Match match = regex.Match(lines[0]);
        Debug.Assert(match.Success);
        mediaToolInfo.Version = match.Groups["version"].Value;

        // Get tool filename
        mediaToolInfo.FileName = GetToolPath();

        // Get other attributes if we can read the file
        if (File.Exists(mediaToolInfo.FileName))
        {
            FileInfo fileInfo = new(mediaToolInfo.FileName);
            mediaToolInfo.ModifiedTime = fileInfo.LastWriteTimeUtc;
            mediaToolInfo.Size = fileInfo.Length;
        }

        return true;
    }

    protected override bool GetLatestVersionWindows(out MediaToolInfo mediaToolInfo)
    {
        // Initialize            
        mediaToolInfo = new MediaToolInfo(this);

        try
        {
            // Get the latest release version number from github releases
            // https://api.github.com/repos/handbrake/handbrake/releases/latest
            if (!Download.DownloadString(new Uri(@"https://api.github.com/repos/handbrake/handbrake/releases/latest"), out string json))
            {
                return false;
            }

            JObject releases = JObject.Parse(json);
            // "tag_name": "1.2.2",
            JToken versiontag = releases["tag_name"];
            Debug.Assert(versiontag != null);
            mediaToolInfo.Version = versiontag.ToString();

            // Create download URL and the output filename using the version number
            // https://github.com/HandBrake/HandBrake/releases/download/1.3.2/HandBrakeCLI-1.3.2-win-x86_64.zip
            mediaToolInfo.FileName = $"HandBrakeCLI-{mediaToolInfo.Version}-win-x86_64.zip";
            mediaToolInfo.Url = $"https://github.com/HandBrake/HandBrake/releases/download/{mediaToolInfo.Version}/{mediaToolInfo.FileName}";
        }
        catch (Exception e) when (Log.Logger.LogAndHandle(e, MethodBase.GetCurrentMethod()?.Name))
        {
            return false;
        }
        return true;
    }

    protected override bool GetLatestVersionLinux(out MediaToolInfo mediaToolInfo)
    {
        // Initialize            
        mediaToolInfo = new MediaToolInfo(this);

        // TODO
        return false;
    }

    public bool ConvertToMkv(string inputName, string videoCodec, int videoQuality, string audioCodec, string outputName)
    {
        // Delete output file
        FileEx.DeleteFile(outputName);

        // Build commandline
        StringBuilder commandline = new();
        DefaultArgs(inputName, outputName, commandline);
        commandline.Append($"--encoder {videoCodec} --encoder-preset medium --quality {videoQuality} ");
        commandline.Append($"--all-subtitles --all-audio --aencoder {audioCodec}");

        // Encode audio and video, copy subtitles
        int exitCode = Command(commandline.ToString());
        return exitCode == 0;
    }

    public bool ConvertToMkv(string inputName, string videoCodec, int videoQuality, string outputName)
    {
        // Delete output file
        FileEx.DeleteFile(outputName);

        // Build commandline
        StringBuilder commandline = new();
        DefaultArgs(inputName, outputName, commandline);
        commandline.Append($"--encoder {videoCodec} --encoder-preset medium --quality {videoQuality} ");
        commandline.Append($"--all-subtitles --all-audio --aencoder copy --audio-fallback {Program.Config.ConvertOptions.AudioEncodeCodec}");

        // Encode video, copy audio and subtitles
        int exitCode = Command(commandline.ToString());
        return exitCode == 0;
    }

    public bool ConvertToMkv(string inputName, string outputName)
    {
        // Use defaults
        return ConvertToMkv(inputName,
            GetVideoEncoder(),
            Program.Config.ConvertOptions.VideoEncodeQuality,
            Program.Config.ConvertOptions.AudioEncodeCodec,
            outputName);
    }

    public bool DeInterlaceToMkv(string inputName, string videoCodec, int videoQuality, string outputName, bool includeSubtitles = true)
    {
        // Delete output file
        FileEx.DeleteFile(outputName);

        // Build commandline
        StringBuilder commandline = new();
        DefaultArgs(inputName, outputName, commandline);
        commandline.Append($"--encoder {videoCodec} --encoder-preset medium --quality {videoQuality} ");
        commandline.Append("--comb-detect --decomb ");
        commandline.Append(includeSubtitles ? "--all-subtitles " : "--subtitle none ");
        commandline.Append($"--all-audio --aencoder copy --audio-fallback {Program.Config.ConvertOptions.AudioEncodeCodec}");

        // Encode and decomb video, copy audio, and conditionally copy subtitles
        int exitCode = Command(commandline.ToString());
        return exitCode == 0;
    }

    public bool DeInterlaceToMkv(string inputName, string outputName, bool includeSubtitles = true)
    {
        // Use defaults
        return DeInterlaceToMkv(inputName,
            GetVideoEncoder(),
            Program.Config.ConvertOptions.VideoEncodeQuality,
            outputName,
            includeSubtitles);
    }

    private static string GetVideoEncoder()
    {
        if (Program.Config.ConvertOptions.UseNvidiaGPU)
        {
            return Program.Config.ConvertOptions.EnableH265Encoder ? H265CodecNvenc : H264CodecNvenc;
        }
        else
        {
            return Program.Config.ConvertOptions.EnableH265Encoder ? H265Codec : H264Codec;
        }
    }

    private static void DefaultArgs(string inputName, string outputName, StringBuilder commandline)
    {
        // TODO: How to suppress console output?
        // if (Program.Options.Parallel)
        commandline.Append($"--input \"{inputName}\" ");
        if (Program.Options.TestSnippets)
        {
            commandline.Append($"{Snippet} ");
        }
        commandline.Append($"--output \"{outputName}\" ");
        commandline.Append("--format av_mkv ");
    }

    private const string H264Codec = "x264";
    private const string H264CodecNvenc = "nvenc_h264";
    private const string H265Codec = "x265";
    private const string H265CodecNvenc = "nvenc_h265";
    private const string Snippet = "--start-at seconds:00 --stop-at seconds:180";
}
