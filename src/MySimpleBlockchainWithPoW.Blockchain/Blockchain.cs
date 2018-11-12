﻿#region usings

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json;

#endregion

namespace MySimpleBlockchainWithPoW.Blockchain
{
    public class Blockchain
    {
        #region Fields

        private readonly List<Transaction> currentTransactionList = new List<Transaction>();

        private List<Block> blockList = new List<Block>();

        private readonly List<Node> nodes = new List<Node>();

        #endregion

        #region Private properties

        private Block LastBlock
        {
            get
            {
                return this.blockList.Last();
            }
        }

        #endregion

        #region Constructors

        //ctor
        public Blockchain()
        {
            this.NodeId = Guid.NewGuid().ToString().Replace("-", "");
            this.CreateNewBlock(100, string.Empty);
        }

        #endregion

        #region Properties

        public string NodeId { get; }

        #endregion

        #region Private methods

        private bool IsValidBlockList(List<Block> pBlockList)
        {
            Block lastBlock = pBlockList.First();
            int currentIndex = 1;
            while (currentIndex < pBlockList.Count)
            {
                var block = pBlockList.ElementAt(currentIndex);

                if (block.PreviousHash != this.GetHash(lastBlock))
                    return false;

                if (!this.IsValidNonce(lastBlock.Nonce, block.Nonce, lastBlock.PreviousHash))
                    return false;

                lastBlock = block;
                currentIndex++;
            }

            return true;
        }

        private bool ResolveConflicts()
        {
            List<Block> newChain = null;

            foreach (Node node in this.nodes)
            {
                var url = new Uri(node.Address, "/blockchain");
                var request = (HttpWebRequest) WebRequest.Create(url);
                var response = (HttpWebResponse) request.GetResponse();

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var model = new
                    {
                        blockList = new List<Block>(),
                        length = 0
                    };
                    Stream stream = response.GetResponseStream();
                    if (stream != null)
                    {
                        string json = new StreamReader(stream).ReadToEnd();
                        var data = JsonConvert.DeserializeAnonymousType(json, model);

                        if (data.blockList.Count > this.blockList.Count && this.IsValidBlockList(data.blockList))
                        {
                            newChain = data.blockList;
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Unable to get response stream");
                    }
                }
            }

            if (newChain != null)
            {
                this.blockList = newChain;
                return true;
            }

            return false;
        }

        private Block CreateNewBlock(int nonce, string previousHash = null)
        {
            var block = new Block(this.blockList.Count, DateTime.UtcNow, nonce,
                                  previousHash ?? this.GetHash(this.blockList.Last()),
                                  this.currentTransactionList.ToList());

            this.currentTransactionList.Clear();
            this.blockList.Add(block);
            return block;
        }

        private int FindNonce(int lastNonce, string previousHash)
        {
            int nonce = 0;
            while (!this.IsValidNonce(lastNonce, nonce, previousHash))
                nonce++;

            return nonce;
        }

        private bool IsValidNonce(int lastNonce, int nonce, string previousHash)
        {
            string guess = $"{lastNonce}{nonce}{previousHash}";
            string result = Helper.GetSha256Hash(guess);
            return result.StartsWith("000");
        }

        private string GetHash(Block block)
        {
            string blockText = JsonConvert.SerializeObject(block);
            return Helper.GetSha256Hash(blockText);
        }

        #endregion

        #region Public methods

        internal string Mine()
        {
            int nonce = this.FindNonce(this.LastBlock.Nonce, this.LastBlock.PreviousHash);

            this.CreateTransaction("0", this.NodeId, 1);
            Block block = this.CreateNewBlock(nonce /*, _lastBlock.PreviousHash*/);

            var response = new
            {
                Message = "Nowy blok został wygenerowany",
                block.Index,
                Transactions = block.TransactionList.ToArray(),
                block.Nonce,
                block.PreviousHash
            };

            return JsonConvert.SerializeObject(response);
        }

        internal string GetFullChain()
        {
            var response = new
            {
                blockList = this.blockList.ToArray(),
                length = this.blockList.Count
            };

            return JsonConvert.SerializeObject(response);
        }

        internal string Consensus()
        {
            bool replaced = this.ResolveConflicts();
            string message = replaced ? "został zamieniony" : "jest autorytatywny";

            var response = new
            {
                Message = $"Nasz blockchain {message}",
                BlockList = this.blockList
            };

            return JsonConvert.SerializeObject(response);
        }

        internal int CreateTransaction(string from, string to, double amount)
        {
            var transaction = new Transaction
            {
                From = from,
                To = to,
                Amount = amount
            };

            this.currentTransactionList.Add(transaction);

            return this.LastBlock?.Index + 1 ?? 0;
        }

        public string RegisterNode(string url)
        {
            this.nodes.Add(new Node
            {
                Address = new Uri($"http://{url}")
            });

            return JsonConvert.SerializeObject($"Węzeł {url} został zarejestrowany");
        }

        #endregion
    }
}