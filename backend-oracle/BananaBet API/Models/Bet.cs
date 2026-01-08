using System.Text.RegularExpressions;

namespace BananaBet_API.Models
{
    public class Bet
    {
        public int Id { get; set; }

        // Адреса гаманця користувача (замість UserId)
        public string UserWalletAddress { get; set; } = null!;

        // FK -> Match
        public int MatchId { get; set; }
        public Match Match { get; set; } = null!;

        // Сума ставки (ETH / Token)
        public decimal Amount { get; set; }

        // Сума, яка реально зіграла після матчінгу
        public decimal PlayAmount { get; set; }

        public int OnChainIndex { get; set; }

        // 0 = Home, 1 = Away (як у смарт-контракті)
        public int Pick { get; set; }

        // Коефіцієнт у момент здійснення ставки
        public decimal OddsAtMoment { get; set; }

        // Tx hash у блокчейні (для перевірки в експлорері)
        public string BlockchainTxHash { get; set; } = null!;

        // Час ставки
        public DateTime BetTime { get; set; }

        // Статус ставки как в блокчейне
        public BetStatus Status { get; set; }
    }

    // ===== BETTING =====
    public enum BetStatus
    {
        Placed = 0,
        Matched = 1,
        Refunded = 2,
        Claimed = 3,
        Win = 4,
        Lose = 5
    }
}
