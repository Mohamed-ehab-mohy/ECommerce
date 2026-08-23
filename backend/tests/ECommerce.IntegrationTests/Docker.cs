using System.IO.Pipes;

namespace ECommerce.IntegrationTests;

public static class Docker
{
    public static bool IsAvailable { get; } = Detect();

    private static bool Detect()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var pipe = new NamedPipeClientStream(".", "docker_engine", PipeDirection.InOut);
                pipe.Connect(2000);
                return pipe.IsConnected;
            }

            return File.Exists("/var/run/docker.sock");
        }
        catch (Exception)
        {
            return false;
        }
    }
}
