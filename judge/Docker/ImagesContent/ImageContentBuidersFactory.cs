using Judge.Docker.ImagesContent.Python;
using Judge.Docker.ImagesContent.Typescript;
using System;

namespace Judge.Docker.ImagesContent
{
    public class ImageContentBuidersFactory : IImageContentBuidersFactory
    {
        public IImageContentBuider BuildImageContentBuider(string lang)
        {
            return lang switch
            {
                "python" => new PythonImageContentBuider(),
                "typescript" => new TypescriptImageContentBuider(),
                _ => throw new ArgumentException(nameof(lang))
            };
        }
    }
}
