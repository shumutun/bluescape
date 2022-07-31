using Judge.ImageContent.Typescript;
using Judge.ImageContent.Typescript.Python;
using System;

namespace Judge.Docker.ImageContent
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
