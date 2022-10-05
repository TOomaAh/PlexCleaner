using System.ComponentModel.DataAnnotations;

namespace PlexCleaner;

public class ConvertOptions
{
    [Required]
    public bool EnableH265Encoder { get; set; }
    [Required]
    [Range(0, 51)]
    public int VideoEncodeQuality { get; set; }
    [Required]
    public string AudioEncodeCodec { get; set; } = "";
    [Required]
    public bool UseNvidiaGPU { get; set; }

    public void SetDefaults()
    {
        EnableH265Encoder = true;
        VideoEncodeQuality = 20;
        AudioEncodeCodec = "ac3";
        UseNvidiaGPU = false;
    }
}


