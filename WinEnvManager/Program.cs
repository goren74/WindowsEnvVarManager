namespace WinEnvManager
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            EnvManager.Init();
            if (args == null || args.Length == 0)
            {
                Application.Run(new Form1());
            } else
            {
                Application.Run(new FormApplyEnv());
            }
        }
    }
}