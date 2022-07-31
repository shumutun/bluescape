using Docker.DotNet;
using Docker.DotNet.Models;
using Judge.Docker.ImagesContent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Judge.Docker
{
    public class DockerApiConfiguration
    {
        private readonly IImageContentBuidersFactory _imageContentBuidersFactory;

        public DockerApiConfiguration()
        {
            _imageContentBuidersFactory = new ImageContentBuidersFactory();
        }

        public async Task<IDockerApiClient> BuildClient()
        {
            var dockerClient = new DockerClientConfiguration().CreateClient();
            var giveupTime = DateTime.UtcNow.AddMinutes(1);
            for (; ; )
            {
                try
                {
                    await Task.Delay(500);
                    var version = await dockerClient.System.GetVersionAsync();
                    return new DockerApiClient(dockerClient, version, _imageContentBuidersFactory);
                }
                catch (HttpRequestException)
                {
                    if (DateTime.UtcNow > giveupTime)
                        throw;
                }
            }
        }

        private class DockerApiClient : IDockerApiClient
        {
            private readonly DockerClient _dockerClient;
            private readonly VersionResponse _version;
            private readonly IImageContentBuidersFactory _imageContentBuidersFactory;

            public string DockerVersion => _version.Version;

            public DockerApiClient(DockerClient dockerClient, VersionResponse version, IImageContentBuidersFactory imageContentBuidersFactory)
            {
                _dockerClient = dockerClient;
                _version = version;
                _imageContentBuidersFactory = imageContentBuidersFactory;
            }

            public async Task<Errorable<string>> CreateContainer(string lang, DirectoryInfo submission)
            {
                var imageId = await BuildImage(lang, submission);
                if (imageId.IsError)
                    return imageId.GetError();
                var resp = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters()
                {
                    Image = imageId.GetValue(),
                    AttachStdin = true,
                    AttachStdout = true,
                    AttachStderr = true,
                    StdinOnce = true,
                    OpenStdin = true
                }); ;
                return new Errorable<string>(resp.ID);
            }
            private async Task<Errorable<string>> BuildImage(string lang, DirectoryInfo submission)
            {
                var imageContentBuilder = _imageContentBuidersFactory.BuildImageContentBuider(lang);
                using var imageContent = imageContentBuilder.BuildImageContent(submission);
                if (imageContent.IsError)
                    return imageContent.GetError();

                var imageTag = $"{lang}_{submission.Name}";
                var logs = new List<string>();
                await _dockerClient.Images.BuildImageFromDockerfileAsync(new ImageBuildParameters
                {
                    Tags = new[] { imageTag }
                }, imageContent.GetValue(), Array.Empty<AuthConfig>(), new Dictionary<string, string>(),
                new Progress<JSONMessage>(m =>
                {
                    if (!string.IsNullOrWhiteSpace(m.Stream))
                        logs.Add(m.Stream);
                }));

                var lastLog = logs.Last();
                var match = Regex.Match(lastLog, @"Successfully built ([a-z0-9]+)\n");
                if (match.Success)
                    return new Errorable<string>(match.Groups[1].Value);
                match = Regex.Match(lastLog, @"---> ([a-z0-9]+)\n");
                if (match.Success)
                    return new Errorable<string>(match.Groups[1].Value);

                var errorBuilder = new StringBuilder().AppendLine("Couldn't find an image ID in image build logs:");
                foreach (var log in logs)
                    errorBuilder.AppendLine(log);
                return errorBuilder.ToString();
            }

            public async Task<Errorable<string>> RunContainer(string containerId, Stream input)
            {
                using var stream = await _dockerClient.Containers.AttachContainerAsync(containerId, false, new ContainerAttachParameters
                {
                    Stream = true,
                    Stderr = true,
                    Stdin = true,
                    Stdout = true
                });

                if (!await _dockerClient.Containers.StartContainerAsync(containerId, new ContainerStartParameters()))
                    return await GetError(containerId);

                await WriteFileToStdin(input, stream);
                stream.CloseWrite();

                var waitResp = await _dockerClient.Containers.WaitContainerAsync(containerId);
                if (waitResp.StatusCode != 0)
                    return await GetError(containerId);

                var (stdout, stderr) = await stream.ReadOutputToEndAsync(default);
                return new Errorable<string>(stdout);
            }
            private async Task<string> GetError(string containerId)
            {
                var errStream = await _dockerClient.Containers.GetContainerLogsAsync(containerId, false, new ContainerLogsParameters
                {
                    ShowStderr = true
                });
                var (stdout, stderr) = await errStream.ReadOutputToEndAsync(default);
                return stderr ?? stdout;
            }
            static async Task WriteFileToStdin(Stream input, MultiplexedStream stream)
            {
                var offset = 0;
                var buffer = new byte[1000];
                do
                {
                    var readed = input.Read(buffer, offset, buffer.Length);
                    if (readed > 0)
                        await stream.WriteAsync(buffer, offset, readed, default);
                    offset += readed;
                }
                while (offset < input.Length);
            }
        }
    }
}
