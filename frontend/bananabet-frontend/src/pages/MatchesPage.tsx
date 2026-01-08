import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { type MatchDto, type MatchStatus } from '../types/match';

const PAGE_SIZE = 5;
const LEAGUE_LABEL = "🏴󠁧󠁢󠁥󠁮󠁧󠁿 Premier League";

type Tab = 'upcoming' | 'ongoing' | 'history';

function MatchesPage() {
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState<Tab>('upcoming');
  
  // State for data
  const [matches, setMatches] = useState<MatchDto[]>([]);
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(true);
  
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const sentinelRef = useRef<HTMLDivElement | null>(null);

  // Helper to fetch matches
  const fetchMatches = useCallback(async (currPage: number, tabForUrl: Tab) => {
    setIsLoading(true);
    setError(null);
    try {
      let baseUrl = 'https://localhost:7018/api/matches';
      if (tabForUrl === 'ongoing') baseUrl = 'https://localhost:7018/api/matches/ongoing';
      if (tabForUrl === 'history') baseUrl = 'https://localhost:7018/api/matches/history';

      const url = `${baseUrl}?page=${currPage}&pageSize=${PAGE_SIZE}`;

      const res = await fetch(url);
      if (!res.ok) throw new Error('Failed to load matches');
      const data = (await res.json()) as MatchDto[];
      
      if (data.length < PAGE_SIZE) {
        setHasMore(false);
      } else {
        setHasMore(true);
      }

      setMatches(prev => currPage === 1 ? data : [...prev, ...data]);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      setIsLoading(false);
    }
  }, []);

  // When tab changes, reset and load page 1
  useEffect(() => {
    setMatches([]);
    setPage(1);
    setHasMore(true);
    void fetchMatches(1, activeTab);
  }, [activeTab, fetchMatches]);

  // Infinite scroll observer
  useEffect(() => {
    if (!sentinelRef.current) return;
    const observer = new IntersectionObserver(
      (entries) => {
        const [entry] = entries;
        if (entry.isIntersecting && !isLoading && hasMore) {
          const nextPage = page + 1;
          setPage(nextPage);
          void fetchMatches(nextPage, activeTab);
        }
      },
      { rootMargin: '200px 0px' }
    );
    observer.observe(sentinelRef.current);
    return () => observer.disconnect();
  }, [page, isLoading, hasMore, activeTab, fetchMatches]);

  const statusMeta = (status: MatchStatus) => {
    const normalize = typeof status === 'number' ? status : Number.isFinite(Number(status)) ? Number(status) : status;

    if (normalize === 4 || status === 'Open') return { label: 'Open', color: 'var(--color-success)' };
    if (normalize === 5 || status === 'Closed') return { label: 'Live', color: 'var(--color-ongoing)' };
    if (normalize === 6 || status === 'Resolved') return { label: 'Finished', color: '#7f8c8d' };

    return { label: String(status), color: '#888' };
  };

  const tabs: { id: Tab; label: string }[] = [
    { id: 'upcoming', label: 'Upcoming' },
    { id: 'ongoing', label: 'Live' },
    { id: 'history', label: 'History' },
  ];

  return (
    <div className="container page-content">
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <h2 style={{ margin: 0, color: 'var(--color-text-main)', display: 'flex', alignItems: 'center', gap: 10 }}>
          {LEAGUE_LABEL}
        </h2>
        
        {/* Modern Tabs */}
        <div style={{ display: 'flex', background: '#2c2c36', padding: 4, borderRadius: 24 }}>
          {tabs.map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              style={{
                padding: '6px 16px',
                border: 'none',
                background: activeTab === tab.id ? 'var(--color-primary)' : 'transparent',
                color: activeTab === tab.id ? '#000' : '#aaa',
                borderRadius: 20,
                cursor: 'pointer',
                fontWeight: 600,
                fontSize: 13,
                transition: 'all 0.2s',
                boxShadow: activeTab === tab.id ? '0 2px 4px rgba(0,0,0,0.2)' : 'none'
              }}
            >
              {tab.label}
            </button>
          ))}
        </div>
      </div>

      {error ? (
        <div style={{ color: 'var(--color-danger)', marginTop: 20, textAlign: 'center' }}>{error}</div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
          {matches.map((m) => (
            <article
              key={m.id}
              onClick={() => navigate(`/matches/${m.id}`)}
              className="card"
              style={{
                cursor: 'pointer',
                padding: 20,
                border: '1px solid transparent',
                borderColor: '#2c2c36'
              }}
              onMouseEnter={(e) => {
                 (e.currentTarget as HTMLElement).style.borderColor = 'var(--color-primary)';
                 (e.currentTarget as HTMLElement).style.transform = 'translateY(-2px)';
              }}
              onMouseLeave={(e) => {
                 (e.currentTarget as HTMLElement).style.borderColor = '#2c2c36';
                 (e.currentTarget as HTMLElement).style.transform = 'translateY(0)';
              }}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                {/* Home Team */}
                <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', minWidth: 100 }}>
                   <div style={{ width: 60, height: 60, background: '#101014', borderRadius: '50%', padding: 8, marginBottom: 8, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                        <img 
                            src={`https://localhost:7018/team-logos/${m.homeTeam}.png`} 
                            alt={m.homeTeam}
                            style={{ width: '100%', height: '100%', objectFit: 'contain' }}
                            onError={(e) => { (e.target as HTMLImageElement).style.visibility = 'hidden'; }}
                        />
                   </div>
                  <div style={{ fontSize: 16, fontWeight: 700, textAlign: 'center' }}>{m.homeTeam}</div>
                </div>
                
                {/* Center Info */}
                <div style={{ textAlign: 'center', flex: 1, padding: '0 20px' }}>
                 <div style={{ fontSize: 12, color: '#666', marginBottom: 4 }}>
                   {new Date(m.startTime).toLocaleString('en-GB', { day: 'numeric', month: 'short', hour: '2-digit', minute:'2-digit' })}
                 </div>
                 <div style={{ fontSize: 28, fontWeight: 900, color: '#333' }}>VS</div>
                 <div
                   style={{
                     display: 'inline-block',
                     padding: '2px 8px',
                     borderRadius: 4,
                     background: statusMeta(m.status).color + '20',
                     color: statusMeta(m.status).color,
                     marginTop: 8,
                     fontSize: 11,
                     fontWeight: 700,
                     letterSpacing: 1,
                     textTransform: 'uppercase'
                   }}
                 >
                   {statusMeta(m.status).label}
                 </div>
                </div>

                {/* Away Team */}
                <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', minWidth: 100 }}>
                    <div style={{ width: 60, height: 60, background: '#101014', borderRadius: '50%', padding: 8, marginBottom: 8, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                         <img 
                            src={`https://localhost:7018/team-logos/${m.awayTeam}.png`} 
                            alt={m.awayTeam}
                            style={{ width: '100%', height: '100%', objectFit: 'contain' }}
                            onError={(e) => { (e.target as HTMLImageElement).style.visibility = 'hidden'; }}
                        />
                    </div>
                  <div style={{ fontSize: 16, fontWeight: 700, textAlign: 'center' }}>{m.awayTeam}</div>
                </div>
              </div>

              {/* Odds Footer */}
               <div
                  style={{
                    display: 'flex',
                    justifyContent: 'center',
                    gap: 40,
                    marginTop: 20,
                    paddingTop: 16,
                    borderTop: '1px solid #2c2c36'
                  }}
                >
                  <div style={{ textAlign: 'center' }}>
                      <span style={{ fontSize: 12, color:'var(--color-text-muted)', display: 'block' }}>1</span>
                      <span style={{ fontSize: 18, color: 'var(--color-primary)', fontWeight: 'bold' }}>x{m.oddsHome.toFixed(2)}</span>
                  </div>
                   <div style={{ textAlign: 'center' }}>
                      <span style={{ fontSize: 12, color:'var(--color-text-muted)', display: 'block' }}>2</span>
                      <span style={{ fontSize: 18, color: 'var(--color-primary)', fontWeight: 'bold' }}>x{m.oddsAway.toFixed(2)}</span>
                  </div>
                </div>
            </article>
          ))}

          {matches.length === 0 && !isLoading && (
            <div style={{ textAlign: 'center', color: 'var(--color-text-muted)', padding: 60 }}>
              No matches found in this category.
            </div>
          )}

          {isLoading && <div style={{ textAlign: 'center', padding: 20, color: 'var(--color-primary)' }}>Loading matches...</div>}

          <div ref={sentinelRef} style={{ height: 1 }} />
        </div>
      )}
    </div>
  );
}

export default MatchesPage;

