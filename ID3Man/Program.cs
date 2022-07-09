using System;

namespace ID3Man
{
    internal class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("usage: id3man <file> <tagName>");
                return -1;
            }

            var parser = new Id3TagParser(args[0]);
            var value = parser.GetFrameValue(args[1]);
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
        }
    }
}
