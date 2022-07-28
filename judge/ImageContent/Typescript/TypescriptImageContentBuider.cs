using ICSharpCode.SharpZipLib.Tar;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace judge.ImageContent.Typescript
{
    public class TypescriptImageContentBuider : ImageContentBuider
    {
        protected override (string content, string error) GetDockerfileContent(DirectoryInfo submission)
        {
            var runFileNameMatch = Regex.Match(submission.Name, ".*_(.*)");
            if (!runFileNameMatch.Success)
                return (null, "Invalid submission name");
            var dockerfileTemplate = GetDockerfileTemplate(Languages.Typescript);
            return (dockerfileTemplate.Replace("<RunFileName>", $"{runFileNameMatch.Groups[1].Value}.js"), null);
        }

        protected override (bool success, string error) AddSubmissionContent(string rootPath, TarOutputStream archive, DirectoryInfo submission)
        {
            const string tsconfigFileName = "tsconfig.json";

            var tsconfigHandled = false;
            foreach (var file in submission.GetFiles())
                if (file.Name == tsconfigFileName)
                {
                    var tsconfig = HandleTsConfig(file);
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
                return (false, $"{tsconfigFileName} not found");

            foreach (var dir in submission.GetDirectories())
                TarAppDir(Path.Combine(rootPath, dir.Name), dir, archive);

            return (true, null);
        }

        private string HandleTsConfig(FileInfo tsconfig)
        {
            using var file = tsconfig.OpenRead();
            using var fileReader = new StreamReader(file);
            using var reader = new JsonTextReader(fileReader);
            var jDoc = JToken.ReadFrom(reader);
            jDoc["compilerOptions"]["outDir"] = "./build";
            return jDoc.ToString(Formatting.Indented);
        }
    }
}
