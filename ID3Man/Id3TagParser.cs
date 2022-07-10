using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ID3Man
{
    internal class Id3TagParser
    {
        private readonly string _filePath;
        private readonly IDictionary<string, string> _nameKeyMapping = new Dictionary<string, string>
        {
            { "title", "TIT2" },
            { "album", "TALB" },
            { "track", "TRCK" },
            { "performer", "TPE1" },
            { "soft-settings", "TSSE"}
        };

        public Id3TagParser(string filePath)
        {
            _filePath = filePath;
        }

        public string? GetFrameValue(string tagName)
        {
            if (!_nameKeyMapping.TryGetValue(tagName, out var key))
            {
                throw new NotImplementedException($"unsupported tag name {tagName}");
            }

            foreach (var frame in GetFrames())
            {
                if (frame.Key == key)
                {
                    return frame.Value;
                }
            }

            return null;
        }

        public IEnumerable<KeyValuePair<string, string>> GetFrames()
        {
            var tag = GetTagBody(_filePath);


            var i = 0;
            while (i < tag.Length)
            {
                // Frame ID      $xx xx xx xx  (four characters)
                // Size      4 * %0xxxxxxx
                // Flags         $xx xx
                
                if (tag[i] == 0)
                {
                    yield break; // padding started
                }

                var frameId = Encoding.ASCII.GetString(tag, i, 4);
                var frameSizeBytes = tag.Skip(i + 4).Take(4).ToArray();
                var frameSize = ParseSize(frameSizeBytes);
                var frameFlags = tag.Skip(i + 8).Take(2).ToArray();
                if (frameFlags[0] != 0 && frameFlags[1] != 0)
                {
                    throw new NotImplementedException($"frame flags are not supported (frame: {frameId})");
                }

                var frameContent = tag.Skip(i + 10).Take(frameSize).ToArray();
                var encoding = frameContent[0];
                if (encoding == 0x3)
                {
                    // without first (encoding) and last (terminator)
                    var textContent = frameContent.Skip(1).Take(frameSize - 2).ToArray();
                    var str = Encoding.UTF8.GetString(textContent);
                    yield return KeyValuePair.Create(frameId, str);
                }
                else
                {
                    throw new NotImplementedException("unsupported encoding");
                }

                i = i + 10 + frameSize;
            }
        }

        private static byte[] GetTagBody(string filePath)
        {
            // ID3v2/file identifier      "ID3"
            // ID3v2 version              $04 00
            // ID3v2 flags                % abcd0000
            // ID3v2 size             4 * % 0xxxxxxx

            using (var stream = File.OpenRead(filePath))
            {
                var header = new byte[10];
                stream.Read(header, 0, 10);

                var fileId = header.Take(3).ToArray();
                if (fileId[0] != 'I' || fileId[1] != 'D' || fileId[2] != '3')
                {
                    throw new InvalidOperationException("not supported file format");
                }

                var majorVersion = header[3];
                if (majorVersion != 4)
                {
                    throw new NotImplementedException($"unsupported tag version {majorVersion}, only 4 is supported");
                }

                var flags = header[5];
                if ((flags & 0x80) != 0)
                {
                    throw new NotImplementedException("Unsynchronisation");
                }
                if ((flags & 0x40) != 0)
                {
                    throw new NotImplementedException("Extended header");
                }
                if ((flags & 0x20) != 0)
                {
                    throw new NotImplementedException("Experimental indicator");
                }
                if ((flags & 0x10) != 0)
                {
                    throw new NotImplementedException("Footer present");
                }

                var sizeBytes = header.Skip(6).Take(4).ToArray();
                var tagSize = ParseSize(sizeBytes);

                var tagBody = new byte[tagSize];
                stream.Read(tagBody, 0, tagSize);
                return tagBody;
            }
        }

        private static int ParseSize(byte[] array)
        {
            if (array.Length != 4)
            {
                throw new ArgumentException("array must be 4 bytes length");
            }

            var byte1 = new byte[4];
            Array.Copy(array, 0, byte1, 0, 1);
            var i1 = BitConverter.ToUInt32(byte1.Reverse().ToArray());

            var byte2 = new byte[4];
            Array.Copy(array, 1, byte2, 1, 1);
            var i2 = BitConverter.ToUInt32(byte2.Reverse().ToArray());

            var byte3 = new byte[4];
            Array.Copy(array, 2, byte3, 2, 1);
            var i3 = BitConverter.ToUInt32(byte3.Reverse().ToArray());

            var byte4 = new byte[4];
            Array.Copy(array, 3, byte4, 3, 1);
            var i4 = BitConverter.ToUInt32(byte4.Reverse().ToArray());

            var size = i4 + (i3 >> 1) + (i2 >> 2) + (i1 >> 3);
            return (int)size;
        }
    }
}
