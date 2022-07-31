using System.IO;

namespace Judge.Docker.ImageContent
{
    public interface IImageContentBuider
    {
        Errorable<Stream> BuildImageContent(DirectoryInfo submission);
    }
}
