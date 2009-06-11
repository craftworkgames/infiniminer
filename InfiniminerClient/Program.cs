using System;

namespace Infiniminer.Client
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            using (InfiniminerGame game = new InfiniminerGame(args))
            {
#if !DEBUG
                try
                {
#endif
                    game.Run();
#if !DEBUG
                }
                catch (Exception e)
                {
                    System.Windows.Forms.MessageBox.Show(e.Message + "\r\n\r\n" + e.StackTrace);
                }
#endif
            }
        }
    }
}

