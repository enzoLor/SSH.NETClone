﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Renci.SshClient.Messages.Transport;
using System.Security.Cryptography;
using System.Numerics;
using Renci.SshClient.Messages;
using Renci.SshClient.Common;
using System.Diagnostics;

namespace Renci.SshClient.Security
{
    /// <summary>
    /// Represents "diffie-hellman-group-exchange-sha1" algorithm implementation.
    /// </summary>
    internal class KeyExchangeDiffieHellmanGroupExchangeSha1 : KeyExchangeDiffieHellman
    {
        /// <summary>
        /// Gets algorithm name.
        /// </summary>
        public override string Name
        {
            get { return "diffie-hellman-group-exchange-sha1"; }
        }

        /// <summary>
        /// Calculates key exchange hash value.
        /// </summary>
        /// <returns>
        /// Key exchange hash.
        /// </returns>
        protected override IEnumerable<byte> CalculateHash()
        {
            var hashData = new _ExchangeHashData
            {
                ClientVersion = this.Session.ClientVersion,
                ServerVersion = this.Session.ServerVersion,
                ClientPayload = this._clientPayload,
                ServerPayload = this._serverPayload,
                HostKey = this._hostKey,
                MinimumGroupSize = 1024,
                PreferredGroupSize = 1024,
                MaximumGroupSize = 1024,
                Prime = this._prime,
                SubGroup = this._group,
                ClientExchangeValue = this._clientExchangeValue,
                ServerExchangeValue = this._serverExchangeValue,
                SharedKey = this.SharedKey,
            }.GetBytes();

            return this.Hash(hashData);
        }

        /// <summary>
        /// Starts key exchange algorithm
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="message">Key exchange init message.</param>
        public override void Start(Session session, KeyExchangeInitMessage message)
        {
            base.Start(session, message);

            this.Session.RegisterMessage<KeyExchangeDhGroupExchangeGroup>();
            this.Session.RegisterMessage<KeyExchangeDhGroupExchangeReply>();

            this.Session.MessageReceived += Session_MessageReceived;

            //  1. send SSH_MSG_KEY_DH_GEX_REQUEST
            this.Session.SendMessage(new KeyExchangeDhGroupExchangeRequest(1024, 1024, 1024));
        }

        /// <summary>
        /// Finishes key exchange algorithm.
        /// </summary>
        public override void Finish()
        {
            base.Finish();

            this.Session.MessageReceived -= Session_MessageReceived;
        }

        private void Session_MessageReceived(object sender, MessageEventArgs<Message> e)
        {
            var groupMessage = e.Message as KeyExchangeDhGroupExchangeGroup;

            if (groupMessage != null)
            {
                //  Unregister message once received
                this.Session.UnRegisterMessage<KeyExchangeDhGroupExchangeGroup>();

                //  2. Receive SSH_MSG_KEX_DH_GEX_GROUP
                this._prime = groupMessage.SafePrime;
                this._group = groupMessage.SubGroup;

                this.PopulateClientExchangeValue();

                //  3. Send SSH_MSG_KEX_DH_GEX_INIT
                this.Session.SendMessage(new KeyExchangeDhGroupExchangeInit(this._clientExchangeValue));

            }
            var replyMessage = e.Message as KeyExchangeDhGroupExchangeReply;

            if (replyMessage != null)
            {
                //  Unregister message once received
                this.Session.UnRegisterMessage<KeyExchangeDhGroupExchangeReply>();

                this.HandleServerDhReply(replyMessage.HostKey, replyMessage.F, replyMessage.Signature);
            }
        }
        
        private class _ExchangeHashData : SshData
        {
            public string ServerVersion { get; set; }

            public string ClientVersion { get; set; }

            public string ClientPayload { get; set; }

            public string ServerPayload { get; set; }

            public string HostKey { get; set; }

            public UInt32 MinimumGroupSize { get; set; }

            public UInt32 PreferredGroupSize { get; set; }

            public UInt32 MaximumGroupSize { get; set; }

            public BigInteger Prime { get; set; }

            public BigInteger SubGroup { get; set; }

            public BigInteger ClientExchangeValue { get; set; }

            public BigInteger ServerExchangeValue { get; set; }

            public BigInteger SharedKey { get; set; }

            protected override void LoadData()
            {
                throw new System.NotImplementedException();
            }

            protected override void SaveData()
            {
                this.Write(this.ClientVersion);
                this.Write(this.ServerVersion);
                this.Write(this.ClientPayload);
                this.Write(this.ServerPayload);
                this.Write(this.HostKey);
                this.Write(this.MinimumGroupSize);
                this.Write(this.PreferredGroupSize);
                this.Write(this.MaximumGroupSize);
                this.Write(this.Prime);
                this.Write(this.SubGroup);
                this.Write(this.ClientExchangeValue);
                this.Write(this.ServerExchangeValue);
                this.Write(this.SharedKey);
            }
        }
    }
}
