// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

interface IERC20 {
    function transfer(address to, uint256 amount) external returns (bool);
    function transferFrom(address from, address to, uint256 amount) external returns (bool);
}

contract BananaBet {

    // ===== CONFIG =====
    uint256 public constant PRECISION = 1000;

    IERC20 public immutable usdb;
    address public oracle;

    mapping(address => uint256) public balances;

    constructor(address _usdb) {
        usdb = IERC20(_usdb);
    }

    // ===== MODIFIERS =====
    modifier onlyOracle() {
        require(msg.sender == oracle, "not oracle");
        _;
    }

    function setOracle(address _oracle) external {
        require(oracle == address(0), "oracle already set");
        oracle = _oracle;
    }

    // ===== MATCHES =====

    error MatchAlreadyExists(uint256 externalId);
    error MatchNotFound(uint256 externalId);
    error InvalidMatchStatus(uint256 externalId, uint8 current, uint8 expected);
    error InvalidResult(uint8 result);
    error AlreadyMatched(uint256 externalId);

    enum MatchStatus {
        Created,
        Open,
        Closed,
        Resolved
    }

    struct Match {
        uint256 externalId;
        uint256 oddsHome;
        uint256 oddsAway;

        uint256 totalHome;
        uint256 totalAway;

        uint256 matchedHome;
        uint256 matchedAway;

        MatchStatus status;
        uint8 result; // 1 = home, 2 = away
        bool matched;
    }

    mapping(uint256 => Match) public matches;

    function createMatch(
    uint256 externalId,
    uint256 oddsHome,
    uint256 oddsAway
) external onlyOracle {
    if (matches[externalId].externalId != 0) {
        revert MatchAlreadyExists(externalId);
    }

    matches[externalId] = Match({
        externalId: externalId,
        oddsHome: oddsHome,
        oddsAway: oddsAway,
        totalHome: 0,
        totalAway: 0,
        matchedHome: 0,
        matchedAway: 0,
        status: MatchStatus.Created,
        result: 0,
        matched: false
    });
}

    function openMatch(uint256 externalId) external onlyOracle {
    Match storage m = matches[externalId];

    if (m.externalId == 0) {
        revert MatchNotFound(externalId);
    }

    if (m.status != MatchStatus.Created) {
        revert InvalidMatchStatus(
            externalId,
            uint8(m.status),
            uint8(MatchStatus.Created)
        );
    }

    m.status = MatchStatus.Open;
}

    function closeMatch(uint256 externalId) external onlyOracle {
    Match storage m = matches[externalId];

    if (m.status != MatchStatus.Open) {
        revert InvalidMatchStatus(
            externalId,
            uint8(m.status),
            uint8(MatchStatus.Open)
        );
    }

    _matchBets(externalId);
    m.status = MatchStatus.Closed;
}

    function resolveMatch(uint256 externalId, uint8 result) external onlyOracle {
    Match storage m = matches[externalId];

    if (m.status != MatchStatus.Closed) {
        revert InvalidMatchStatus(
            externalId,
            uint8(m.status),
            uint8(MatchStatus.Closed)
        );
    }

    if (result > 2) {
        revert InvalidResult(result);
    }

    m.result = result;
    m.status = MatchStatus.Resolved;
}

    // ===== BETTING =====
    enum BetStatus {
        Placed,
        Matched,
        Refunded,
        Claimed
    }

    struct Bet {
        address user;
        uint256 amount;
        uint256 playAmount;
        uint8 side; // 1 = home, 2 = away
        BetStatus status;
    }

    mapping(uint256 => Bet[]) public betsByMatch;

    function placeBet(
        uint256 externalId,
        uint8 side,
        uint256 amount
    ) external {
        Match storage m = matches[externalId];

        require(m.status == MatchStatus.Open, "match not open");
        require(side == 1 || side == 2, "bad side");
        require(amount > 0, "amount = 0");
        require(balances[msg.sender] >= amount, "not enough balance");

        balances[msg.sender] -= amount;

        betsByMatch[externalId].push(
            Bet({
                user: msg.sender,
                amount: amount,
                playAmount: 0,
                side: side,
                status: BetStatus.Placed
            })
        );

        if (side == 1) m.totalHome += amount;
        else m.totalAway += amount;
    }

    // ===== MATCHING ENGINE =====
    function _matchBets(uint256 externalId) internal {
        Match storage m = matches[externalId];
        require(!m.matched, "already matched");

        // --- HOME ---
        uint256 maxHome =
            (m.totalAway * m.oddsAway) / m.oddsHome;

        uint256 remainingHome = maxHome;

        Bet[] storage bets = betsByMatch[externalId];

        for (uint256 i = 0; i < bets.length; i++) {
            Bet storage b = bets[i];
            if (b.side != 1) continue;

            uint256 play = b.amount;
            if (play > remainingHome) play = remainingHome;

            b.playAmount = play;
            b.status = BetStatus.Matched;

            balances[b.user] += (b.amount - play);

            remainingHome -= play;
            m.matchedHome += play;

            if (remainingHome == 0) break;
        }

        // --- AWAY ---
        uint256 requiredAway =
            (m.matchedHome * m.oddsHome) / m.oddsAway;

        uint256 remainingAway = requiredAway;

        for (uint256 i = 0; i < bets.length; i++) {
            Bet storage b = bets[i];
            if (b.side != 2) continue;

            uint256 play = b.amount;
            if (play > remainingAway) play = remainingAway;

            b.playAmount = play;
            b.status = BetStatus.Matched;

            balances[b.user] += (b.amount - play);

            remainingAway -= play;
            m.matchedAway += play;

            if (remainingAway == 0) break;
        }

        m.matched = true;
    }

    // ===== CLAIM =====
    function claim(uint256 externalId) external {
    Match storage m = matches[externalId];
    require(m.status == MatchStatus.Resolved, "not resolved");

    Bet[] storage bets = betsByMatch[externalId];

    for (uint256 i = 0; i < bets.length; i++) {
        Bet storage b = bets[i];

        if (b.user != msg.sender) continue;
        if (b.status != BetStatus.Matched) continue;
        if (b.playAmount == 0) continue;

        // ===== DRAW =====
        if (m.result == 0) {
            balances[msg.sender] += b.playAmount;
            b.status = BetStatus.Claimed;
            continue;
        }

        // ===== WIN =====
        if (b.side == m.result) {
            uint256 odds = (b.side == 1)
                ? m.oddsHome
                : m.oddsAway;

            uint256 payout =
                (b.playAmount * odds) / PRECISION;

            balances[msg.sender] += payout;
            b.status = BetStatus.Claimed;
        }
    }
}

    // ===== DEPOSIT / WITHDRAW =====
    function deposit(uint256 amount) external {
        require(amount > 0, "amount = 0");
        require(usdb.transferFrom(msg.sender, address(this), amount));
        balances[msg.sender] += amount;
    }

    function withdraw(uint256 amount) external {
        require(amount > 0, "amount = 0");
        require(balances[msg.sender] >= amount, "not enough balance");

        balances[msg.sender] -= amount;
        require(usdb.transfer(msg.sender, amount));
    }
}
