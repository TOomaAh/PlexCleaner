﻿using System.Collections.Generic;
using Newtonsoft.Json;

// Convert JSON file to C# using quicktype.io in VSCode https://marketplace.visualstudio.com/items?itemName=typeguard.quicktype-vs
// TODO: Find JSON schema definition
// https://stackoverflow.com/questions/61398647/where-can-i-get-the-ffprobe-json-schema-definition

// Use ffprobe example output:
// ffprobe -loglevel quiet -show_streams -print_format json file.mkv

// Convert array[] to List<>
// Remove per item NullValueHandling = NullValueHandling.Ignore and add to Converter settings

// No JSON schema, but XML schema
// https://github.com/FFmpeg/FFmpeg/blob/master/doc/ffprobe.xsd

// ReSharper disable once CheckNamespace
namespace PlexCleaner.FfMpegToolJsonSchema;

public class FfProbe
{
    [JsonProperty("streams")]
    public List<Stream> Streams { get; } = new();

    [JsonProperty("format")]
    public Format Format { get; } = new();

    public static FfProbe FromJson(string json)
    {
        return JsonConvert.DeserializeObject<FfProbe>(json, Settings);
    }

    private static readonly JsonSerializerSettings Settings = new()
    {
        Formatting = Formatting.Indented
    };
}

public class Format
{
    [JsonProperty("format_name")]
    public string FormatName { get; set; } = "";

    [JsonProperty("duration")]
    public double Duration { get; set; }

    [JsonProperty("tags")]
    public Dictionary<string, string> Tags { get; } = new();
}

public class Stream
{
    [JsonProperty("index")]
    public int Index { get; set; }

    [JsonProperty("codec_name")]
    public string CodecName { get; set; } = "";

    [JsonProperty("codec_long_name")]
    public string CodecLongName { get; set; } = "";

    [JsonProperty("profile")]
    public string Profile { get; set; } = "";

    [JsonProperty("codec_type")]
    public string CodecType { get; set; } = "";

    [JsonProperty("codec_tag_string")]
    public string CodecTagString { get; set; } = "";

    [JsonProperty("level")]
    public string Level { get; set; } = "";

    [JsonProperty("field_order")]
    public string FieldOrder { get; set; } = "";

    [JsonProperty("closed_captions")]
    public bool ClosedCaptions { get; set; }

    [JsonProperty("disposition")]
    public Disposition Disposition { get; } = new();

    [JsonProperty("tags")]
    public Dictionary<string, string> Tags { get; } = new();
}

public class Disposition
{
    [JsonProperty("default")]
    public bool Default { get; set; }
    [JsonProperty("forced")]
    public bool Forced { get; set; }
}

public class PacketInfo
{
    [JsonProperty("packets")]
    public List<Packet> Packets { get; } = new();
}

public class Packet
{
    [JsonProperty("codec_type")]
    public string CodecType { get; set; } = "";

    [JsonProperty("stream_index")]
    public long StreamIndex { get; set; } = -1;

    [JsonProperty("pts_time")]
    public double PtsTime { get; set; } = double.NaN;

    [JsonProperty("dts_time")]
    public double DtsTime { get; set; } = double.NaN;

    [JsonProperty("duration_time")]
    public double DurationTime { get; set; } = double.NaN;

    [JsonProperty("size")]
    public long Size { get; set; } = -1;
}
