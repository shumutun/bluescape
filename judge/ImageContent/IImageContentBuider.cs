using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace judge.ImageContent
{
    public interface IImageContentBuider
    {
        Stream BuildImageContent(DirectoryInfo submission);
    }
}
