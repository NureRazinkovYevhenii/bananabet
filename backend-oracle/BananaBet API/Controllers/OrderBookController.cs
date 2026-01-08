using System.Numerics;
using BananaBet_API.DTO;
using BananaBet_API.Services;
using Microsoft.AspNetCore.Mvc;
using Nethereum.Util;
using Nethereum.Web3;

namespace BananaBet_API.Controllers
{
    [ApiController]
    [Route("api/orderbook")]
    public class OrderBookController : ControllerBase
    {
        private readonly BlockchainClient _blockchain;
        const int USDB_DECIMALS = 6;

        public OrderBookController(BlockchainClient blockchain)
        {
            _blockchain = blockchain;
        }

        // GET /api/orderbook/{externalId}
        [HttpGet("{externalId}")]
        public async Task<ActionResult<OrderBookDto>> Get(string externalId, CancellationToken ct)
        {
            if (!BigInteger.TryParse(externalId, out var extId))
                return BadRequest("Invalid externalId");

            var (found, data) = await _blockchain.GetMatchAsync(extId, ct);
            if (!found || data == null)
                return NotFound();

            // Totals are stored on-chain in token base units. USDB decimals = 6.
            decimal home = Web3.Convert.FromWei(data.TotalHome, USDB_DECIMALS);
            decimal away = Web3.Convert.FromWei(data.TotalAway, USDB_DECIMALS);

            var dto = new OrderBookDto
            {
                HomeTotal = home,
                AwayTotal = away
            };

            return Ok(dto);
        }
    }
}

