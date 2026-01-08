using BananaBet_API.DTO;
using BananaBet_API.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using System.Numerics;

namespace BananaBet_API.Services
{
    /// <summary>
    /// Thin wrapper over Nethereum for BananaBet oracle calls.
    /// </summary>
    public class BlockchainClient
    {
        private readonly Web3 _web3;
        private readonly string _contractAddress;
        private readonly ILogger<BlockchainClient> _logger;
        private readonly HexBigInteger _maxPriorityFeePerGas;
        private readonly HexBigInteger _maxFeePerGas;

        public BlockchainClient(IConfiguration config, ILogger<BlockchainClient> logger)
        {
            _logger = logger;

            var privateKey = config["ORACLE_PRIVATE_KEY"] ?? throw new InvalidOperationException("ORACLE_PRIVATE_KEY is missing");
            var rpcUrl = config["RPC_URL"] ?? throw new InvalidOperationException("RPC_URL is missing");
            _contractAddress = config["CONTRACT_ADDRESS"] ?? throw new InvalidOperationException("CONTRACT_ADDRESS is missing");

            var chainId = config.GetValue<long?>("BLOCKCHAIN_CHAIN_ID") ?? 11155111; // Sepolia default
            var account = new Account(privateKey, chainId);
            _web3 = new Web3(account, rpcUrl);

            // EIP-1559 static gas config for Sepolia to avoid tip=0 errors.
            _maxPriorityFeePerGas = new HexBigInteger(Web3.Convert.ToWei(2, UnitConversion.EthUnit.Gwei));
            _maxFeePerGas = new HexBigInteger(Web3.Convert.ToWei(30, UnitConversion.EthUnit.Gwei));
        }

        public async Task<ChainTxResponse> CreateMatchAsync(BigInteger externalId, decimal oddsHome, decimal oddsAway, CancellationToken ct)
        {
            try
            {
                var msg = new CreateMatchFunction
                {
                    ExternalId = externalId,
                    OddsHome = ToUint(oddsHome),
                    OddsAway = ToUint(oddsAway)
                };
                ApplyEip1559(msg);

                var handler = _web3.Eth.GetContractTransactionHandler<CreateMatchFunction>();
                var receipt = await handler.SendRequestAndWaitForReceiptAsync(
                    _contractAddress,
                    msg,
                    cancellationToken: ct
                );

                return new ChainTxResponse(ChainTxResult.Sent, receipt.TransactionHash, OnChainMatchStatus.Created);
            }
            catch (Nethereum.JsonRpc.Client.RpcResponseException ex)
                when (ex.Message.Contains("match exists", StringComparison.OrdinalIgnoreCase))
            {
                var status = await TryGetStatusAsync(externalId, ct);
                return new ChainTxResponse(ChainTxResult.AlreadyExists, null, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "createMatch failed for {ExternalId}", externalId);
                return new ChainTxResponse(ChainTxResult.Failed, null, null);
            }
        }

        public async Task<ChainTxResponse> OpenMatchAsync(BigInteger externalId, CancellationToken ct)
        {
            try
            {
                var msg = new OpenMatchFunction { ExternalId = externalId };
                ApplyEip1559(msg);

                var handler = _web3.Eth.GetContractTransactionHandler<OpenMatchFunction>();
                var receipt = await handler.SendRequestAndWaitForReceiptAsync(
                    _contractAddress,
                    msg,
                    cancellationToken: ct
                );

                return new ChainTxResponse(ChainTxResult.Sent, receipt.TransactionHash, OnChainMatchStatus.Open);
            }
            catch (Nethereum.ABI.FunctionEncoding.SmartContractRevertException ex)
                when (ex.Message.Contains("InvalidMatchStatus", StringComparison.OrdinalIgnoreCase))
            {
                var status = await TryGetStatusAsync(externalId, ct);
                return new ChainTxResponse(ChainTxResult.BadStatus, null, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "openMatch failed for {ExternalId}", externalId);
                return new ChainTxResponse(ChainTxResult.Failed, null, null);
            }
        }

        public async Task<ChainTxResponse> CloseMatchAsync(BigInteger externalId, CancellationToken ct)
        {
            try
            {
                var msg = new CloseMatchFunction
                {
                    ExternalId = externalId
                };
                ApplyEip1559(msg);

                var handler =
                    _web3.Eth.GetContractTransactionHandler<CloseMatchFunction>();

                var receipt = await handler.SendRequestAndWaitForReceiptAsync(
                    _contractAddress,
                    msg,
                    cancellationToken: ct
                );

                return new ChainTxResponse(ChainTxResult.Sent, receipt.TransactionHash, OnChainMatchStatus.Closed);
            }
            catch (Nethereum.ABI.FunctionEncoding.SmartContractRevertException ex)
                when (ex.Message.Contains("InvalidMatchStatus", StringComparison.OrdinalIgnoreCase))
            {
                var status = await TryGetStatusAsync(externalId, ct);
                return new ChainTxResponse(ChainTxResult.BadStatus, null, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "closeMatch failed for {ExternalId}", externalId);
                return new ChainTxResponse(ChainTxResult.Failed, null, null);
            }
        }

        public async Task<ChainTxResponse> ResolveMatchAsync(BigInteger externalId, byte result,CancellationToken ct)
        {
            try
            {
                var msg = new ResolveMatchFunction
                {
                    ExternalId = externalId,
                    Result = result
                };
                ApplyEip1559(msg);

                var handler =
                    _web3.Eth.GetContractTransactionHandler<ResolveMatchFunction>();

                var receipt = await handler.SendRequestAndWaitForReceiptAsync(
                    _contractAddress,
                    msg,
                    cancellationToken: ct
                );

                return new ChainTxResponse(ChainTxResult.Sent, receipt.TransactionHash, OnChainMatchStatus.Resolved);
            }
            catch (Nethereum.ABI.FunctionEncoding.SmartContractRevertException ex)
                when (ex.Message.Contains("InvalidMatchStatus", StringComparison.OrdinalIgnoreCase))
            {
                var status = await TryGetStatusAsync(externalId, ct);
                return new ChainTxResponse(ChainTxResult.BadStatus, null, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "resolveMatch failed for {ExternalId}", externalId);
                return new ChainTxResponse(ChainTxResult.Failed, null, null);
            }
        }

        public async Task<(bool Found, GetMatchOutput? Data)> GetMatchAsync(BigInteger externalId, CancellationToken ct)
        {
            try
            {
                var query = new GetMatchFunction { ExternalId = externalId };
                var handler = _web3.Eth.GetContractQueryHandler<GetMatchFunction>();
                var dto = await handler.QueryDeserializingToObjectAsync<GetMatchOutput>(
                    query, _contractAddress, block: null);

                if (dto == null || dto.ExternalId == 0)
                    return (false, null);

                return (true, dto);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch match {ExternalId}", externalId);
                return (false, null);
            }
        }

        public async Task<List<OnChainBet>> GetBetsByMatchAsync(BigInteger externalId, CancellationToken ct)
        {
            try
            {
                var query = new GetBetsByMatchFunction { ExternalId = externalId };
                var handler = _web3.Eth.GetContractQueryHandler<GetBetsByMatchFunction>();
                var dto = await handler.QueryDeserializingToObjectAsync<GetBetsByMatchOutput>(
                    query, _contractAddress, block: null);

                if (dto?.Bets == null)
                    return new List<OnChainBet>();

                const int USDB_DECIMALS = 6;
                return dto.Bets.Select((b, i) => new OnChainBet
                {
                    User = b.User,
                    Amount = Web3.Convert.FromWei(b.Amount, USDB_DECIMALS),
                    PlayAmount = Web3.Convert.FromWei(b.PlayAmount, USDB_DECIMALS),
                    Side = b.Side,
                    Status = b.Status,
                    OnChainIndex = i
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch betsByMatch {ExternalId}", externalId);
                return new List<OnChainBet>();
            }
        }

        private static BigInteger ToUint(decimal value)
        {
            // Scale odds to 3 decimals (PRECISION = 1000) for on-chain integer representation.
            return new BigInteger(Math.Round(value * 1_000m, MidpointRounding.AwayFromZero));
        }

        private void ApplyEip1559(FunctionMessage msg)
        {
            // Explicit EIP-1559 fees to avoid "gas tip cap 0" and stay deterministic.
            msg.MaxPriorityFeePerGas = _maxPriorityFeePerGas;
            msg.MaxFeePerGas = _maxFeePerGas;
            msg.GasPrice = null; // disable legacy pricing
        }

        private async Task<OnChainMatchStatus?> TryGetStatusAsync(BigInteger externalId, CancellationToken ct)
        {
            try
            {
                var query = new GetMatchFunction { ExternalId = externalId };
                var handler = _web3.Eth.GetContractQueryHandler<GetMatchFunction>();
                var dto = await handler
                    .QueryDeserializingToObjectAsync<GetMatchOutput>(
                        query, _contractAddress, block: null);

                if (dto == null || dto.ExternalId == 0)
                    return null;

                return (OnChainMatchStatus)dto.Status;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch on-chain status for {ExternalId}", externalId);
                return null;
            }
        }

        [Function("createMatch")]
        private class CreateMatchFunction : FunctionMessage
        {
            [Parameter("uint256", "externalId", 1)]
            public BigInteger ExternalId { get; set; }

            [Parameter("uint256", "oddsHome", 2)]
            public BigInteger OddsHome { get; set; }

            [Parameter("uint256", "oddsAway", 3)]
            public BigInteger OddsAway { get; set; }
        }

        [Function("openMatch")]
        private class OpenMatchFunction : FunctionMessage
        {
            [Parameter("uint256", "externalId", 1)]
            public BigInteger ExternalId { get; set; }
        }

        [Function("closeMatch")]
        private class CloseMatchFunction : FunctionMessage
        {
            [Parameter("uint256", "externalId", 1)]
            public BigInteger ExternalId { get; set; }
        }

        [Function("resolveMatch")]
        private class ResolveMatchFunction : FunctionMessage
        {
            [Parameter("uint256", "externalId", 1)]
            public BigInteger ExternalId { get; set; }

            [Parameter("uint8", "result", 2)]
            public byte Result { get; set; }
        }

        [Function("matches", typeof(GetMatchOutput))]
        private class GetMatchFunction : FunctionMessage
        {
            [Parameter("uint256", "externalId", 1)]
            public BigInteger ExternalId { get; set; }
        }

        [FunctionOutput]
        public class GetMatchOutput : IFunctionOutputDTO
        {
            [Parameter("uint256", "externalId", 1)]
            public BigInteger ExternalId { get; set; }

            [Parameter("uint256", "oddsHome", 2)]
            public BigInteger OddsHome { get; set; }

            [Parameter("uint256", "oddsAway", 3)]
            public BigInteger OddsAway { get; set; }

            [Parameter("uint256", "totalHome", 4)]
            public BigInteger TotalHome { get; set; }

            [Parameter("uint256", "totalAway", 5)]
            public BigInteger TotalAway { get; set; }

            [Parameter("uint256", "matchedHome", 6)]
            public BigInteger MatchedHome { get; set; }

            [Parameter("uint256", "matchedAway", 7)]
            public BigInteger MatchedAway { get; set; }

            [Parameter("uint8", "status", 8)]
            public byte Status { get; set; }

            [Parameter("uint8", "result", 9)]
            public byte Result { get; set; }

            [Parameter("bool", "matched", 10)]
            public bool Matched { get; set; }
        }

        [Function("getBetsByMatch", typeof(GetBetsByMatchOutput))]
        private class GetBetsByMatchFunction : FunctionMessage
        {
            [Parameter("uint256", "externalId", 1)]
            public BigInteger ExternalId { get; set; }
        }

        [FunctionOutput]
        public class GetBetsByMatchOutput : IFunctionOutputDTO
        {
            [Parameter("tuple[]", "bets", 1)]
            public List<BetOutput> Bets { get; set; } = new();
        }

        public class BetOutput
        {
            [Parameter("address", "user", 1)]
            public string User { get; set; } = string.Empty;

            [Parameter("uint256", "amount", 2)]
            public BigInteger Amount { get; set; }

            [Parameter("uint256", "playAmount", 3)]
            public BigInteger PlayAmount { get; set; }

            [Parameter("uint8", "side", 4)]
            public byte Side { get; set; }

            [Parameter("uint8", "status", 5)]
            public byte Status { get; set; }
        }

        public class OnChainBet
        {
            public string User { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public decimal PlayAmount { get; set; }
            public byte Side { get; set; }
            public byte Status { get; set; }
            public int OnChainIndex { get; set; }
        }
    }
}

