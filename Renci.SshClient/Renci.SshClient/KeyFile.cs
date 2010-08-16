﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Renci.SshClient.Security;

namespace Renci.SshClient
{
    public class KeyFile
    {
        private Regex _beginKeyLine = new Regex(@"----[ ]*BEGIN (?<keyName>.+) PRIVATE KEY[ ]*----");
        private Regex _headerLine = new Regex(@"(?<headerTag>[^:]{1,64}):[ ](?<headerValue>[^:]+(?<continue>\\)?)");
        private Regex _headerLineContinue = new Regex(@"(?<headerValue>[^:]+(?<continue>\\)?)");
        private Regex _endKeyLine = new Regex(@"----[ ]*END (?<keyName>.+) PRIVATE KEY[ ]*----");

        private CryptoPrivateKey _key;

        public string AlgorithmName
        {
            get
            {
                return this._key.Name;
            }
        }

        public IEnumerable<byte> PublicKey
        {
            get
            {
                return this._key.GetPublicKey().GetBytes();
            }
        }

        public IEnumerable<byte> GetSignature(IEnumerable<byte> sessionId)
        {
            return this._key.GetSignature(sessionId);
        }

        public KeyFile()
        {

        }

        public void Open(Stream privateKey, string passPhrase)
        {
            var headerTag = string.Empty;
            var headerValue = string.Empty;
            var headerValueContinue = false;
            var data = new StringBuilder();
            var keyName = string.Empty;

            var fileLine = string.Empty;

            using (StreamReader sr = new StreamReader(privateKey))
            {
                while ((fileLine = sr.ReadLine()) != null)
                {
                    var match = _beginKeyLine.Match(fileLine);
                    if (match.Success)
                    {
                        keyName = match.Result("${keyName}");
                        continue;
                    }

                    match = _endKeyLine.Match(fileLine);
                    if (match.Success)
                    {
                        var endKeyName = match.Result("${keyName}");
                        if (!endKeyName.Equals(keyName))
                            throw new InvalidDataException("Invalid data key file.");
                        break;
                    }


                    //  Ignore everything if BEGIN was not found yet
                    if (string.IsNullOrEmpty(keyName))
                    {
                        continue;
                    }

                    match = _headerLine.Match(fileLine);
                    if (match.Success)
                    {
                        headerTag = match.Result("${headerTag}");
                        headerValue = match.Result("${headerValue}");
                        if (match.Result("${continue}") == @"\")
                        {
                            headerValueContinue = true;
                        }
                        else
                        {
                            headerValueContinue = false;
                        }
                        continue;
                    }

                    if (headerValueContinue)
                    {
                        headerValue += fileLine;
                        if (match.Result("${continue}") == @"\")
                        {
                            headerValueContinue = true;
                        }
                        else
                        {
                            headerValueContinue = false;
                        }
                        continue;
                    }

                    data.Append(fileLine);
                }
            }

            if (string.IsNullOrEmpty(keyName))
            {
                throw new InvalidDataException("Invalid Public key file");
            }

            switch (keyName)
            {
                case "RSA":
                    this._key = new CryptoPrivateKeyRsa();

                    break;
                case "DSA":
                    this._key = new CryptoPrivateKeyDss();
                    break;
                default:
                    throw new NotSupportedException(string.Format("Key '{0}' is not supported.", keyName));
            }

            this._key.Load(System.Convert.FromBase64String(data.ToString()), passPhrase.GetSshBytes());
        }

        public void Open(string fileName)
        {
            this.Open(File.Open(fileName, FileMode.Open), null);
        }

        public void Open(string fileName, string passPhrase)
        {
            this.Open(File.Open(fileName, FileMode.Open), passPhrase);
        }

    }
}