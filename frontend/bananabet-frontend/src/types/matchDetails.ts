import { type MatchStatus } from './match';

export type MatchDetailsDto = {
  id: number;
  externalId: string;
  homeTeam: string;
  awayTeam: string;
  startTime: string;
  oddsHome: number;
  oddsAway: number;
  status: MatchStatus;
  result: string | null;
};

export type OrderBookDto = {
  homeTotal: number;
  awayTotal: number;
};

