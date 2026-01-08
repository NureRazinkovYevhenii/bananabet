using BananaBet_API.Models;

namespace BananaBet_API.DTO
{
    public enum ChainTxResult
    {
        Sent,
        AlreadyExists,
        BadStatus,
        Failed
    }

    public record ChainTxResponse(
    ChainTxResult Result,
    string? Tx,
    OnChainMatchStatus? CurrentStatus
);
}
