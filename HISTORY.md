# PlexCleaner

Utility to optimize media files for Direct Play in Plex, Emby, Jellyfin.

## Release History

- Version 2.9:
  - Added remote docker container debug support.  
    - `develop` tagged docker builds use the `Debug` build target, and will now install the .NET SDK and the [VsDbg](https://aka.ms/getvsdbgsh) .NET Debugger.
    - Added a `--debug` command line option that will wait for a debugger to be attached on launch.
    - Remote debugging in docker over SSH can be done using [VSCode](https://github.com/OmniSharp/omnisharp-vscode/wiki/Attaching-to-remote-processes) or [Visual Studio](https://docs.microsoft.com/en-us/visualstudio/debugger/attach-to-process-running-in-docker-container?view=vs-2022).
  - Updated Dockerfile with latest Linux install steps for MediaInfo and MKVToolNix.
  - Updated System.CommandLine usage to accommodate Beta 4 breaking changes.
- Version 2.8:
  - Added parallel file processing support:
    - Greatly improves throughput on high core count systems, where a single instance of FFmpeg or HandBrake can't utilize all available processing power.
    - Enable parallel processing by using the `--parallel` command line option.
    - The default thread count is equal to half the number of system cores.
    - Override the default thread count by using the `--threadcount` option, e.g. `PlexCleaner --parallel --threadcount 2`.
    - The executing ThreadId is logged to output, this helps with correlating between sequential and logical operations.
    - Interactive console output from tools are disabled when parallel processing is enabled, this avoids console overwrites.
  - General refactoring, bug fixes, and upstream package updates.
- Version 2.7:
  - Log names of all processed files that are in `VerifyFailed` state at the end of the `process` command.
  - Prevent duplicate entries in `ProcessOptions:FileIgnoreList` setting when `VerifyOptions:RegisterInvalidFiles` is set, could happen when using `--reprocess 2`.
  - Added a JSON schema for the configuration file, useful when authoring in tools that honors schemas.
  - Added a "Sandbox" project to simplify code experimentation, e.g. creating a JSON schema from code.
  - Fixed verify and repair logic when `VerifyOptions:AutoRepair` is enabled and file is in `VerifyFailed` state but not `RepairFailed`, could happen when processing is interrupted.
  - Silenced the noisy `tool version mismatch` warnings when `ProcessOptions:SidecarUpdateOnToolChange` is disabled.
  - Replaced `FileEx.IsFileReadWriteable()` with `!FileInfo.IsReadOnly` to optimize for speed over accuracy, testing for attributes vs. opening for write access.
  - Pinned docker base image to `ubuntu:focal` vs. `ubuntu:latest` until Handbrake PPA ads support for Jammy, tracked as [#98](https://github.com/ptr727/PlexCleaner/issues/98).
- Version 2.6:
  - Fixed `SidecarFile.Update()` bug that would not update the sidecar when only the `State` changed, and kept re-verifying the same verified files.
  - Added a `--reprocess` option to the `process` command, `process --reprocess [0 (default), 1, 2]`
    - The `--reprocess` option can be used to override conditional sidecar state optimizations, e.g. don't verify if already verified.
    - 0: Default behavior, do not do any reprocessing.
    - 1: Re-process low cost operations, e.g. tag detection, closed caption detection, etc.
    - 2: Re-process all operations including expensive operations, e.g. deinterlace detection, bitrate calculation, stream verification, etc.
    - Whenever processing logic is updated or improved (e.g. this release), it is recommended to run with `--reprocess 1` at least once.
  - Added workaround for HandBrake that [force converts](https://github.com/HandBrake/HandBrake/issues/160) closed captions and subtitle tracks to `ASS` format.
    - After HandBrake deinterlacing, the original subtitles are added to the output file, bypassing HandBrake subtle logic.
    - Subtitle track formats and attributes are preserved, and closed captions embedded are not converted to subtitle tracks.
    - The HandBrake issue tracked as [#95](https://github.com/ptr727/PlexCleaner/issues/95).
  - Added the removal of [EIA-608](https://en.wikipedia.org/wiki/EIA-608) Closed Captions from video streams.
    - Closed Caption subtitles in video streams are undesired as they cannot be managed, all subtitles should be in discrete tracks.
    - FFprobe [fails](https://www.mail-archive.com/ffmpeg-devel@ffmpeg.org/msg126211.html) to set the `closed_captions` JSON attribute in JSON output mode, but does detect and print `Closed Captions` in normal output mode.
    - FFprobe issue tracked as [#94](https://github.com/ptr727/PlexCleaner/issues/94).
  - Added the ability to bootstrap 7-Zip downloads on Windows, manually downloading `7za.exe` is no longer required.
    - Getting started is now easier, just run:
      - `PlexCleaner.exe --settingsfile PlexCleaner.json defaultsettings`
      - `PlexCleaner.exe --settingsfile PlexCleaner.json checkfornewtools`
  - The `--mediafiles` option no longer supports multiple entries per option, use multiple `--mediafiles` options instead.
    - Deprecation warning initially issued with v2.3.5.
    - Old style: `--mediafiles path1 path2`
    - New style: `--mediafiles path1 --mediafiles path2`
  - Improved the metadata, tag, and attachment detection and cleanup logic.
    - FFprobe container and track tags are now evaluated for unwanted metadata.
    - Attachments are now deleted before processing, eliminating problems with cover art being detected as video tracks, or FFMpeg converting covert art into video tracks.
    - Run with `process --reprocess 1` at least once to re-evaluate conditions.
  - Removed the `upgradesidecar` command.
    - Sidecar schemas are automatically upgraded since v2.5.
  - Removed the `verify` command.
    - Use `process --reprocess 2` instead.
  - Removed the `getbitrateinfo` command.
    - Use `process --reprocess 2` instead.
  - Minor code cleanup and improvements.
- Version 2.5:
  - Changed the config file JSON schema to simplify authoring of multi-value settings, resolves [#85](https://github.com/ptr727/PlexCleaner/issues/85)
    - Older file schemas will automatically be upgraded without requiring user input.
    - Comma separated lists in string format converted to array of strings.
      - Old: `"ReMuxExtensions": ".avi,.m2ts,.ts,.vob,.mp4,.m4v,.asf,.wmv,.dv",`
      - New: `"ReMuxExtensions": [ ".avi", ".m2ts", ".ts", ".vob", ".mp4", ".m4v", ".asf", ".wmv", ".dv" ]`
    - Multiple VideoFormat comma separated lists in strings converted to array of objects.
      - Old:
        - `"ReEncodeVideoFormats": "mpeg2video,mpeg4,msmpeg4v3,msmpeg4v2,vc1,h264,wmv3,msrle,rawvideo,indeo5"`
        - `"ReEncodeVideoCodecs": "*,dx50,div3,mp42,*,*,*,*,*,*"`
        - `"ReEncodeVideoProfiles": "*,*,*,*,*,Constrained Baseline@30,*,*,*,*"`
      - New: `"ReEncodeVideo": [ { "Format": "mpeg2video" }, { "Format": "mpeg4", "Codec": "dx50" }, ... ]`
  - Replaced [GitVersion](https://github.com/GitTools/GitVersion) with [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) as versioning tool, resolves [#16](https://github.com/ptr727/PlexCleaner/issues/16).
    - Main branch will now build using `Release` configuration, other branches will continue building with `Debug` configuration.
    - Prerelease builds are now posted to GitHub releases tagged as `pre-release`, Docker builds continue to be tagged as `develop`.
  - Docker builds are now also pushed to [GitHub Container Registry](https://github.com/ptr727/PlexCleaner/pkgs/container/plexcleaner).
    - Builds will continue to push to Docker Hub while it remains free to use.
  - Added a xUnit unit test project.
    - Currently the only tests are for config and sidecar JSON schema backwards compatibility.
  - Code cleanup and refactoring to make current versions of Visual Studio and Rider happy.
- Version 2.4.5
  - Update FfMpeg in Linux instructions and in Docker builds to version 5.0.
- Version 2.4.3
  - Added more robust error and control logic for handling specific AVI files.
    - Detect and ignore cover art and thumbnail video tracks.
    - Perform conditional interlace detection using FfMpeg idet filter.
    - Verify media tool track identification matches.
    - Modify sidecar file hashing to support small files.
  - Use C# 10 file scoped namespaces.
- Version 2.4.1
  - Added `ProcessOptions:RestoreFileTimestamp` JSON option to restore the media file modified time to match the original value.
  - Fixed media tool logic to account for WMV files with cover art, and added `wmv3` and `wmav2` codecs to be converted.
- Version 2.3.5
  - Deprecation warning for `--mediafiles` option taking multiple paths, instead use multiple invocations.
    - Old style: `--mediafiles path1 path2`
    - New style: `--mediafiles path1 --mediafiles path2`
  - Added `removesubtitles` command to remove all subtitles, useful when the media contains annoying forced subtitles with ads.
- Version 2.3.2
  - Warn when the HDR profile is `Dolby Vision` (profile 5) vs. `Dolby Vision / SMPTE ST 2086` (profile 7).
    - Unless using DV capable hardware, profile 5 may play but will result in funky colors on HDR10 hardware.
    - The warning is only logged during the verify step, repair is not possible.
    - To re-verify existing 4K files use the `verify` command, or reset the state using the `createsidecar` and `process` commands.
  - Renamed `getsidecar` command to `getsidecarinfo` for consistency with other `getxxxinfo` commands.
  - Added `gettoolinfo` command to print media info reported by tools.
  - Refactored duplicate file iteration logic to use lambdas.
- Version 2.3:
  - Migrated from .NET 5 to .NET 6.
- Version 2.1:
  - Added backwards compatibility for some older JSON schemas.
  - Added the `upgradesidecar` command to migrate sidecar files to the current JSON schema version.
  - Sidecar JSON schema changes:
    - Replaced the unreliable file modified timestamp state tracking with a SHA256 hash of parts of the MKV file.
    - Replaced the `Verified` boolean with `State` flags to track more granular file state and modification changes.
    - Run the `upgradesidecar` command to migrate sidecar files to the current schema version.
  - Repairing metadata inconsistencies, e.g. MuxingMode not specified for S_VOBSUB subtitle codecs, by remuxing the MKV file.
  - Added a `ToolsOptions:AutoUpdate` configuration option to automatically update the tools before each run.
- Version 2.0:
  - Linux and Docker are now supported platforms.
    - Automatic downloading of tools on Linux is not currently supported, tools need to be manually installed on the system.
    - The Docker build includes all the prerequisite tools, and is easier to use vs. installing all the tools on Linux.
  - Support for H.265 encoding added.
  - All file metadata, titles, tags, and track names are now deleted during media file cleanup.
  - Windows systems will be kept awake during processing.
  - Schema version numbers were added to JSON config files, breaking backwards compatibility.
    - Sidecar JSON will be invalid and recreated, including re-verifying that can be very time consuming.
    - Tools JSON will be invalid and `checkfortools` should be used to update tools.
  - Tool version numbers are now using the short version number, allowing for Sidecar compatibility between Windows and Linux.
  - Processing of the same media can be mixed between Windows, Linux, and Docker, note that the paths in the `FileIgnoreList` setting are platform specific.
  - New options were added to the JSON config file.
    - `ConvertOptions:EnableH265Encoder`: Enable H.265 encoding vs. H.264.
    - `ToolsOptions:UseSystem`: Use tools from the system path vs. from the Tools folder, this is the default on Linux.
    - `VerifyOptions:RegisterInvalidFiles`: Add files that fail verify and repair to the `ProcessOptions:FileIgnoreList`.
    - `ProcessOptions:ReEncodeAudioFormats` : `opus` codec added to default list.
  - File logging and console output is now done using structured Serilog logging.
    - Basic console and file logging options are used, configuration from JSON is not currently supported.
