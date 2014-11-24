using System;
using Microsoft.SPOT;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Text;
using HA.SPOT.Remoting.Interop;
using System.Reflection;
using System.Collections;


namespace HA.SPOT.Remoting
{
    public class Server
    {
        public int Port { get; protected set; }
        public int Timeout { get; protected set; }

        private ArrayList services;
        private Thread serverThread;
        private bool cancel;

        public Server(int port, int timeout)
        {
            this.services = new ArrayList();
            this.Timeout = timeout;
            this.Port = port;
            this.serverThread = new Thread(RunServer);
            Debug.Print("Web server started on port " + port.ToString());
        }

        public void AddService(object Service)
        {
            this.services.Add(Service);
        }

        public bool Start()
        {
            bool bStarted = true;
            // start server           
            try
            {
                cancel = false;
                serverThread.Start();
                Debug.Print("Started server in thread " + serverThread.GetHashCode().ToString());
            }
            catch
            {   //if there is a problem, maybe due to the fact we did not wait enough
                cancel = true;
                bStarted = false;
            }
            return bStarted;
        }

        private void RunServer()
        {
            using (Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                //set a receive Timeout to avoid too long connection 
                server.ReceiveTimeout = this.Timeout;
                server.Bind(new IPEndPoint(IPAddress.Any, this.Port));
                server.Listen(int.MaxValue);
                while (!cancel)
                {
                    try
                    {

                        using (Socket connection = server.Accept())
                        {
                            if (connection.Poll(-1, SelectMode.SelectRead))
                            {// Create buffer and receive raw bytes.
                                byte[] bytes = new byte[connection.Available];
                                int count = connection.Receive(bytes);
                                Debug.Print("Request received from "
            + connection.RemoteEndPoint.ToString() + " at " + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss"));
                                //stup some time for send timeout as 10s.
                                //necessary to avoid any problem when multiple requests are done the same time.
                                connection.SendTimeout = this.Timeout; ;
                                // Convert to string, will include HTTP headers.
                                string JSON = new string(Encoding.UTF8.GetChars(bytes));

                                try
                                {
                                    RemotingCommand command = RemotingCommand.Deserialize(JSON);
                                    RemotingResponse response = RunCommand(command);
                                    connection.Send(Encoding.UTF8.GetBytes(response.Serialize()));
                                }
                                catch (Exception e)
                                {
                                    RemotingResponse rr = new RemotingResponse(e);
                                    connection.Send(Encoding.UTF8.GetBytes(rr.Serialize()));
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        //this may be due to a bad IP address
                        Debug.Print(e.Message);
                    }
                }
            }
        }

        private object RunCommand(string methodName, Type type, object[] parameters)
        {
            foreach (object obj in this.services)
            {
                if (type.IsInstanceOfType(obj))
                {
                    MethodInfo methodInfo = type.GetMethod(methodName);
                    return methodInfo.Invoke(obj, parameters);
                }
            }
            throw new NotSupportedException();
        }

        private RemotingResponse RunCommand(RemotingCommand command)
        {
            object ret = RunCommand(command.MethodName, command.CommandType, command.Parameters);
            return new RemotingResponse(ret);
        }
    }
}
