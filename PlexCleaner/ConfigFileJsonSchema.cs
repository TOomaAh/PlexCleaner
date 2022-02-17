﻿using Newtonsoft.Json;
using System.IO;
using System.ComponentModel;
using System;

namespace PlexCleaner;

public class ConfigFileJsonSchemaBase
{
    // Default to 0 if no value specified, and always write the version first
    [DefaultValue(0)]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate, Order = -2)]
    public int SchemaVersion { get; set; } = ConfigFileJsonSchema.Version;
}

[Obsolete("Replaced in Schema v2", false)]
public class ConfigFileJsonSchema1 : ConfigFileJsonSchemaBase
{
    public ToolsOptions ToolsOptions { get; set; } = new();
    public ConvertOptions ConvertOptions { get; set; } = new();
    public ProcessOptions1 ProcessOptions { get; set; } = new();
    public MonitorOptions MonitorOptions { get; set; } = new();
    public VerifyOptions VerifyOptions { get; set; } = new();

    public const int Version = 1;
}

public class ConfigFileJsonSchema : ConfigFileJsonSchemaBase
{
    public ConfigFileJsonSchema() { }

#pragma warning disable CS0618 // Type or member is obsolete
    public ConfigFileJsonSchema(ConfigFileJsonSchema1 configFileJsonSchema1)
#pragma warning restore CS0618 // Type or member is obsolete
    {
        // Keep the original schema version
        SchemaVersion = configFileJsonSchema1.SchemaVersion;

        // Assign same values
        ToolsOptions = configFileJsonSchema1.ToolsOptions;
        ConvertOptions = configFileJsonSchema1.ConvertOptions;
        MonitorOptions = configFileJsonSchema1.MonitorOptions;
        VerifyOptions = configFileJsonSchema1.VerifyOptions;

        // Create current version from old version
        ProcessOptions = new(configFileJsonSchema1.ProcessOptions);
    }

    public ToolsOptions ToolsOptions { get; set; } = new();
    public ConvertOptions ConvertOptions { get; set; } = new();
    public ProcessOptions ProcessOptions { get; set; } = new();
    public MonitorOptions MonitorOptions { get; set; } = new();
    public VerifyOptions VerifyOptions { get; set; } = new();

    public const int Version = 2;

    public static void WriteDefaultsToFile(string path)
    {
        ConfigFileJsonSchema config = new();
        ToFile(path, config);
    }

    public static ConfigFileJsonSchema FromFile(string path)
    {
        return FromJson(File.ReadAllText(path));
    }

    public static void ToFile(string path, ConfigFileJsonSchema json)
    {
        // Set the schema to the current version
        json.SchemaVersion = ConfigFileJsonSchema.Version;

        // Write JSON to file
        File.WriteAllText(path, ToJson(json));
    }

    public static string ToJson(ConfigFileJsonSchema settings) =>
        JsonConvert.SerializeObject(settings, Settings);

    public static ConfigFileJsonSchema FromJson(string json)
    {
        // Deserialize the base class to get the schema version
        int schemaVersion = JsonConvert.DeserializeObject<ConfigFileJsonSchemaBase>(json, Settings).SchemaVersion;

        // Deserialize the correct version
        switch (schemaVersion)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            // Version 1
            case ConfigFileJsonSchema1.Version:
                // Create current version from old version
                return new ConfigFileJsonSchema(JsonConvert.DeserializeObject<ConfigFileJsonSchema1>(json, Settings));
#pragma warning restore CS0618 // Type or member is obsolete
            // Current version
            case ConfigFileJsonSchema.Version:
                return JsonConvert.DeserializeObject<ConfigFileJsonSchema>(json, Settings);
            // Unknown version
            default:
                throw new NotSupportedException(nameof(schemaVersion));
        }
    }

    private static readonly JsonSerializerSettings Settings = new()
    {
        Formatting = Formatting.Indented,
        StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
        NullValueHandling = NullValueHandling.Ignore
    };
}