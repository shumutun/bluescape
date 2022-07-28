using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace judge.ImageContent
{
    public interface IImageContentBuider
    {
        (Stream content, string error) BuildImageContent(DirectoryInfo submission);
    }
}
