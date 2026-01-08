export interface BetDto {
  id: number;
  userWalletAddress: string;
  matchId: number;
  matchExternalId: number;
  homeTeam: string;
  awayTeam: string;
  startTime: string;
  pick: number;
  amount: number;
  playAmount: number;
  oddsAtMoment: number;
  betTime: string;
  blockchainTxHash: string;
  status: string;
}

export type ClaimRequest = {
  txHash: string;
};
