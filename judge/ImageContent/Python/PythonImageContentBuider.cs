using System.IO;
using System.Text.RegularExpressions;

namespace judge.ImageContent.Typescript.Python
{
    public class PythonImageContentBuider : ImageContentBuider
    {
        protected override string GetDockerfileContent(DirectoryInfo submission)
        {
            var runFileNameMatch = Regex.Match(submission.Name, ".*_(.*)");
            if (!runFileNameMatch.Success)
                return null;
            var dockerfileTemplate = GetDockerfileTemplate(Languages.Typescript);
            return dockerfileTemplate.Replace("<RunFileName>", $"{runFileNameMatch.Groups[1].Value}.py");
        }
    }
}
