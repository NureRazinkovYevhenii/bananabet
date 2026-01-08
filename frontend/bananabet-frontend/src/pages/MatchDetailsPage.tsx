import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { type MatchDetailsDto, type OrderBookDto } from '../types/matchDetails';
import { type MatchStatus } from '../types/match';
import { computeOrderbookImbalance } from '../utils/orderbook';
import { type WalletState } from '../useWallet';
import { getBananaBetContract, getSigner, TOKEN_DECIMALS } from '../services/contracts';
import { ApiService } from '../services/api';
import { parseUnits } from 'ethers';

type Props = {
  wallet: WalletState;
};

// Helper to normalize status
const getStatus = (status: MatchStatus) => {
  const norm = typeof status === 'number' ? status : Number.isFinite(Number(status)) ? Number(status) : status;
  return norm;
};

const statusMeta = (status: MatchStatus) => {
  const norm = getStatus(status);
  if (norm === 4 || status === 'Open') return { label: 'Open For Betting', color: 'var(--color-success)' };
  if (norm === 5 || status === 'Closed') return { label: 'Betting Closed', color: 'var(--color-danger)' };
  if (norm === 6 || status === 'Resolved') return { label: 'Match Resolved', color: '#7f8c8d' };
  return { label: String(status), color: '#888' };
};

const isOpen = (status: MatchStatus) => {
  const norm = getStatus(status);
  return norm === 4 || status === 'Open';
};

function MatchDetailsPage({ wallet }: Props) {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const matchId = Number(id);

  const { address, connect, balance } = wallet;

  const [match, setMatch] = useState<MatchDetailsDto | null>(null);
  const [orderbook, setOrderbook] = useState<OrderBookDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  // Betting state
  const [selectedSide, setSelectedSide] = useState<'home' | 'away' | null>(null);
  const [amountInput, setAmountInput] = useState('');
  const [betLoading, setBetLoading] = useState(false);
  const [txHash, setTxHash] = useState<string | null>(null);

  const isInsufficientBalance = useMemo(() => {
    if (!balance || !amountInput) return false;
    const bal = Number(balance);
    const amt = Number(amountInput);
    return !isNaN(bal) && !isNaN(amt) && amt > bal;
  }, [balance, amountInput]);

  useEffect(() => {
    if (!matchId) return;
    const load = async () => {
      setLoading(true);
      setError(null);
      try {
        const res = await fetch(`https://localhost:7018/api/matches/${matchId}`);
        if (!res.ok) throw new Error('Match not found');
        const data = (await res.json()) as MatchDetailsDto;
        setMatch(data);

        const obRes = await fetch(`https://localhost:7018/api/orderbook/${data.externalId}`);
        if (obRes.ok) {
          const ob = (await obRes.json()) as OrderBookDto;
          setOrderbook(ob);
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Unknown error');
      } finally {
        setLoading(false);
      }
    };
    void load();
  }, [matchId]);

  const imbalance = useMemo(() => {
    if (!match || !orderbook) return null;
    return computeOrderbookImbalance({
      totalHome: orderbook.homeTotal,
      totalAway: orderbook.awayTotal,
      oddsHome: match.oddsHome,
      oddsAway: match.oddsAway,
    });
  }, [match, orderbook]);

  const handlePlaceBet = async () => {
    if (!address) {
      void connect();
      return;
    }
    if (!match || !selectedSide || !amountInput) return;

    try {
      setBetLoading(true);
      setTxHash(null);

      const amountWei = parseUnits(amountInput, TOKEN_DECIMALS);
      const sideInt = selectedSide === 'home' ? 1 : 2;

      const signer = await getSigner();
      const contract = getBananaBetContract(signer);

      const tx = await contract.placeBet(match.externalId, sideInt, amountWei);
      setTxHash(tx.hash);
      
      await tx.wait(); 

      // Notify backend
      await ApiService.createBet(
        tx.hash,
        match.id,
        sideInt,
        Number(amountInput),
        address
      );

      alert(`Bet placed successfully! Tx: ${tx.hash}`);
      window.location.reload();
    } catch (err: any) {
      console.error(err);
      alert('Error placing bet: ' + (err.reason || err.message));
    } finally {
      setBetLoading(false);
    }
  };

  if (loading) return <div className="container" style={{padding: 40, textAlign: 'center'}}>Loading...</div>;
  if (error) return <div className="container" style={{padding: 40, textAlign: 'center', color: 'var(--color-danger)'}}>{error}</div>;
  if (!match) return <div className="container">Match not found</div>;

  const normStatus = getStatus(match.status);

  return (
    <div className="container page-content">
      <button
        className="btn-secondary"
        onClick={() => navigate('/matches')}
        style={{ marginBottom: 24, border: 'none', paddingLeft: 0, color: 'var(--color-text-muted)' }}
      >
        ← Back to list
      </button>

      {/* MATCH HEADER CARD */}
      <div className="card" style={{ marginBottom: 24, padding: 40 }}>
        <div style={{ textAlign: 'center', marginBottom: 24 }}>
           <div style={{ 
              display: 'inline-block', 
              padding: '4px 12px', 
              borderRadius: 16, 
              background: statusMeta(match.status).color + '22', 
              color: statusMeta(match.status).color,
              fontSize: 12, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 1,
              marginBottom: 20
           }}>
              {statusMeta(match.status).label}
           </div>

           <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', gap: 40 }}>
              {/* Home */}
              <div style={{ width: 150 }}>
                  <img src={`https://localhost:7018/team-logos/${match.homeTeam}.png`} alt={match.homeTeam} style={{ width: 100, height: 100, objectFit: 'contain', marginBottom: 12 }} onError={(e) => { (e.target as HTMLImageElement).style.visibility = 'hidden'; }} />
                  <h2 style={{ fontSize: 24, margin: 0 }}>{match.homeTeam}</h2>
              </div>
              
              <div style={{ fontSize: 20, fontWeight: 300, color: 'var(--color-text-muted)' }}>VS</div>

              {/* Away */}
              <div style={{ width: 150 }}>
                  <img src={`https://localhost:7018/team-logos/${match.awayTeam}.png`} alt={match.awayTeam} style={{ width: 100, height: 100, objectFit: 'contain', marginBottom: 12 }} onError={(e) => { (e.target as HTMLImageElement).style.visibility = 'hidden'; }} />
                   <h2 style={{ fontSize: 24, margin: 0 }}>{match.awayTeam}</h2>
              </div>
           </div>

           <div style={{ marginTop: 20, color: 'var(--color-text-muted)' }}>
             {new Date(match.startTime).toLocaleString()}
           </div>

           {match.result && (
            <div style={{ marginTop: 24, padding: 16, background: 'rgba(52, 152, 219, 0.1)', borderRadius: 8, border: '1px solid #3498db' }}>
              <span style={{ color: '#3498db', fontWeight: 'bold' }}>WINNER: </span>
              <span style={{ fontSize: 18, color: 'white', marginLeft: 8 }}>
                 {(match.result === '1' || match.result === 'Home') ? match.homeTeam : (match.result === '2' || match.result === 'Away') ? match.awayTeam : 'Draw'}
              </span>
            </div>
           )}
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'minmax(300px, 2fr) minmax(250px, 1fr)', gap: 24 }}>
          {/* LEFT COL: BETTING */}
          <div>
            {isOpen(match.status) ? (
              <>
                 <h3 style={{ marginTop: 0 }}>Make your prediction</h3>
                 <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
                    {/* Home Tile */}
                    <div 
                      onClick={() => setSelectedSide('home')}
                      className="card"
                      style={{ 
                        cursor: 'pointer', 
                        borderColor: selectedSide === 'home' ? 'var(--color-primary)' : 'var(--color-border)',
                        background: selectedSide === 'home' ? 'rgba(241, 196, 15, 0.1)' : 'var(--color-card-bg)',
                        textAlign: 'center',
                        transition: '0.2s'
                      }}
                    >
                      <div style={{ fontWeight: 600, color: 'var(--color-text-muted)' }}>{match.homeTeam}</div>
                      <div style={{ fontSize: 32, fontWeight: 700, color: 'var(--color-primary)', margin: '10px 0' }}>{match.oddsHome.toFixed(2)}</div>
                      {imbalance && imbalance.betterSide === 'Home' && (
                        <div style={{ fontSize: 12, color: 'var(--color-success)' }}>
                         Liquidity Incentive Available
                        </div>
                      )}
                    </div>

                    {/* Away Tile */}
                    <div 
                      onClick={() => setSelectedSide('away')}
                      className="card"
                      style={{ 
                        cursor: 'pointer', 
                        borderColor: selectedSide === 'away' ? 'var(--color-primary)' : 'var(--color-border)',
                         background: selectedSide === 'away' ? 'rgba(241, 196, 15, 0.1)' : 'var(--color-card-bg)',
                        textAlign: 'center',
                        transition: '0.2s'
                      }}
                    >
                      <div style={{ fontWeight: 600, color: 'var(--color-text-muted)' }}>{match.awayTeam}</div>
                      <div style={{ fontSize: 32, fontWeight: 700, color: 'var(--color-primary)', margin: '10px 0' }}>{match.oddsAway.toFixed(2)}</div>
                      {imbalance && imbalance.betterSide === 'Away' && (
                         <div style={{ fontSize: 12, color: 'var(--color-success)' }}>
                         Liquidity Incentive Available
                        </div>
                      )}
                    </div>
                 </div>

                 {selectedSide && (
                   <div style={{ marginTop: 24, background: '#2c2c36', padding: 20, borderRadius: 12 }}>
                      <div style={{ marginBottom: 12, display: 'flex', justifyContent: 'space-between' }}>
                        <span>Selected Outcome:</span>
                        <span style={{ fontWeight: 'bold', color: 'var(--color-primary)' }}>
                          {selectedSide === 'home' ? match.homeTeam : match.awayTeam}
                        </span>
                      </div>
                      <div style={{ display: 'flex', gap: 12 }}>
                        <input 
                          type="number"
                          placeholder="Amount (USDb)"
                          value={amountInput}
                          onChange={e => setAmountInput(e.target.value)}
                          style={{ 
                            flex: 1, 
                            padding: 12, 
                            borderRadius: 8, 
                            border: isInsufficientBalance ? '1px solid var(--color-danger)' : '1px solid #444', 
                            background: '#101014', 
                            color: 'white',
                            fontSize: 16
                          }}
                        />
                        <button 
                          className="btn-primary" 
                          onClick={handlePlaceBet}
                          disabled={betLoading || !amountInput || isInsufficientBalance}
                          style={{ minWidth: 120 }}
                        >
                          {betLoading ? 'Signing...' : 'BET'}
                        </button>
                      </div>
                      {isInsufficientBalance && (
                        <div style={{ marginTop: 8, color: 'var(--color-danger)', fontSize: 13 }}>
                          Insufficient balance. Available: {Number(balance).toFixed(2)} USDb
                        </div>
                      )}
                      {txHash && (
                        <div style={{ marginTop: 12, color: 'var(--color-success)', fontSize: 13, wordBreak: 'break-all' }}>
                          Transaction sent: {txHash}
                        </div>
                      )}
                   </div>
                 )}
              </>
            ) : (
              <div className="card" style={{ textAlign: 'center', padding: 40, color: 'var(--color-text-muted)' }}>
                 Markets are currently closed for this event.
              </div>
            )}
          </div>

          {/* RIGHT COL: MARKET STATS */}
          <div className="card" style={{ height: 'fit-content' }}>
             <h4 style={{ marginTop: 0, color: 'var(--color-text-muted)', textTransform: 'uppercase', fontSize: 12, letterSpacing: 1 }}>Market Depth</h4>
             
             {orderbook ? (
               <div style={{ marginTop: 20 }}>
                 <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8, fontSize: 14 }}>
                    <span>Home Pool</span>
                    <span>Away Pool</span>
                 </div>
                 
                 <div style={{ display: 'flex', height: 8, background: '#333', borderRadius: 4, overflow: 'hidden' }}>
                    <div style={{ width: `${(orderbook.homeTotal / (orderbook.homeTotal + orderbook.awayTotal || 1)) * 100}%`, background: 'var(--color-blue)' }} />
                    <div style={{ width: `${(orderbook.awayTotal / (orderbook.homeTotal + orderbook.awayTotal || 1)) * 100}%`, background: 'var(--color-danger)' }} />
                 </div>

                 <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 8, fontWeight: 'bold' }}>
                    <span style={{ color: 'var(--color-blue)' }}>${orderbook.homeTotal.toFixed(0)}</span>
                    <span style={{ color: 'var(--color-danger)' }}>${orderbook.awayTotal.toFixed(0)}</span>
                 </div>
                 
                 <div style={{ padding: 12, borderRadius: 8, background: '#1c1c21', border: '1px solid #333', marginTop: 16 }}>
                    {imbalance && imbalance.side !== 'Balanced' && imbalance.betterSide ? (
                       <div style={{ fontSize: 13 }}>
                          <div>
                            Advantage: <strong style={{ color: imbalance.side === 'Home' ? 'var(--color-blue)' : 'var(--color-danger)' }}>{imbalance.side === 'Home' ? match.homeTeam : match.awayTeam}</strong>
                          </div>
                          <div style={{ marginTop: 4, color: '#aaa', lineHeight: 1.4 }}>
                            Needs <strong style={{ color: 'var(--color-success)' }}>{imbalance.missingStake.toFixed(2)} USDb</strong> on {imbalance.betterSide === 'Home' ? match.homeTeam : match.awayTeam} to balance.
                          </div>
                       </div>
                    ) : (
                      <div style={{ fontSize: 13, color: 'var(--color-success)' }}>Market is perfectly balanced.</div>
                    )}
                 </div>

                 <div style={{ marginTop: 24, paddingTop: 24, borderTop: '1px solid #333' }}>
                    <div style={{ fontSize: 12, color: 'var(--color-text-muted)', marginBottom: 8 }}>TOTAL LOCKED VALUE</div>
                    <div style={{ fontSize: 24 }}>${(orderbook.homeTotal + orderbook.awayTotal).toFixed(2)}</div>
                 </div>
               </div>
             ) : (
               <p>Loading market data...</p>
             )}
          </div>
      </div>
    </div>
  );
}

export default MatchDetailsPage;

