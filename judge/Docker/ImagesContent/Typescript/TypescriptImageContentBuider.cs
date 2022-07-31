using ICSharpCode.SharpZipLib.Tar;
using Judge.Docker.ImageContent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;

namespace Judge.ImageContent.Typescript
{
    public class TypescriptImageContentBuider : ImageContentBuider
    {
        protected override Languages Language => Languages.Typescript;

        protected override string? AddSubmissionContent(string rootPath, TarOutputStream archive, DirectoryInfo submission)
        {
            const string tsconfigFileName = "tsconfig.json";

            var tsconfigHandled = false;
            foreach (var file in submission.GetFiles())
                if (file.Name == tsconfigFileName)
                {
                    var tsconfig = HandleTsConfig(file);
                    if (tsconfig == null)
                        return $"Invalid {tsconfigFileName}";
                    using var tsconfigStream = new MemoryStream(Encoding.UTF8.GetBytes(tsconfig));
                    TarAppFile(rootPath, file.Name, tsconfigStream, archive);
                    tsconfigHandled = true;
                }
                else
                {
                    using var fileStream = file.OpenRead();
                    TarAppFile(rootPath, file.Name, fileStream, archive);
                }
            if (!tsconfigHandled)
                return $"{tsconfigFileName} not found";

            foreach (var dir in submission.GetDirectories())
                TarAppDir(Path.Combine(rootPath, dir.Name), dir, archive);

            return null;
        }

        private string? HandleTsConfig(FileInfo tsconfig)
        {
            using var file = tsconfig.OpenRead();
            using var fileReader = new StreamReader(file);
            using var reader = new JsonTextReader(fileReader);
            var jDoc = JToken.ReadFrom(reader);
            var compilerOptions = jDoc["compilerOptions"];
            if (compilerOptions == null)
                return null;
            compilerOptions["outDir"] = "./build";
            return jDoc.ToString(Formatting.Indented);
        }
    }
}
