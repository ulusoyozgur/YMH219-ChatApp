namespace ChatApp;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.Run(args.Contains("--client") ? new ClientForm() : new ServerForm());
    }
}
