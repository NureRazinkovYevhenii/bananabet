namespace BananaBet_API.Models
{
    public enum MatchPipelineStatus
    {
        Fetched,
        OddsCalculated,
        ReadyForChain,
        OnChain,
        Open,
        Closed,
        Resolved
    }

    public enum OnChainMatchStatus : byte
    {
        Created = 0,
        Open = 1,
        Closed = 2,
        Resolved = 3
    }


}

