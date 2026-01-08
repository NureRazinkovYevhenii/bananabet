using System.Numerics;
using BananaBet_API.DTO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Web3;

namespace BananaBet_API.Services
{
    /// <summary>
    /// Validates placeBet transactions on-chain without relying on events.
    /// </summary>
    public class BlockchainTxVerifierService
    {
        private readonly Web3 _web3;
        private readonly ILogger<BlockchainTxVerifierService> _logger;
        private readonly string _contractAddress;
        private const int USDB_DECIMALS = 6;

        public BlockchainTxVerifierService(IConfiguration config, ILogger<BlockchainTxVerifierService> logger)
        {
            _logger = logger;
            _contractAddress = config["CONTRACT_ADDRESS"]?.ToLowerInvariant()
                ?? throw new InvalidOperationException("CONTRACT_ADDRESS is missing");

            var rpcUrl = config["RPC_URL"] ?? throw new InvalidOperationException("RPC_URL is missing");
            _web3 = new Web3(rpcUrl);
        }

        public async Task<VerifiedBetTx?> VerifyPlaceBetAsync(string txHash)
        {
            try
            {
                var tx = await _web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(txHash);
                if (tx == null)
                {
                    _logger.LogWarning("Tx not found: {TxHash}", txHash);
                    return null;
                }

                var receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txHash);
                if (receipt == null || receipt.Status == null || receipt.Status.Value == 0)
                {
                    _logger.LogWarning("Tx not successful: {TxHash}", txHash);
                    return null;
                }

                if (!string.Equals(tx.To, _contractAddress, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Tx {TxHash} not sent to contract", txHash);
                    return null;
                }

                var msg = new PlaceBetFunction();
                try
                {
                    msg.DecodeInput(tx.Input);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to decode input for tx {TxHash}", txHash);
                    return null;
                }

                return new VerifiedBetTx
                {
                    MatchExternalId = msg.ExternalId.ToString(),
                    Pick = (int)msg.Side,
                    Amount = Web3.Convert.FromWei(msg.Amount, USDB_DECIMALS),
                    UserWalletAddress = tx.From ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected verification error for tx {TxHash}", txHash);
                return null;
            }
        }

        public async Task<(bool Valid, string? ExternalId, string User)> VerifyClaimAsync(string txHash)
        {
            try
            {
                var tx = await _web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(txHash);
                if (tx == null) return (false, null, string.Empty);

                var receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txHash);
                if (receipt == null || receipt.Status.Value == 0) return (false, null, string.Empty);

                if (!string.Equals(tx.To, _contractAddress, StringComparison.OrdinalIgnoreCase)) return (false, null, string.Empty);

                var msg = new ClaimFunction();
                try
                {
                    msg.DecodeInput(tx.Input);
                }
                catch { return (false, null, string.Empty); }

                return (true, msg.ExternalId.ToString(), tx.From);
            }
            catch { return (false, null, string.Empty); }
        }

        [Function("placeBet")]
        private class PlaceBetFunction : FunctionMessage
        {
            [Parameter("uint256", "externalId", 1)]
            public BigInteger ExternalId { get; set; }

            [Parameter("uint8", "side", 2)]
            public byte Side { get; set; }

            [Parameter("uint256", "amount", 3)]
            public BigInteger Amount { get; set; }
        }

        [Function("claim")]
        private class ClaimFunction : FunctionMessage
        {
            [Parameter("uint256", "externalId", 1)]
            public BigInteger ExternalId { get; set; }
        }
    }
}

