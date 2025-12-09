using System;
using System.Drawing;
using System.IO;

namespace Clickboard
{
    [Serializable]
    public class ClipboardEntry
    {
        public string DisplayName { get; set; }
        public string TextValue { get; set; }
        public byte[] ImageData { get; set; }
        public string ImageFormat { get; set; }

        // Audio support
        public byte[] AudioData { get; set; }
        public string AudioFormat { get; set; } // ".mp3", ".wav", ".ogg"
        public bool IsAudio => AudioData != null && AudioData.Length > 0 && !string.IsNullOrEmpty(AudioFormat);

        public bool IsImage => ImageData != null && ImageData.Length > 0;

        public Image GetImage()
        {
            if (!IsImage) return null;
            using (var ms = new MemoryStream(ImageData))
            {
                // For GIF, return as Image but note animation is not preserved in preview
                return Image.FromStream(ms);
            }
        }
        // Helper for GIF raw stream if needed
        public Stream GetImageStream()
        {
            if (!IsImage) return null;
            return new MemoryStream(ImageData);
        }
    }
}