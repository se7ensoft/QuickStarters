﻿using Newtonsoft.Json;
using P2PNET.TransportLayer;
using P2PNET.TransportLayer.EventArgs;
using P2PTank.Entities.P2PMessages;
using System;
using System.Threading.Tasks;
using WaveEngine.Framework;
using WaveEngine.Networking.P2P;
using System.Collections.Generic;

namespace P2PTank.Managers
{
    public class P2PManager : Component
    {
        private Peer2Peer peer2peer;

        public event EventHandler<PeerChangeEventArgs> PeerChange;
        public event EventHandler<MsgReceivedEventArgs> MsgReceived;

        public P2PManager()
        {
            this.peer2peer = new Peer2Peer();

            this.peer2peer.PeerChange += this.OnPeerChanged;
            this.peer2peer.MsgReceived += this.OnMsgReceived;
        }
        
        protected override void Removed()
        {
            this.peer2peer.PeerChange -= this.OnPeerChanged;
            this.peer2peer.MsgReceived -= this.OnMsgReceived;

            base.Removed();
        }

        public async Task StartAsync()
        {
            await peer2peer.StartAsync();
        }

        public async Task SendMessage(string ipAddress, string message, TransportType transportType)
        {
            await peer2peer.SendMessage(ipAddress, message, transportType);
        }

        public async Task SendBroadcastAsync(string message)
        {
            await peer2peer.SendBroadcastAsync(message);
        }

        public string CreateMessage(P2PMessageType messageType, object content)
        {
            var contentSerialized = JsonConvert.SerializeObject(content);

            return string.Format("{0}/{1}", messageType, contentSerialized);
        }

        public Dictionary<P2PMessageType, object> ReadMessage(string message)
        {
            Dictionary<P2PMessageType, object> messageObject = new Dictionary<P2PMessageType, object>();
            var result = message.Split('/');

            P2PMessageType messageType;
            Enum.TryParse(result[0], out messageType);

            switch(messageType)
            {
                case P2PMessageType.CreatePlayer:
                    messageObject.Add(
                        P2PMessageType.CreatePlayer, 
                        JsonConvert.DeserializeObject<CreatePlayerMessage>(result[1]));
                    break;
                case P2PMessageType.Move:
                    messageObject.Add(
                        P2PMessageType.Move, 
                        JsonConvert.DeserializeObject<MoveMessage>(result[1]));
                    break;
                case P2PMessageType.Rotate:
                    messageObject.Add(
                        P2PMessageType.Rotate,
                        JsonConvert.DeserializeObject<RotateMessage>(result[1]));
                    break;
                case P2PMessageType.BarrelRotate:
                    messageObject.Add(
                        P2PMessageType.BarrelRotate,
                        JsonConvert.DeserializeObject<BarrelRotate>(result[1]));
                    break;
                case P2PMessageType.Shoot:
                    messageObject.Add(
                        P2PMessageType.Shoot, 
                        JsonConvert.DeserializeObject<ShootMessage>(result[1]));
                    break;
                case P2PMessageType.DestroyPlayer:
                    messageObject.Add(
                        P2PMessageType.DestroyPlayer, 
                        JsonConvert.DeserializeObject<DestroyPlayerMessage>(result[1]));
                    break;
                case P2PMessageType.BulletCreate:
                    messageObject.Add(
                        P2PMessageType.BulletCreate,
                        JsonConvert.DeserializeObject<BulletCreateMessage>(result[1]));
                    break;
                case P2PMessageType.BulletMove:
                    messageObject.Add(
                        P2PMessageType.BulletMove,
                        JsonConvert.DeserializeObject<BulletMoveMessage>(result[1]));
                    break;
                case P2PMessageType.BulletDestroy:
                    messageObject.Add(
                        P2PMessageType.BulletDestroy,
                        JsonConvert.DeserializeObject<BulletDestroyMessage>(result[1]));
                    break;
            }

            return messageObject;
        }

        private void OnMsgReceived(object sender, MsgReceivedEventArgs e)
        {
            this.MsgReceived?.Invoke(this, e);
        }

        private void OnPeerChanged(object sender, PeerChangeEventArgs e)
        {
            this.PeerChange?.Invoke(this, e);
        }
    }
}