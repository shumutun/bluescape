using System.IO;

namespace Judge.Docker.ImagesContent
{
    public interface IImageContentBuider
    {
        Errorable<Stream> BuildImageContent(DirectoryInfo submission);
    }
}
