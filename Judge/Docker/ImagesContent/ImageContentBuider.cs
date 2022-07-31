using ICSharpCode.SharpZipLib.Tar;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Judge.Docker.ImagesContent
{
    public abstract class ImageContentBuider : IImageContentBuider
    {
        public Errorable<Stream> BuildImageContent(DirectoryInfo submission)
        {
            var dockerfileContentRes = GetDockerfileContent(submission);
            if (dockerfileContentRes.IsError)
                return dockerfileContentRes.Error;

            var tarball = new MemoryStream();
            using var archive = new TarOutputStream(tarball, Encoding.UTF8)
            {
                IsStreamOwner = false
            };

            using var dockerfileStream = new MemoryStream(Encoding.UTF8.GetBytes(dockerfileContentRes.Value));
            TarAppFile(null, "Dockerfile", dockerfileStream, archive);

            var addSubmissionContentError = AddSubmissionContent("app", archive, submission);
            if (addSubmissionContentError != null)
                return addSubmissionContentError;

            archive.Close();
            tarball.Position = 0;
            return new Errorable<Stream>(tarball);
        }

        protected abstract Languages Language { get; }
        protected virtual Errorable<string> GetDockerfileContent(DirectoryInfo submission)
        {
            var dockerfileTemplate = GetDockerfileTemplate(Language);
            if (dockerfileTemplate == null)
                return $"Can't find a dockerfile template for the laguage '{Language}'";

            var files = submission.GetFiles();
            if (files.Length == 1)
                return new Errorable<string>(value: dockerfileTemplate.Replace("<RunFileName>", $"{Path.GetFileNameWithoutExtension(files[0].Name)}"));

            var runFileNameMatch = Regex.Match(submission.Name, ".*_(.*)$");
            if (!runFileNameMatch.Success)
                return  "Invalid submission name";

            return new Errorable<string>(value: dockerfileTemplate.Replace("<RunFileName>", $"{runFileNameMatch.Groups[1].Value}"));
        }
        protected string? GetDockerfileTemplate(Languages language)
        {
            var assembly = GetType().Assembly;
            using var stream = assembly.GetManifestResourceStream($"Judge.Docker.ImagesContent.{language}.DockerfileTemplate");
            if (stream == null)
                return null;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        protected virtual string? AddSubmissionContent(string rootPath, TarOutputStream archive, DirectoryInfo submission)
        {
            TarAppDir(rootPath, submission, archive);
            return null;
        }
        protected static void TarAppDir(string rootPath, DirectoryInfo submission, TarOutputStream archive)
        {
            foreach (var file in submission.GetFiles())
            {
                using var fileStream = file.OpenRead();
                TarAppFile(rootPath, file.Name, fileStream, archive);
            }
            foreach (var dir in submission.GetDirectories())
                TarAppDir(Path.Combine(rootPath, dir.Name), dir, archive);
        }
        protected static void TarAppFile(string? rootPath, string fileName, Stream fileStream, TarOutputStream archive)
        {
            var submissionEntry = TarEntry.CreateTarEntry(string.IsNullOrWhiteSpace(rootPath) ? fileName : Path.Combine(rootPath, fileName));
            submissionEntry.Size = fileStream.Length;
            submissionEntry.TarHeader.Mode = Convert.ToInt32("100755", 8); //chmod 755
            archive.PutNextEntry(submissionEntry);
            var submissionData = new byte[fileStream.Length];
            fileStream.Read(submissionData, 0, submissionData.Length);
            archive.Write(submissionData, 0, submissionData.Length);
            archive.CloseEntry();
        }
    }
}
