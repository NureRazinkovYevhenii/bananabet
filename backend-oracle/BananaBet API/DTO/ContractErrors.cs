using Nethereum.ABI.FunctionEncoding.Attributes;
using System.Numerics;
using Nethereum.Contracts;

namespace BananaBet_API.DTO
{
    [Error("MatchAlreadyExists")]
    public class MatchAlreadyExistsError : IErrorDTO
    {
        [Parameter("uint256", "externalId", 1)]
        public BigInteger ExternalId { get; set; }
    }



[Error("InvalidMatchStatus")]
    public class InvalidMatchStatusError : IErrorDTO
    {
        [Parameter("uint256", "externalId", 1)]
        public BigInteger ExternalId { get; set; }

        [Parameter("uint8", "current", 2)]
        public byte Current { get; set; }

        [Parameter("uint8", "expected", 3)]
        public byte Expected { get; set; }
    }
}
