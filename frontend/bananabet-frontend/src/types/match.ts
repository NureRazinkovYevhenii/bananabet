export type MatchStatus =
  | 'Fetched'
  | 'OddsCalculated'
  | 'ReadyForChain'
  | 'OnChain'
  | 'Open'
  | 'Closed'
  | 'Resolved'
  | number
  | string;

export type MatchDto = {
  id: number;
  externalId: string;
  homeTeam: string;
  awayTeam: string;
  startTime: string; // ISO string from API
  oddsHome: number;
  oddsAway: number;
  status: MatchStatus;
};

