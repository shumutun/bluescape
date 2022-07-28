using System.IO;
using System.Text.RegularExpressions;

namespace judge.ImageContent.Typescript.Python
{
    public class PythonImageContentBuider : ImageContentBuider
    {
        protected override (string content, string error) GetDockerfileContent(DirectoryInfo submission)
        {
            var runFileNameMatch = Regex.Match(submission.Name, ".*_(.*)");
            if (!runFileNameMatch.Success)
                return (null, "Invalid submission name");
            var dockerfileTemplate = GetDockerfileTemplate(Languages.Python);
            return (dockerfileTemplate.Replace("<RunFileName>", $"{runFileNameMatch.Groups[1].Value}.py"), null);
        }
    }
}
