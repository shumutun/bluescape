using ICSharpCode.SharpZipLib.Tar;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text.RegularExpressions;

namespace judge.ImageContent.Typescript
{
    public class TypescriptImageContentBuider : ImageContentBuider
    {
        protected override string GetDockerfileContent(DirectoryInfo submission)
        {
            var runFileNameMatch = Regex.Match(submission.Name, ".*_(.*)");
            if (!runFileNameMatch.Success)
                return null;
            var dockerfileTemplate = GetDockerfileTemplate(Languages.Typescript);
            return dockerfileTemplate.Replace("<RunFileName>", $"{runFileNameMatch.Groups[1].Value}.js");
        }

        protected override void AddSubmissionContent(string rootPath, TarOutputStream archive, DirectoryInfo submission)
        {
            const string tsconfigFileName = "tsconfig.json";

            var tsconfigHandled = false;
            foreach (var file in submission.GetFiles())
                if (file.Name == tsconfigFileName)
                {
                    using var tsconfigStream = HandleTsConfig(file);
                    TarAppFile(rootPath, file.Name, tsconfigStream, archive);
                    tsconfigHandled = true;
                }
                else
                {
                    using var fileStream = file.OpenRead();
                    TarAppFile(rootPath, file.Name, fileStream, archive);
                }
            if (!tsconfigHandled)
            {
                using var tsconfigStream = CreateTsConfig();
                TarAppFile(rootPath, tsconfigFileName, tsconfigStream, archive);
            }
            foreach (var dir in submission.GetDirectories())
                TarAppDir(Path.Combine(rootPath, dir.Name), dir, archive);
        }

        private Stream HandleTsConfig(FileInfo tsconfig)
        {
            using var file = tsconfig.OpenRead();
            using var fileReader = new StreamReader(file);
            using var reader = new JsonTextReader(fileReader);
            var jDoc = JToken.ReadFrom(reader);

            jDoc["compilerOptions"]["outDir"] = "./build";

            var res = new MemoryStream();
            using var textWriter = new StreamWriter(res);
            using var writer = new JsonTextWriter(textWriter);
            jDoc.WriteTo(writer);
            return res;
        }

        private Stream CreateTsConfig()
        {
            var jDoc = JObject.Parse("{'compilerOptions': {'outDir': './build'}}");
            var res = new MemoryStream();
            using var textWriter = new StreamWriter(res);
            using var writer = new JsonTextWriter(textWriter);
            jDoc.WriteTo(writer);
            return res;
        }
    }
}
