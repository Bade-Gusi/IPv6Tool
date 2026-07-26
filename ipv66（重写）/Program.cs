namespace ipv66_重写_
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1(isAutoStart: args.Contains("--autostart")));
        }
    }
}
