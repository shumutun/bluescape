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
            report.Append($"The judge reviewing the case '{lang}: {submission.Name}'...");
            var containerId = await _dockerClient.CreateContainer(lang, submission);
            if (containerId.IsError)
            {
                report.AppendLine(containerId.GetError());
                return;
            }
            var successCount = 0;
            var execTime = TimeSpan.Zero;
            foreach (var code in _codes)
            {
                using var input = code.File.OpenRead();
                var startTime = DateTime.Now;
                var res = await _dockerClient.RunContainer(containerId.GetValue(), input);
                if (res.IsError)
                {
                    report.AppendLine($"Error occured while checking the code '{code.File.Name}': {res.GetError()}");
                    continue;
                }
                if (string.Equals(res.GetValue(), code.ExpectedRes, StringComparison.OrdinalIgnoreCase))
                    successCount++;

                execTime.Add(DateTime.UtcNow - startTime);
            }
            report.AppendLine($"Score: {successCount} / {_codes.Count} in {execTime:c}");
        }
    }
}
