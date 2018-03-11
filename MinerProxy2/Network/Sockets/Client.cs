﻿using Serilog;
using System;
using System.Net.Sockets;
using System.Text;

namespace MinerProxy2.Network.Sockets
{
    public class Client
    {
        private readonly Socket clientSocket = new Socket
               (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private const int BUFFER_SIZE = 2048;
        private byte[] buffer = new byte[BUFFER_SIZE];

        private string host;
        private int port;

        public event EventHandler<ServerDataReceivedArgs> OnServerDataReceived;

        public event EventHandler<ServerErrorArgs> OnServerError;

        public event EventHandler<ServerConnectedArgs> OnServerConnected;

        public event EventHandler<ServerDisonnectedArgs> OnServerDisconnected;

        //todo https://docs.microsoft.com/en-us/dotnet/framework/network-programming/asynchronous-client-socket-example
        // Need to make the client async as well, BegineREceive, etc...

        //RaiseServerDataReceived?.Invoke(this, new ServerDataReceivedArgs(data, clientSocket));

        public void Connect()
        {
            clientSocket.BeginConnect(host, port,
                new AsyncCallback(ConnectCallback), clientSocket);
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket socket = (Socket)ar.AsyncState;

                // Complete the connection.
                socket.EndConnect(ar);

                Log.Information("Socket connected to {0}", socket.RemoteEndPoint.ToString());

                OnServerConnected?.Invoke(this, new ServerConnectedArgs(socket));

                socket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);
            }
            catch (Exception exception)
            {
                Log.Error(exception, "ConnectCallback");
            }
        }
        
        public void SendToPool(byte[] data)
        {
            Log.Debug("Client SendToPool: " + Encoding.ASCII.GetString(data));
            // Begin sending the data to the remote device.
            try
            {
                this.clientSocket.BeginSend(data, 0, data.Length, SocketFlags.None,
                    new AsyncCallback(SendCallback), clientSocket);
            } catch (Exception ex)
            {
                //Reconnect to pool?
                Log.Error(ex, "SendToPool");
            }
        }
        
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = client.EndSend(ar);
                Log.Debug("Sent {0} bytes to server.", bytesSent);
            }
            catch (Exception exception)
            {
                Log.Error(exception, "SendCallback");
            }
        }

        private void Receive(Socket socket)
        {
            try
            {
                // Begin receiving the data from the remote device.
                socket.BeginReceive(buffer, 0, BUFFER_SIZE, 0,
                    new AsyncCallback(ReceiveCallback), socket);
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Receive");
            }
        }

        private void ReceiveCallback(IAsyncResult AR)
        {
            Socket socket = (Socket)AR.AsyncState;
            int received;

            try
            {
                received = socket.EndReceive(AR);
            }
            catch (SocketException ex)
            {
                //Log.Error(ex, "Client forcefully disconnected");
                // Don't shutdown because the socket may be disposed and its disconnected anyway.
                OnServerDisconnected?.Invoke(this, new ServerDisonnectedArgs(socket));
                socket.Close();
                return;
            }

            byte[] recBuf = new byte[received];
            Array.Copy(buffer, recBuf, received);

            //Log.Debug("Pool sent: " + Encoding.ASCII.GetString(recBuf));

            OnServerDataReceived?.Invoke(this, new ServerDataReceivedArgs(recBuf, socket));

            try
            {
                socket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);
            }
            catch (Exception ex)
            {
                OnServerError?.Invoke(this, new ServerErrorArgs(ex, socket));
                Log.Error(ex, "Pool BeginReceive Error");
            }
        }
    }
}