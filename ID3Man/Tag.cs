using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ID3Man
{
    internal class Tag
    {
        public static Tag GetFromFile(string filePath)
        {
            // ID3v2/file identifier      "ID3"
            // ID3v2 version              $04 00
            // ID3v2 flags                % abcd0000
            // ID3v2 size             4 * % 0xxxxxxx

            var header = new byte[10];
            byte[] tagBody;
            using (var stream = File.OpenRead(filePath))
            {
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
                var integer = new SynchsafeInteger(sizeBytes);
                var tagSize = integer.ToInt();

                tagBody = new byte[tagSize];
                stream.Read(tagBody, 0, tagSize);
            }
            var tagRaw = header.Concat(tagBody).ToArray();

            return Deserialize(tagRaw);
        }

        public static Tag Deserialize(byte[] tagRaw)
        {
            var tag = new Tag();
            var tagBodyRaw = tagRaw.Skip(10).ToArray();
            foreach (var frame in DeserializeFrames(tagBodyRaw))
            {
                tag.Frames.Add(frame);
            }
            return tag;
        }

        private static IReadOnlyDictionary<string, string> DeserializeFrames(byte[] tagBody)
        {
            var frames = new Dictionary<string, string>();
            var i = 0;
            while (i < tagBody.Length)
            {
                // Frame ID      $xx xx xx xx  (four characters)
                // Size      4 * %0xxxxxxx
                // Flags         $xx xx
                
                if (tagBody[i] == 0)
                {
                    break; // padding started
                }

                var frameId = Encoding.ASCII.GetString(tagBody, i, 4);
                var frameSizeBytes = tagBody.Skip(i + 4).Take(4).ToArray();
                var frameSize = new SynchsafeInteger(frameSizeBytes).ToInt();
                var frameFlags = tagBody.Skip(i + 8).Take(2).ToArray();
                if (frameFlags[0] != 0 && frameFlags[1] != 0)
                {
                    throw new NotImplementedException($"frame flags are not supported (frame: {frameId})");
                }

                var frameContent = tagBody.Skip(i + 10).Take(frameSize).ToArray();
                var encoding = frameContent[0];
                if (encoding == 0x3)
                {
                    // without first (encoding) and last (terminator)
                    var textContent = frameContent.Skip(1).Take(frameSize - 2).ToArray();
                    var str = Encoding.UTF8.GetString(textContent);
                    frames[frameId] = str;
                }
                else
                {
                    throw new NotImplementedException("unsupported encoding");
                }

                i = i + 10 + frameSize;
            }

            return frames;
        }

        public IDictionary<string, string> Frames { get; } = new Dictionary<string, string>();

        public static byte[] SerializeFrames(IEnumerable<KeyValuePair<string, string>> frames)
        {
            var framesRaw = new List<byte>();
            foreach (var frame in frames)
            {
                // ID - 4 bytes
                var frameRaw = new List<byte>();
                var frameId = Encoding.ASCII.GetBytes(frame.Key);
                if (frameId.Length != 4)
                {
                    throw new InvalidOperationException($"invalid frameID: {frameId}");
                }
                frameRaw.AddRange(frameId);
                
                // size - 4 bytes
                var content = Encoding.UTF8.GetBytes(frame.Value);
                var frameSize = content.Length + 2;
                var frameSizeBytes = new SynchsafeInteger(frameSize).ToArray();
                frameRaw.AddRange(frameSizeBytes);

                // flags - 2 bytes
                frameRaw.Add(0);
                frameRaw.Add(0);

                // content
                frameRaw.Add(3); // utf-8 mark
                frameRaw.AddRange(content);
                frameRaw.Add(0); // null-terminator

                framesRaw.AddRange(frameRaw);
            }

            return framesRaw.ToArray();
        }

        public byte[] Serialize()
        {
            var tag = new List<byte>(10);

            // tag header - 10 bytes
            tag.Add((byte)'I'); // 0 fixed file id
            tag.Add((byte)'D'); // 1 fixed file id
            tag.Add((byte)'3'); // 2 fixed file id
            tag.Add(4);         // 3 major version
            tag.Add(0);         // 4 minor version
            tag.Add(0);         // 5 flags

            // size - 4 bytes
            var framesRaw = SerializeFrames(Frames);
            var tagSize = framesRaw.Length;
            var sizeBytes = new SynchsafeInteger(tagSize).ToArray();
            tag.AddRange(sizeBytes); // 6,7,8,9

            tag.AddRange(framesRaw);

            return tag.ToArray();
        }
    }
}
