using System.IO;
using System.Threading.Tasks;

namespace Judge.Docker
{
    public interface IDockerApiClient
    {
        string DockerVersion { get; }

        Task<Errorable<string>> CreateContainer(string lang, DirectoryInfo submission);
        Task<Errorable<string>> RunContainer(string containerId, Stream input);
    }
}
