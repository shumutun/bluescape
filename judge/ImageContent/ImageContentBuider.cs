using ICSharpCode.SharpZipLib.Tar;
using System;
using System.IO;
using System.Text;

namespace judge.ImageContent
{
    public abstract class ImageContentBuider : IImageContentBuider
    {
        public (Stream content, string error) BuildImageContent(DirectoryInfo submission)
        {
            var tarball = new MemoryStream();
            using var archive = new TarOutputStream(tarball, Encoding.UTF8)
            {
                IsStreamOwner = false
            };
            var dockerFile = GetDockerfileContent(submission);
            if (dockerFile.content == null)
                return (null, dockerFile.error);
            using var dockerfileStream = new MemoryStream(Encoding.UTF8.GetBytes(dockerFile.content));
            TarAppFile(null, "Dockerfile", dockerfileStream, archive);

            var addSubmissionContentRes = AddSubmissionContent("app", archive, submission);
            if (!addSubmissionContentRes.success)
                return (null, addSubmissionContentRes.error);
            archive.Close();
            tarball.Position = 0;
            return (tarball, null);
        }

        protected virtual (bool success, string error) AddSubmissionContent(string rootPath, TarOutputStream archive, DirectoryInfo submission)
        {
            TarAppDir(rootPath, submission, archive);
            return (true, null);
        }

        protected abstract (string content, string error) GetDockerfileContent(DirectoryInfo submission);

        protected static Stream GetSubmissionStream(DirectoryInfo submission, string dockerFile)
        {
            var tarball = new MemoryStream();
            using var archive = new TarOutputStream(tarball, Encoding.UTF8)
            {
                IsStreamOwner = false
            };
            using var dockerfileStream = new MemoryStream(Encoding.UTF8.GetBytes(dockerFile));
            TarAppFile(null, "Dockerfile", dockerfileStream, archive);
            TarAppDir("app", submission, archive);
            archive.Close();
            tarball.Position = 0;
            return tarball;
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
        protected static void TarAppFile(string rootPath, string fileName, Stream fileStream, TarOutputStream archive)
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
        protected static string GetDockerfileTemplate(Languages lang)
        {
            var assembly = typeof(ImageContentBuidersFactory).Assembly;
            var stream = assembly.GetManifestResourceStream($"judge.ImageContent.{lang}.Dockerfile");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
