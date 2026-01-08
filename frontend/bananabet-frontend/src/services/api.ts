import type { BetDto, ClaimRequest } from '../types/bet';

const API_BASE = 'https://localhost:7018/api';

export const ApiService = {
  getBetsByWallet: async (walletAddress: string): Promise<BetDto[]> => {
    const response = await fetch(`${API_BASE}/bets/by-wallet/${walletAddress}`);
    if (!response.ok) {
      throw new Error(`Failed to fetch bets: ${response.statusText}`);
    }
    return response.json();
  },

  submitClaim: async (request: ClaimRequest): Promise<void> => {
    const response = await fetch(`${API_BASE}/claims`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
        const errorText = await response.text();
        throw new Error(errorText || `Claim failed: ${response.statusText}`);
    }
  },

  createBet: async (txHash: string, matchId: number, pick: number, amount: number, userWalletAddress: string): Promise<BetDto> => {
    const response = await fetch(`${API_BASE}/bets`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        txHash,
        matchId,
        pick,
        amount,
        userWalletAddress
      })
    });
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(errorText || 'Failed to create bet on backend');
    }
    return response.json();
  }
};

