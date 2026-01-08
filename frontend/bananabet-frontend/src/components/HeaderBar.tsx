import { useMemo, useState, useRef, useEffect } from 'react';
import { parseUnits } from 'ethers';
import { type WalletState } from '../useWallet';
import {
  BANANABET_CONTRACT_ADDRESS,
  MAX_APPROVE_AMOUNT,
  TOKEN_DECIMALS,
  getBananaBetContract,
  getSigner,
  getUsdbContract,
} from '../services/contracts';
import { NavLink, useNavigate } from 'react-router-dom';

type Action = 'deposit' | 'withdraw';

const safeParseAmount = (value: string): bigint | null => {
  try {
    if (!value.trim()) return null;
    return parseUnits(value, TOKEN_DECIMALS);
  } catch {
    return null;
  }
};

type Props = {
  wallet: WalletState;
};

function HeaderBar({ wallet }: Props) {
  const { address, balance, connect, refreshBalance, disconnect } = wallet;
  const navigate = useNavigate();

  const [allowance, setAllowance] = useState<bigint>(0n);
  const [action, setAction] = useState<Action | null>(null);
  const [amountInput, setAmountInput] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [txHash, setTxHash] = useState<string | null>(null);

  // Dropdown state
  const [isProfileOpen, setIsProfileOpen] = useState(false);
  const profileRef = useRef<HTMLDivElement | null>(null);

  const amountWei = useMemo(() => safeParseAmount(amountInput), [amountInput]);
  const needsApprove = useMemo(
    () => action === 'deposit' && Boolean(amountWei && amountWei > 0n && allowance < amountWei),
    [action, allowance, amountWei],
  );

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (profileRef.current && !profileRef.current.contains(event.target as Node)) {
        setIsProfileOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, []);

  const ensureConnected = () => {
    if (!address) {
      throw new Error('Підключіть MetaMask, щоб продовжити.');
    }
    return address;
  };

  const refreshAllowance = async () => {
    if (!address) return;
    try {
      const signer = await getSigner();
      const usdb = getUsdbContract(signer);
      const val = await usdb.allowance(address, BANANABET_CONTRACT_ADDRESS);
      setAllowance(val);
    } catch {
      // ignore
    }
  };

  useEffect(() => {
    if (address) {
      void refreshAllowance();
    }
  }, [address]);

  // When opening deposit modal, update allowance
  useEffect(() => {
    if (action === 'deposit') {
      void refreshAllowance();
    }
  }, [action]);

  const approveIfNeeded = async (amount: bigint) => {
    if (allowance >= amount) return;
    const signer = await getSigner();
    const usdb = getUsdbContract(signer);
    const tx = await usdb.approve(BANANABET_CONTRACT_ADDRESS, MAX_APPROVE_AMOUNT);
    setTxHash(tx.hash); // show approval tx
    await tx.wait(1);
    await refreshAllowance();
  };

  const submit = async () => {
    try {
      ensureConnected();
      setError(null);
      setTxHash(null);

      if (!amountWei || amountWei <= 0n) {
        throw new Error('Введіть коректну суму.');
      }

      setIsLoading(true);

      if (action === 'deposit') {
        await approveIfNeeded(amountWei);
        const signer = await getSigner();
        const banana = getBananaBetContract(signer);
        const tx = await banana.deposit(amountWei);
        setTxHash(tx.hash);
        await tx.wait(1);
      } else {
        const signer = await getSigner();
        const banana = getBananaBetContract(signer);
        const tx = await banana.withdraw(amountWei);
        setTxHash(tx.hash);
        await tx.wait(1);
      }

      setAmountInput('');
      setAction(null);
      await Promise.all([refreshBalance(), refreshAllowance()]);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Помилка транзакції.');
    } finally {
      setIsLoading(false);
    }
  };

  const balanceLabel = balance ?? '—';

  return (
    <header
      style={{
        height: 'var(--nav-height)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        padding: '0 24px',
        background: 'var(--color-card-bg)',
        borderBottom: '1px solid var(--color-border)',
        position: 'sticky',
        top: 0,
        zIndex: 100
      }}
    >
      <div style={{ display: 'flex', alignItems: 'center', gap: 32 }}>
        <NavLink to="/" style={{ fontSize: 22, fontWeight: 800, color: 'var(--color-primary)', textDecoration: 'none', display: 'flex', alignItems: 'center', gap: 8 }}>
          <span>🍌 BananaBet</span>
        </NavLink>
        <nav style={{ display: 'flex', gap: 20 }}>
          <NavLink 
            to="/matches" 
            style={({ isActive }) => ({
               color: isActive ? 'var(--color-primary)' : 'var(--color-text-muted)',
               textDecoration: 'none',
               fontWeight: 600
            })}
          >
            Матчі
          </NavLink>
        </nav>
      </div>

      <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
        {!address ? (
          <button onClick={connect} className="btn-primary">
            Підключити Wallet
          </button>
        ) : (
          <>
            <div style={{ 
              display: 'flex', 
              alignItems: 'center', 
              background: '#2c2c36', 
              padding: '6px 12px', 
              borderRadius: 20, 
              border: '1px solid var(--color-border)' 
            }}>
              <span style={{ fontWeight: 700, color: '#2ecc71', marginRight: 8 }}>{balanceLabel} USDb</span>
              <div style={{ display: 'flex', gap: 4 }}>
                <button 
                  onClick={() => setAction('deposit')}
                  style={{ background: 'var(--color-primary)', border: 'none', borderRadius: 4, width: 22, height: 22, cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'black' }}
                  title="Deposit"
                >＋</button>
                 <button 
                  onClick={() => setAction('withdraw')}
                  style={{ background: '#555', border: 'none', borderRadius: 4, width: 22, height: 22, cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'white' }}
                  title="Withdraw"
                >-</button>
              </div>
            </div>

            <div style={{ position: 'relative' }} ref={profileRef}>
              <button 
                onClick={() => setIsProfileOpen(!isProfileOpen)}
                style={{
                  background: 'linear-gradient(135deg, #1e3c72 0%, #2a5298 100%)',
                  border: 'none',
                  borderRadius: '50%',
                  width: 36,
                  height: 36,
                  cursor: 'pointer',
                  overflow: 'hidden',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  color: 'white',
                  fontWeight: 600,
                  fontSize: 14
                }}
              >
                {address.substring(2, 4).toUpperCase()}
              </button>
              
              {isProfileOpen && (
                <div style={{
                  position: 'absolute',
                  top: '120%',
                  right: 0,
                  width: 200,
                  background: 'var(--color-card-bg)',
                  border: '1px solid var(--color-border)',
                  borderRadius: 12,
                  boxShadow: '0 4px 20px rgba(0,0,0,0.5)',
                  padding: 8,
                  zIndex: 200
                }}>
                   <div style={{ padding: '8px 12px', borderBottom: '1px solid var(--color-border)', marginBottom: 8, color: '#888', fontSize: 12, overflow: 'hidden', textOverflow: 'ellipsis' }}>
                    {address}
                   </div>
                   <button 
                     onClick={() => { navigate('/profile'); setIsProfileOpen(false); }}
                     style={{ width: '100%', textAlign: 'left', background: 'transparent', border: 'none', color: 'white', padding: '10px 12px', cursor: 'pointer', borderRadius: 6, display: 'flex', alignItems: 'center', gap: 8 }}
                     className="hover-bg"
                   >
                     👤 Мій профіль
                   </button>
                   <button 
                     onClick={() => { disconnect(); setIsProfileOpen(false); }}
                     style={{ width: '100%', textAlign: 'left', background: 'transparent', border: 'none', color: '#e74c3c', padding: '10px 12px', cursor: 'pointer', borderRadius: 6, display: 'flex', alignItems: 'center', gap: 8 }}
                     className="hover-bg"
                   >
                     🔌 Відключитись
                   </button>
                </div>
              )}
            </div>
          </>
        )}
      </div>

       {/* Modal for Deposit/Withdraw */}
       {action && (
         <div className="modal-overlay" onClick={() => setAction(null)}>
           <div className="modal" onClick={e => e.stopPropagation()}>
             <h3 style={{ marginTop: 0, color: 'var(--color-primary)' }}>
               {action === 'deposit' ? 'Поповнити USDb' : 'Вивести USDb'}
             </h3>
             
             {needsApprove && (
               <div style={{ background: 'rgba(231, 76, 60, 0.15)', color: '#e74c3c', padding: 12, borderRadius: 8, fontSize: 13, marginBottom: 16 }}>
                 ℹ️ Для депозиту спочатку потрібно підтвердити використання токенів (Approve).
               </div>
             )}

             <div style={{ marginBottom: 16 }}>
               <label style={{ display: 'block', fontSize: 13, color: '#aaa', marginBottom: 6 }}>Сума</label>
               <input 
                 autoFocus
                 type="number" 
                 value={amountInput}
                 onChange={e => setAmountInput(e.target.value)}
                 style={{ 
                   width: '93%', 
                   background: '#101014', 
                   border: '1px solid #333', 
                   color: 'white', 
                   padding: 12, 
                   borderRadius: 8, 
                   fontSize: 16 
                  }}
               />
             </div>

             <div style={{ display: 'flex', gap: 10, justifyContent: 'flex-end' }}>
               <button 
                onClick={() => setAction(null)}
                style={{ background: 'transparent', border: 'none', color: '#888', cursor: 'pointer', padding: '8px 16px' }}
               >
                 Скасувати
               </button>
               <button 
                 className="btn-primary" 
                 onClick={submit} 
                 disabled={isLoading}
               >
                 {isLoading 
                    ? 'Обробка...' 
                    : needsApprove ? 'Approve' : (action === 'deposit' ? 'Deposit' : 'Withdraw')
                 }
               </button>
             </div>

             {txHash && (
               <div style={{ marginTop: 12, fontSize: 12, color: 'var(--color-success)', wordBreak: 'break-all' }}>
                 Tx: {txHash}
               </div>
             )}
             {error && (
               <div style={{ marginTop: 12, fontSize: 12, color: '#e74c3c' }}>
                 Error: {error}
               </div>
             )}
           </div>
         </div>
       )}
    </header>
  );
}

export default HeaderBar;

