using System.IO;

namespace Judge.Codes
{
    public class Code
    {
        public string ExpectedRes { get; }
        public FileInfo File { get; }

        public Code(FileInfo file, string expectedRes)
        {
            File = file;
            ExpectedRes = expectedRes;
        }
    }
}
