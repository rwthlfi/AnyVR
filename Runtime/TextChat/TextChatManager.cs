// AnyVR is a multiuser, multiplatform XR framework.
// Copyright (C) 2024 Engineering Hydrology, RWTH Aachen University.
// 
// AnyVR is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published
// by the Free Software Foundation, either version 3 of the License,
// or (at your option) any later version.
// 
// AnyVR is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANT-
// ABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with AnyVR.
// If not, see <https://www.gnu.org/licenses/>.

using System;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

namespace AnyVR.TextChat
{
    public class TextChatManager : NetworkBehaviour
    {
        private readonly CircularBuffer<TextMessage> _buffer = new(100);

        private void Awake()
        {
            InitSingleton();
        }

        public event Action<TextMessage> TextMessageReceived;

        public event Action MessagesSynced;

        public override void OnStartClient()
        {
            base.OnStartClient();
            RequestBuffer();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _buffer.Push(new TextMessage(-1, "Start of chat"));
        }

        [ServerRpc(RequireOwnership = false)]
        public void SendTextMessage(string msg, NetworkConnection sender = null)
        {
            if (sender == null)
            {
                return;
            }

            TextMessage tm = new(sender.ClientId, msg);
            _buffer.Push(tm);
            ReceiveTextMessage(tm);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestBuffer(NetworkConnection conn = null)
        {
            if (conn == null)
            {
                return;
            }

            TextMessage[] buffer = _buffer.GetAll();
            SyncBuffer(conn, buffer);
        }

        [TargetRpc]
        private void SyncBuffer(NetworkConnection conn, TextMessage[] buffer)
        {
            _buffer.Clear();
            foreach (TextMessage textMessage in buffer)
            {
                _buffer.Push(textMessage);
            }

            MessagesSynced?.Invoke();
        }

        [ObserversRpc]
        private void ReceiveTextMessage(TextMessage msg)
        {
            _buffer.Push(msg);
            TextMessageReceived?.Invoke(msg);
        }

        public CircularBuffer<TextMessage> GetBuffer()
        {
            return _buffer;
        }

        #region Singleton

        internal static TextChatManager s_instance;

        private void InitSingleton()
        {
            if (s_instance != null)
            {
                Debug.LogWarning("Instance of TextChatManager already exists!");
                Destroy(this);
            }

            s_instance = this;
        }

        #endregion
    }

    public struct TextMessage
    {
        public readonly int SenderId;
        public readonly string Message;

        public TextMessage(int senderId, string message)
        {
            SenderId = senderId;
            Message = message;
        }
    }
}
