﻿using MinerProxy2.Interfaces;
using MinerProxy2.Network;
using MinerProxy2.Network.Sockets;
using MinerProxy2.Pools;
using Serilog;
using System;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.IO;

namespace MinerProxy2.Coins
{
    public class EthereumPoolHandler : ICoinHandlerPool
    {
        private MinerServer _minerServer;
        private PoolClient _pool;
        private PoolInstance _poolInfo;

        public void BroadcastToMiners(byte[] data)
        {
            //This will probably be removed, I don't think it's necessary..
            _minerServer.BroadcastToMiners(data);
        }

        public void DoPoolLogin(PoolClient poolClient)
        {
            //Log.Information("Sending login to pool");
            //_pool.SendToPool(Encoding.ASCII.GetBytes("{\"worker\": \"" + "eth1.0" + "\", \"jsonrpc\": \"2.0\", \"params\": [\"" + _poolInfo.GetCurrentPool().poolWallet + "." + _poolInfo.GetCurrentPool().poolWorkerName + "\", \"x\"], \"id\": 2, \"method\": \"eth_submitLogin\"}\r\n"));

        }

        public void PoolConnected(PoolClient poolClient)
        {
            Log.Information("Pool connected: " + poolClient.poolEndPoint);
        }

        public void PoolDataReceived(byte[] data, PoolClient poolClient)
        {
            //process pool data here
            //such as pushing work to all miners
            //or letting miners know the share they submitted was accepted/rejected.
            //Log.Information("Pool " + poolClient.poolEndPoint + " sent: " + Encoding.ASCII.GetString(data));

            //This is just for testing with 1 miner.
            //_minerServer.BroadcastToMiners(data);

            string test = Encoding.ASCII.GetString(data);

            foreach (string s in test.Split('\r', '\n'))
            {
                if (s.Length <= 1)
                    continue;

                dynamic dyn = JsonConvert.DeserializeObject(s);

                //Log.Information("Split: " + s);
                if (Helpers.JsonHelper.DoesJsonObjectExist(dyn.id))
                {
                    //Log.Information("dyn.id: " + dyn.id);
                    switch ((int)dyn.id)
                    {
                        case 0:
                            Log.Information("Server sent new work");
                            byte[] work = Encoding.ASCII.GetBytes(s);
                            //_pool.currentWork = new byte[work.Length];
                            _pool.currentWork = work;
                            //Log.Debug("currentWork: " + _pool.currentWork.Length);
                            
                            _minerServer.BroadcastToMiners(work);
                            break;

                        case 1:
                            Log.Information("Case 1: " + s);
                            break;

                        case 2:
                            Log.Information("Server sent Auth success");
                            _minerServer.BroadcastToMiners(Encoding.ASCII.GetBytes(s));
                            break;

                        case 3:
                            Log.Information("Server sent eth_getWork");

                            if (_pool.currentWork != Encoding.ASCII.GetBytes(s))
                                _minerServer.BroadcastToMiners(data);

                            _pool.currentWork = Encoding.ASCII.GetBytes(s);
                            break;

                        case 4:
                            Log.Information("Share accepted by pool");
                            _minerServer.BroadcastToMiners(Encoding.ASCII.GetBytes(s));
                            break;

                        case 5:
                            Log.Information("Case 5: " + s);
                            break;

                        case 6:
                            Log.Information("Hashrate accepted by pool?");
                            _minerServer.BroadcastToMiners(Encoding.ASCII.GetBytes(s));
                            break;

                        default:
                            Log.Information("Unhandled: " + s);
                            break;
                    }
                } /* else if (dyn.error != null && dyn.result == null)
            {
                Log.Error("Server sent Error: " + dyn.error.code + ": " + dyn.error.message);
            }
            */

            }

        }

        public void PoolDisconnected(PoolClient poolClient)
        {
            Log.Information("Pool disconnected: " + poolClient.poolEndPoint);
        }

        public void PoolError(Exception exception, PoolClient poolClient)
        {
            Log.Error(exception, "Pool Error");
        }

        public void SendToMiner(byte[] data, TcpConnection connection)
        {
            _minerServer.SendToMiner(data, connection);
        }

        public void SendToPool(byte[] data)
        {
            _pool.SendToPool(data);
        }

        public void SetMinerServer(MinerServer minerServer)
        {
            _minerServer = minerServer;
        }

        public void SetPoolClient(PoolClient pool)
        {
            _pool = pool;
        }

        public void SetPoolInfo(PoolInstance poolInfo)
        {
            this._poolInfo = poolInfo;
        }
    }
}