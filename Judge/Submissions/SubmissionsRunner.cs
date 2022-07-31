using Judge.Codes;
using Judge.Docker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Judge.Submissions
{
    public class SubmissionsRunner
    {
        private readonly IDockerApiClient _dockerClient;
        private readonly IReadOnlyCollection<Code> _codes;

        public SubmissionsRunner(IDockerApiClient dockerClient, IReadOnlyCollection<Code> codes)
        {
            _dockerClient = dockerClient;
            _codes = codes;
        }

        public async Task RunSubmissionsForLannguage(DirectoryInfo languageSubmissionsDir, Action<string> onSubmissionReport)
        {
            var lang = languageSubmissionsDir.Name;
            foreach (var submission in languageSubmissionsDir.GetDirectories())
            {
                var report = new StringBuilder();
                await RunSubmission(submission, lang, report);
                onSubmissionReport(report.ToString());
            }
        }

        private async Task RunSubmission(DirectoryInfo submission, string lang, StringBuilder report)
        {
            report.AppendLine($"The judge is considering the case '{lang}: {submission.Name}'");
            var containerId = await _dockerClient.CreateContainer(lang, submission);
            if (containerId.IsError)
            {
                report.AppendLine($"Failed to create a container: {containerId.GetError()}");
                return;
            }
            var successCount = 0;
            foreach (var code in _codes)
            {
                report.Append($"\tCode {code.File.Name}...");
                using var input = code.File.OpenRead();
                var startTime = DateTime.UtcNow;
                var res = await _dockerClient.RunContainer(containerId.GetValue(), input);
                if (res.IsError)
                {
                    report.AppendLine($" Error occurred while checking the code '{code.File.Name}': {res.GetError()}");
                    continue;
                }
                if (string.Equals(res.GetValue(), code.ExpectedRes, StringComparison.OrdinalIgnoreCase))
                    successCount++;
                
                report.AppendLine($" Expected: '{code.ExpectedRes}'; Recieved: '{res.GetValue()}'; in {DateTime.UtcNow - startTime:c}");
            }
            report.AppendLine($"\tScore: {successCount} / {_codes.Count}");
        }
    }
}
