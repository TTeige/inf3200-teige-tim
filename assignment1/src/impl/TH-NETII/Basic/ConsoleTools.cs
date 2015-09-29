using System;

namespace THNETII
{
    /// <summary>
    /// Static class that provides functions and procedures for common interactions with the console class in a .NET Console application.
    /// </summary>
    public static class ConsoleTools
    {
        /// <summary>
        /// Writes a message to the console prompting the user to press a certain key so that the program continues to execute the specified action.
        /// This procedure intercepts all keys that are pressed, so that nothing is displayed in the Console. By default the
        /// Enter key is used as the key that has to be pressed.
        /// </summary>
        /// <param name="key">The key that the Console will wait for. If this parameter is omitted the Enter key is used.</param>
        /// <param name="action">The action that the user is prompted to press the <paramref name="key"/> for.</param>
        public static void WriteKeyPressForExit(ConsoleKey key = ConsoleKey.Enter, string action = "exit")
        {
            Console.WriteLine();
            Console.WriteLine("Press the {0} key on your keyboard to {1} . . .", key, action);
            while (Console.ReadKey(intercept: true).Key != key) { }
        }

        /// <summary>
        /// Emulates the pause command in the console by invoking a cmd-process. This method blocks the execution until the user has pressed a button. The console will output a string that is defined by the language setting of your operating system
        /// </summary>
        public static void Pause()
        {
            if ((PlatformID.Win32NT | PlatformID.Win32Windows).HasFlag(Environment.OSVersion.Platform))
            {
                Console.WriteLine();
                System.Diagnostics.Process pauseProc =
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo() { FileName = "cmd", Arguments = "/C pause", UseShellExecute = false });
                pauseProc.WaitForExit();
            }
            else
            {
                ConsoleTools.WriteKeyPressForExit(action: "continue");
            }
        }

        /// <summary>
        /// Reads a password from the standard input stream without printing the password characters to the screen.
        /// </summary>
        /// <remarks>Pressing the backspace key does work while entering the password.</remarks>
        /// <returns>A string containing the password that has been entered into the standard input stream.</returns>
        public static string GetPassword()
        {
            System.Text.StringBuilder pwdBuilder = new System.Text.StringBuilder();
            while (true)
            {
                ConsoleKeyInfo i = Console.ReadKey(true);
                if (i.Key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (i.Key == ConsoleKey.Backspace)
                {
                    pwdBuilder.Remove(startIndex: pwdBuilder.Length - 1, length: 1);
                    //Console.Write("\b \b");
                }
                else
                {
                    pwdBuilder.Append(i.KeyChar);
                    //Console.Write("*");
                }
            }
            return pwdBuilder.ToString();
        }

        /// <summary>
        /// Reads a password from the standard input stream without printing the password characters to the screen.
        /// </summary>
        /// <remarks>Pressing the backspace key does work while entering the password.</remarks>
        /// <returns>A <see cref="System.Security.SecureString"/> instance containing the password that has been entered into the standard input stream.</returns>
        public static System.Security.SecureString GetSecurePassword()
        {
            System.Security.SecureString pwd = new System.Security.SecureString();
            while (true)
            {
                ConsoleKeyInfo i = Console.ReadKey(true);
                if (i.Key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (i.Key == ConsoleKey.Backspace)
                {
                    pwd.RemoveAt(pwd.Length - 1);
                    //Console.Write("\b \b");
                }
                else
                {
                    pwd.AppendChar(i.KeyChar);
                    //Console.Write("*");
                }
            }
            return pwd;
        }
    }
}
