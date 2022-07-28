using judge.ImageContent.Typescript;
using judge.ImageContent.Typescript.Python;
using System;
using System.IO;

namespace judge.ImageContent
{
    public static class ImageContentBuidersFactory
    {
        public static IImageContentBuider GetImageContentBuider(string lang)
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
