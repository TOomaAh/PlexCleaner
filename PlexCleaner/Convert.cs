﻿using InsaneGenius.Utilities;
using System;
using System.Diagnostics;
using System.IO;

namespace PlexCleaner;

public static class Convert
{
    public static bool ConvertToMkv(string inputName, out string outputName)
    {
        // Convert all tracks
        return ConvertToMkv(inputName, null, null, out outputName);
    }

    public static bool ConvertToMkv(string inputName, MediaInfo keep, MediaInfo reencode, out string outputName)
    {
        return ConvertToMkvFfMpeg(inputName, keep, reencode, out outputName);
    }

    public static bool ConvertToMkvFfMpeg(string inputName, out string outputName)
    {
        // Convert all tracks
        return ConvertToMkvFfMpeg(inputName, null, null, out outputName);
    }

    public static bool ConvertToMkvFfMpeg(string inputName, MediaInfo keep, MediaInfo reencode, out string outputName)
    {
        if (inputName == null)
        {
            throw new ArgumentNullException(nameof(inputName));
        }

        // Match the logic in ReMuxToMKV()

        // Test
        if (Program.Options.TestNoModify)
        {
            outputName = inputName;
            return true;
        }

        // Create a temp filename based on the input name
        outputName = Path.ChangeExtension(inputName, ".mkv");
        string tempName = Path.ChangeExtension(inputName, ".tmp");

        // Convert using ffmpeg
        if (!Tools.FfMpeg.ConvertToMkv(inputName, keep, reencode, tempName))
        {
            FileEx.DeleteFile(tempName);
            return false;
        }

        // Rename the temp file to the output file
        if (!FileEx.RenameFile(tempName, outputName))
        {
            return false;
        }

        // If the input and output names are not the same, delete the input
        return inputName.Equals(outputName, StringComparison.OrdinalIgnoreCase) ||
               FileEx.DeleteFile(inputName);
    }

    public static bool ReMuxToMkv(string inputName, out string outputName)
    {
        if (inputName == null)
        {
            throw new ArgumentNullException(nameof(inputName));
        }

        // Match the logic in ConvertToMKV()

        // Test
        if (Program.Options.TestNoModify)
        {
            outputName = inputName;
            return true;
        }

        // Create a MKV and temp filename based on the input name
        outputName = Path.ChangeExtension(inputName, ".mkv");
        string tempName = Path.ChangeExtension(inputName, ".tmp");

        // MkvMerge and FfMpeg both have problems dealing with some AVI files, so we will try both
        // E.g. https://github.com/FFmpeg/FFmpeg/commit/8de1ee9f725aa3c550f425bd3120bcd95d5b2ea8
        // E.g. https://github.com/mbunkus/mkvtoolnix/issues/2123

        // Try MKV first
        if (!Tools.MkvMerge.ReMuxToMkv(inputName, tempName))
        {
            // Failed, delete temp file
            FileEx.DeleteFile(tempName);

            // Cancel requested
            if (Program.IsCancelledError())
            {
                return false;
            }

            // Retry using FfMpeg
            if (!Tools.FfMpeg.ReMuxToMkv(inputName, tempName))
            {
                // Failed, delete temp file
                FileEx.DeleteFile(tempName);

                // Error
                return false;
            }
        }

        // Rename the temp file to the output file
        if (!FileEx.RenameFile(tempName, outputName))
        {
            return false;
        }

        // If the input and output names are not the same, delete the input
        return inputName.Equals(outputName, StringComparison.OrdinalIgnoreCase) ||
               FileEx.DeleteFile(inputName);
    }

    public static bool ReMuxToMkv(string inputName, MediaInfo keep, out string outputName)
    {
        if (inputName == null)
        {
            throw new ArgumentNullException(nameof(inputName));
        }

        if (keep == null)
        {
            throw new ArgumentNullException(nameof(keep));
        }

        // This only works on MKV files and MkvMerge MediaInfo types
        Debug.Assert(keep.Parser == MediaTool.ToolType.MkvMerge);
        Debug.Assert(MkvMergeTool.IsMkvFile(inputName));

        // Match the logic in ConvertToMKV()

        // Test
        if (Program.Options.TestNoModify)
        {
            outputName = inputName;
            return true;
        }

        // Create a temp filename based on the input name
        outputName = Path.ChangeExtension(inputName, ".mkv");
        string tempName = Path.ChangeExtension(inputName, ".tmp");

        // Remux keeping specific tracks
        if (!Tools.MkvMerge.ReMuxToMkv(inputName, keep, tempName))
        {
            FileEx.DeleteFile(tempName);
            return false;
        }

        // Rename the temp file to the output file
        if (!FileEx.RenameFile(tempName, outputName))
        {
            return false;
        }

        // If the input and output names are not the same, delete the input
        return inputName.Equals(outputName, StringComparison.OrdinalIgnoreCase) ||
               FileEx.DeleteFile(inputName);
    }

    public static bool DeInterlaceToMkv(string inputName, out string outputName)
    {
        // HandBrake produces the best deinterlacing results
        return DeInterlaceToMkvHandbrake(inputName, out outputName);
    }

    public static bool DeInterlaceToMkvHandbrake(string inputName, out string outputName)
    {
        if (inputName == null)
        {
            throw new ArgumentNullException(nameof(inputName));
        }

        // Match the logic in ConvertToMKV()

        // Test
        if (Program.Options.TestNoModify)
        {
            outputName = inputName;
            return true;
        }

        // Create a temp filename based on the input name
        outputName = Path.ChangeExtension(inputName, ".mkv");
        string tempName = Path.ChangeExtension(inputName, ".tmp");

        // Deinterlace video using handbrake
        if (!Tools.HandBrake.DeInterlaceToMkv(inputName, tempName))
        {
            FileEx.DeleteFile(tempName);
            return false;
        }

        // Rename the temp file to the output file
        if (!FileEx.RenameFile(tempName, outputName))
        {
            return false;
        }

        // If the input and output names are not the same, delete the input
        return inputName.Equals(outputName, StringComparison.OrdinalIgnoreCase) ||
               FileEx.DeleteFile(inputName);
    }

    public static bool ConvertToMkvHandBrake(string inputName, out string outputName)
    {
        if (inputName == null)
        {
            throw new ArgumentNullException(nameof(inputName));
        }

        // Match the logic in ConvertToMKV()

        // Test
        if (Program.Options.TestNoModify)
        {
            outputName = inputName;
            return true;
        }

        // Create a temp filename based on the input name
        outputName = Path.ChangeExtension(inputName, ".mkv");
        string tempName = Path.ChangeExtension(inputName, ".tmp");

        // Re-encode audio and video using handbrake
        if (!Tools.HandBrake.ConvertToMkv(inputName, tempName))
        {
            FileEx.DeleteFile(tempName);
            return false;
        }

        // Rename the temp file to the output file
        if (!FileEx.RenameFile(tempName, outputName))
        {
            return false;
        }

        // If the input and output names are not the same, delete the input
        return inputName.Equals(outputName, StringComparison.OrdinalIgnoreCase) ||
               FileEx.DeleteFile(inputName);
    }
}