using System;

namespace ID3Man
{
    // todo: support frame id
    internal class Program
    {
        private static string Usage = @"Usage:
id3man <file> - list all frame as key-value from file <file>
id3man <file> <name> - get value for <name> frame from file <file>
id3man <in-file> <name> <value> <out-file> - create <out-file> as copy of <in-file> with set value <value >for frame <name>";
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine(Usage);
                return -1;
            }

            var manager = new TagManager(args[0]);
            switch (args.Length)
            {
                case 1: // get all frames
                    foreach (var frame in manager.GetFrames())
                    {
                        Console.WriteLine($"{frame.Key} = {frame.Value}");
                    }
                    return 0;

                case 2: // get specific frame value
                    var value = manager.GetFrameValue(args[1]);
                    if (string.IsNullOrEmpty(value))
                    {
                        Console.Error.WriteLine($"tag {args[1]} not found");
                        return -2;
                    }
                    else
                    {
                        Console.WriteLine(value);
                        return 0;
                    }

                case 4: // set specific frame value
                    manager.SetFrameValue(args[1], args[2], args[3]);
                    return 0;

                default:
                    Console.Error.WriteLine(Usage);
                    return -1;
            }
        }

/*
        // to lazy for test project
        static int Main(string[] args)
        {
            // test SynchsafeInteger
            // foreach (var i in new [] {0x0,
            //                           0xFF,
            //                           0xFF00,
            //                           0x120000,
            //                           0x0A000001 })
            // {
            //     var integer = new SynchsafeInteger(i);
            //     var raw = integer.ToArray();
            //     var integer2 = new SynchsafeInteger(raw);
            //     if (i != integer2.ToInt())
            //     {
            //         Console.WriteLine("error");
            //         return -1;
            //     }
            // }

            // chech parsing
            // var tag = Tag.GetFromFile(args[0]);
            // var tagRaw = tag.Serialize();
            // var tag2 = Tag.Deserialize(tagRaw);
            // var tagRaw2 = tag2.Serialize();

            // if (!System.Linq.Enumerable.SequenceEqual(tagRaw, tagRaw2))
            // {
            //     Console.WriteLine("error");
            //     return -1;
            // }

            // if (!System.Linq.Enumerable.SequenceEqual(tag.Frames, tag2.Frames))
            // {
            //     Console.WriteLine("error");
            //     return -1;
            // }

            return 0;
        }
*/
    }
}
