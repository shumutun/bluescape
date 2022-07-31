using Judge.Codes;
using Judge.Docker;
using Judge.Submissions;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Judge
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Startig a court session...");
                MainAsync(args).Wait();
                Console.WriteLine("The court session is over!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"The court session was interrupted by the error:");
                Console.WriteLine(ex);
            }
        }

        static async Task MainAsync(string[] args)
        {
            //const string path = "/app/assesments";
            const string path = "../../bluescape-judge-assesments";

            var submissionsDir = new DirectoryInfo(Path.Combine(path, "submissions"));
            var codesDir = new DirectoryInfo(Path.Combine(path, "codes"));

            var codes = new CodesSet(codesDir);
            if (codes.HasErrors)
            {
                Console.WriteLine($"Code were parsed with errors:");
                foreach (var error in codes.GetErrors())
                    Console.WriteLine(error);
            }

            var dockerClient = await new DockerApiConfiguration().BuildClient();
            var submissionsRunner = new SubmissionsRunner(dockerClient, codes);
            Console.WriteLine($"The court session has begun (docker version: {dockerClient.DockerVersion})");
            Console.WriteLine();

            foreach (var languageSubmissions in submissionsDir.GetDirectories())
                await submissionsRunner.RunSubmissionsForLannguage(languageSubmissions, Console.WriteLine);
        }

    }
}
