using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ID3Man
{
    internal class TagManager
    {
        private readonly string _filePath;
        private readonly IDictionary<string, string> _nameIdMapping = new Dictionary<string, string>
        {
            { "title", "TIT2" },
            { "album", "TALB" },
            { "track", "TRCK" },
            { "performer", "TPE1" },
            { "soft-settings", "TSSE"}
        };

        public TagManager(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException($"'{nameof(filePath)}' cannot be null or empty.", nameof(filePath));
            }

            _filePath = filePath;
        }

        public string? GetFrameValue(string frameName)
        {
            if (string.IsNullOrEmpty(frameName))
            {
                throw new ArgumentException($"'{nameof(frameName)}' cannot be null or empty.", nameof(frameName));
            }

            if (!_nameIdMapping.TryGetValue(frameName, out var id))
            {
                throw new NotImplementedException($"unsupported frame name {frameName}");
            }

            if (!GetFrames().TryGetValue(id, out var value))
            {
                return null;
            }

            return value;
        }

        public IReadOnlyDictionary<string, string> GetFrames()
        {
            var tag = Tag.GetFromFile(_filePath);
            return tag.Frames.ToDictionary(f => f.Key, f => f.Value);
        }

        public void SetFrameValue(string frameName,
                                  string value,
                                  string outputFilePath)
        {
            if (string.IsNullOrEmpty(frameName))
            {
                throw new ArgumentException($"'{nameof(frameName)}' cannot be null or empty.", nameof(frameName));
            }
            if (string.IsNullOrEmpty(outputFilePath))
            {
                throw new ArgumentException($"'{nameof(outputFilePath)}' cannot be null or empty.", nameof(outputFilePath));
            }

            if (!_nameIdMapping.TryGetValue(frameName, out var id))
            {
                throw new NotImplementedException($"unsupported tag name {frameName}");
            }

            var tag = Tag.GetFromFile(_filePath);
            var inTagSize = tag.Serialize().Length;

            tag.Frames[id] = value;
            var outTagRaw = tag.Serialize();

            using (var outFile = File.OpenWrite(outputFilePath))
            using (var inFile = File.OpenRead(_filePath))
            {
                inFile.Position = inTagSize; // skip old tag

                outFile.Write(outTagRaw, 0, outTagRaw.Length);
                inFile.CopyTo(outFile);
            }
        }
    }
}
