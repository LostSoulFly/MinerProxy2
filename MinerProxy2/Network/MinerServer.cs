﻿/* MinerProxy2 programmed by LostSoulfly.
   GNU General Public License v3.0 */

using MinerProxy2.Helpers;
using MinerProxy2.Interfaces;
using MinerProxy2.Miners;
using MinerProxy2.Network.Connections;
using MinerProxy2.Network.Sockets;
using Serilog;

namespace MinerProxy2.Network
{
    public class MinerServer
    {
        private readonly PoolClient _poolClient;
        private readonly Server minerServer;
        private MinerManager _minerManager;
        private int port;

        public ICoinHandlerMiner _coinHandler;

        public int GetNumberOfConnections { get { return minerServer.GetNumberOfConnections; } }

        public MinerServer(int port, PoolClient pool, MinerManager minerManager, ICoinHandlerMiner coinHandler)
        {
            Log.Debug("MinerServer initialized for {0}", pool.poolEndPoint);

            this._minerManager = minerManager;
            this.port = port;

            _poolClient = pool;
            _coinHandler = coinHandler;
            _coinHandler.SetMinerServer(this);
            _coinHandler.SetPoolClient(_poolClient);

            minerServer = new Server();
            minerServer.OnClientDataReceived += MinerServer_OnClientDataReceived;
            minerServer.OnClientConnected += MinerServer_OnClientConnected;
            minerServer.OnClientDisconnected += MinerServer_OnClientDisconnected;
            minerServer.OnClientError += MinerServer_OnClientError;
        }

        private void MinerServer_OnClientConnected(object sender, ClientConnectedArgs e)
        {
            _poolClient.CheckPoolConnection();
            Log.Information("{0} has connected for [{1}] on port {2}", e.connection.endPoint.ToString(), _poolClient.poolWorkerName, this.port);
            _coinHandler.MinerConnected(e.connection);
        }

        private void MinerServer_OnClientDataReceived(object sender, ClientDataReceivedArgs e)
        {
            //Log.Information(Encoding.ASCII.GetString(e.Data));
            _coinHandler.MinerDataReceived(e.Data, e.connection);
        }

        private void MinerServer_OnClientDisconnected(object sender, ClientDisonnectedArgs e)
        {
            Miner miner = _minerManager.GetMiner(e.connection);

            Log.Information("{0} has disconnected for {1}", miner.workerIdentifier, _poolClient.poolEndPoint);

            if (miner != null)
                _minerManager.RemoveMiner(miner);

            _poolClient.CheckPoolConnection();
        }

        private void MinerServer_OnClientError(object sender, ClientErrorArgs e)
        {
            Miner miner = _minerManager.GetMiner(e.connection);

            Log.Information("{0} has disconnected for {1}", miner.workerIdentifier, _poolClient.poolEndPoint);

            if (miner != null)
                _minerManager.RemoveMiner(miner);

            _poolClient.CheckPoolConnection();
        }

        public void BroadcastToMiners(byte[] data)
        {
            minerServer.BroadcastToMiners(data);
        }

        public void BroadcastToMiners(string data)
        {
            minerServer.BroadcastToMiners(data.GetBytes());
        }

        public void ListenForMiners()
        {
            Log.Information("{0} MinerServer listening on {1}.", _poolClient.poolEndPoint, this.port);
            minerServer.Start(port);
        }

        public void SendToMiner(byte[] data, TcpConnection connection)
        {
            minerServer.Send(data, connection);
        }

        public void SendToMiner(string data, TcpConnection connection)
        {
            minerServer.Send(data.GetBytes(), connection);
        }

        public void SendToPool(byte[] data)
        {
            Log.Debug("MinerServer SendToPool");
            _poolClient.SendToPool(data);
        }

        public void SendToPool(string data)
        {
            Log.Debug("MinerServer SendToPool");
            _poolClient.SendToPool(data.GetBytes());
        }

        public void StopListening()
        {
            minerServer.Stop();
        }
    }
}