using Docker.DotNet;
using Docker.DotNet.Models;
using judge.ImageContent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Judge
{
    class Program
    {
        static void Main(string[] args)
        {
            //const string path = "/app/assesments";
            const string path = "../../bluescape-judge-assesments";

            Console.WriteLine("Preparing for a court session...");

            var submissionsDir = new DirectoryInfo(Path.Combine(path, "submissions"));
            var codesDir = new DirectoryInfo(Path.Combine(path, "codes"));
            try
            {
                var task = RunSubmissions(submissionsDir, codesDir);
                task.Wait();
                Console.WriteLine("The court session ended!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"The court session was interrupted by an error!");
                Console.WriteLine(ex);
            }
        }

        static async Task<DockerClient> Connect()
        {
            for (; ; )
            {
                try
                {
                    await Task.Delay(500);
                    var dockerClient = new DockerClientConfiguration().CreateClient();
                    var version = await dockerClient.System.GetVersionAsync();
                    Console.WriteLine($"The court session has begun (docker version: {version.Version}).");
                    return dockerClient;
                }
                catch (HttpRequestException) { }
            }
        }

        static async Task RunSubmissions(DirectoryInfo submissionsDir, DirectoryInfo codesDir)
        {
            using var dockerClient = await Connect();
            var codes = ParseCodes(codesDir).ToArray();
            foreach (var languageSubmissions in submissionsDir.GetDirectories())
            {
                foreach (var submission in languageSubmissions.GetDirectories())
                {
                    Console.Write($"The judge reviewing the case '{languageSubmissions.Name}: {submission.Name}'...");
                    var res = await RunSubmission(dockerClient, languageSubmissions.Name, submission, codes);
                    Console.WriteLine($" The decision on the case: {res}");
                }
            }
        }

        static IEnumerable<(FileInfo fileInfo, bool expectedRes)> ParseCodes(DirectoryInfo codesDir)
        {
            foreach (var code in codesDir.GetFiles())
            {
                var expectedResMatch = Regex.Match(code.Name, @".*_(True|False)\.txt", RegexOptions.IgnoreCase);
                if (expectedResMatch.Success)
                    if (bool.TryParse(expectedResMatch.Groups[1].Value, out bool expectedRes))
                        yield return (code, expectedRes);
            }
        }

        static async Task<string> RunSubmission(DockerClient dockerClient, string lang, DirectoryInfo submission, (FileInfo fileInfo, bool expectedRes)[] codes)
        {
            var createContainerRes = await CreateContainer(dockerClient, lang, submission);
            if (createContainerRes.containerId == null)
                return createContainerRes.error;
            var successCount = 0;
            var execTime = 0.0;
            foreach (var (fileInfo, expectedRes) in codes)
            {
                var res = await RunSubmissionForCode(dockerClient, createContainerRes.containerId, fileInfo);
                if (!res.resp.HasValue)
                    return $"Error occured while checking the code '{fileInfo.Name}': {res.err}";
                if (expectedRes == res.resp.Value)
                    successCount++;

                execTime += res.execTime.Value.TotalMilliseconds;
            }
            return $"Score: {successCount} / {codes.Length} in {TimeSpan.FromMilliseconds(execTime).ToString("c")}";
        }

        static async Task<(string imageId, string error)> BuildImage(DockerClient dockerClient, string lang, DirectoryInfo submission)
        {
            var buildImageContentResult = ImageContentBuidersFactory.GetImageContentBuider(lang).BuildImageContent(submission);
            if (buildImageContentResult.content == null)
                return (null, buildImageContentResult.error);
            try
            {
                var logs = new List<string>();
                await dockerClient.Images.BuildImageFromDockerfileAsync(new ImageBuildParameters(), buildImageContentResult.content, Array.Empty<AuthConfig>(), new Dictionary<string, string>(),
                new Progress<JSONMessage>(m =>
                {
                    if (!string.IsNullOrWhiteSpace(m.Stream))
                        logs.Add(m.Stream);
                }));

                foreach (var log in logs)
                {
                    var match = Regex.Match(log, @"Successfully built (.*)\n");
                    if (match.Success)
                        return (match.Groups[1].Value, null);

                }
                var errorBuilder = new StringBuilder().AppendLine("Couldn't find an image ID in image build logs:");
                foreach (var log in logs)
                    errorBuilder.AppendLine(log);

                return (null, errorBuilder.ToString());
            }
            finally
            {
                buildImageContentResult.content.Dispose();
            }
        }

        static async Task<(string containerId, string error)> CreateContainer(DockerClient dockerClient, string lang, DirectoryInfo submission)
        {
            var imageBuildRes = await BuildImage(dockerClient, lang, submission);
            if (imageBuildRes.imageId == null)
                return (null, imageBuildRes.error);
            var resp = await dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters()
            {
                Image = imageBuildRes.imageId,
                AttachStdin = true,
                AttachStdout = true,
                AttachStderr = true,
                StdinOnce = true,
                OpenStdin = true
            });
            return (resp.ID, null);
        }

        static async Task<(bool? resp, string err, TimeSpan? execTime)> RunSubmissionForCode(DockerClient dockerClient, string containerId, FileInfo code)
        {
            using var stream = await dockerClient.Containers.AttachContainerAsync(containerId, false, new ContainerAttachParameters
            {
                Stream = true,
                Stderr = true,
                Stdin = true,
                Stdout = true
            });

            if (!await dockerClient.Containers.StartContainerAsync(containerId, new ContainerStartParameters()))
                return (null, "Can't start a container", null);

            var startTime = DateTime.UtcNow;
            await WriteFileToStdin(code, stream);
            stream.CloseWrite();

            var waitResp = await dockerClient.Containers.WaitContainerAsync(containerId);
            var execTime = DateTime.UtcNow - startTime;
            if (waitResp.StatusCode != 0)
            {
                var errStream = await dockerClient.Containers.GetContainerLogsAsync(containerId, false, new ContainerLogsParameters
                {
                    ShowStderr = true
                });
                var err = await errStream.ReadOutputToEndAsync(default);
                return (null, err.stderr, execTime);
            }
            var output = await stream.ReadOutputToEndAsync(default);
            if (bool.TryParse(output.stdout, out bool resp))
                return (resp, null, execTime);
            return (null, $"Submission response is invalid: {output.stdout}", execTime);
        }

        static async Task WriteFileToStdin(FileInfo file, MultiplexedStream stream)
        {
            using var codeStream = file.OpenRead();
            var offset = 0;
            var buffer = new byte[1000];
            var readed = 0;
            do
            {
                readed = codeStream.Read(buffer, offset, buffer.Length);
                if (readed > 0)
                    await stream.WriteAsync(buffer, offset, readed, default);
                offset += readed;
            }
            while (offset < codeStream.Length);
        }
    }
}
