import { useEffect, useState } from 'react';
import { type WalletState } from '../useWallet';
import { ApiService } from '../services/api';
import type { BetDto } from '../types/bet';
import { getBananaBetContract, getSigner } from '../services/contracts';

type Props = {
  wallet: WalletState;
};

function ProfilePage({ wallet }: Props) {
  const [bets, setBets] = useState<BetDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [claimLoading, setClaimLoading] = useState<number | null>(null); 
  const [error, setError] = useState<string | null>(null);

  const loadBets = async () => {
    if (!wallet.address) return;
    setIsLoading(true);
    setError(null);
    try {
      const data = await ApiService.getBetsByWallet(wallet.address);
      setBets(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadBets();
  }, [wallet.address]);

  const handleClaim = async (bet: BetDto) => {
    try {
      setClaimLoading(bet.matchExternalId);
      const signer = await getSigner();
      const contract = getBananaBetContract(signer);
      
      const tx = await contract.claim(bet.matchExternalId);
      await tx.wait();

      await ApiService.submitClaim({ txHash: tx.hash });
      alert('Winnings claimed successfully!');
      window.location.reload();
    } catch (err: any) {
      console.error(err);
      alert('Error claiming: ' + (err.reason || err.message));
    } finally {
      setClaimLoading(null);
    }
  };

  if (!wallet.address) {
    return (
      <div className="container" style={{ padding: 40, textAlign: 'center' }}>
        <h2>Please connect your wallet to view profile.</h2>
      </div>
    );
  }

  return (
    <div className="container page-content">
      <div className="card" style={{ marginBottom: 32 }}>
        <h2 style={{ marginTop: 0 }}>Wallet Profile</h2>
        <div style={{ color: 'var(--color-primary)', fontSize: 18, fontFamily: 'monospace' }}>
          {wallet.address}
        </div>
      </div>
      
      <h3 style={{ marginBottom: 16 }}>Bet History</h3>
      
      {isLoading ? (
        <div>Loading history...</div>
      ) : error ? (
        <div style={{ color: 'var(--color-danger)' }}>{error}</div>
      ) : bets.length === 0 ? (
        <div style={{ color: 'var(--color-text-muted)' }}>No bets found.</div>
      ) : (
        <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 14 }}>
            <thead>
              <tr style={{ background: '#2c2c36', textAlign: 'left', color: '#ccc' }}>
                <th style={{ padding: '16px 24px' }}>Match</th>
                <th style={{ padding: '16px 24px' }}>Start Time</th>
                <th style={{ padding: '16px 24px' }}>Bet Time</th>
                <th style={{ padding: '16px 24px' }}>Pick</th>
                <th style={{ padding: '16px 24px' }}>Amount</th>
                <th style={{ padding: '16px 24px' }}>Play Amount</th>
                <th style={{ padding: '16px 24px' }}>Odds</th>
                <th style={{ padding: '16px 24px' }}>Status</th>
                <th style={{ padding: '16px 24px' }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {bets.map((bet) => (
                <tr key={bet.id} style={{ borderBottom: '1px solid #2c2c36' }}>
                  <td style={{ padding: '16px 24px', fontWeight: 600 }}>
                    {bet.homeTeam} vs {bet.awayTeam}
                  </td>
                   <td style={{ padding: '16px 24px', color: '#888' }}>
                    {new Date(bet.startTime).toLocaleString()}
                  </td>
                  <td style={{ padding: '16px 24px', color: '#888' }}>
                    {bet.betTime ? new Date(bet.betTime).toLocaleString() : '-'}
                  </td>
                  <td style={{ padding: '16px 24px' }}>
                    <span style={{ color: 'var(--color-primary)' }}>
                      {bet.pick === 1 ? bet.homeTeam : bet.pick === 2 ? bet.awayTeam : 'Draw'}
                    </span>
                  </td>
                  <td style={{ padding: '16px 24px' }}>
                    {Number(bet.amount).toFixed(2)} USDb
                  </td>
                  <td style={{ padding: '16px 24px' }}>
                    {Number(bet.playAmount).toFixed(2)} USDb
                  </td>
                  <td style={{ padding: '16px 24px' }}>
                    x{Number(bet.oddsAtMoment).toFixed(2)}
                  </td>
                  <td style={{ padding: '16px 24px' }}>
                     <span style={{ 
                       color: bet.status === 'Win' ? 'var(--color-success)' : bet.status === 'Lost' ? 'var(--color-danger)' : '#aaa' 
                     }}>
                       {bet.status}
                     </span>
                  </td>
                  <td style={{ padding: '16px 24px', display: 'flex', alignItems: 'center', gap: 12 }}>
                    {bet.blockchainTxHash && (
                      <a
                        href={`https://sepolia.etherscan.io/tx/${bet.blockchainTxHash}`}
                        target="_blank"
                        rel="noreferrer"
                        title="View on Etherscan"
                        style={{ color: '#555', fontSize: 18 }}
                      >
                        ↗
                      </a>
                    )}
                    {bet.status === 'Win' && (
                      <button 
                        onClick={() => handleClaim(bet)} 
                        disabled={claimLoading !== null}
                        className="btn-primary"
                        style={{ padding: '4px 12px', fontSize: 12 }}
                      >
                        {claimLoading === bet.matchExternalId ? '...' : 'Claim'}
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

export default ProfilePage;
