using ICSharpCode.SharpZipLib.Tar;
using System;
using System.IO;
using System.Text;

namespace judge.ImageContent
{
    public abstract class ImageContentBuider : IImageContentBuider
    {
        public Stream BuildImageContent(DirectoryInfo submission)
        {
            var tarball = new MemoryStream();
            using var archive = new TarOutputStream(tarball, Encoding.UTF8)
            {
                IsStreamOwner = false
            };
            var dockerFile = GetDockerfileContent(submission);
            using var dockerfileStream = new MemoryStream(Encoding.UTF8.GetBytes(dockerFile));
            TarAppFile(null, "Dockerfile", dockerfileStream, archive);
            AddSubmissionContent("app", archive, submission);
            archive.Close();
            tarball.Position = 0;
            return tarball;
        }

        protected virtual void AddSubmissionContent(string rootPath, TarOutputStream archive, DirectoryInfo submission)
        {
            TarAppDir(rootPath, submission, archive);
        }

        protected abstract string GetDockerfileContent(DirectoryInfo submission);

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
