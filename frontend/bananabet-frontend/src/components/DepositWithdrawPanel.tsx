import { useEffect, useMemo, useState } from 'react';
import { formatUnits, parseUnits } from 'ethers';
import { type WalletState } from '../useWallet';
import {
  BANANABET_CONTRACT_ADDRESS,
  MAX_APPROVE_AMOUNT,
  TOKEN_DECIMALS,
  getBananaBetContract,
  getProvider,
  getSigner,
  getUsdbContract,
} from '../services/contracts';

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

function DepositWithdrawPanel({ wallet }: Props) {
  const { address, isSepolia, connect } = wallet;

  const [amountInput, setAmountInput] = useState('');
  const [usdbBalance, setUsdbBalance] = useState<bigint>(0n);
  const [bananaBalance, setBananaBalance] = useState<bigint>(0n);
  const [allowance, setAllowance] = useState<bigint>(0n);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [txHash, setTxHash] = useState<string | null>(null);

  const amountWei = useMemo(() => safeParseAmount(amountInput), [amountInput]);
  const needsApprove = useMemo(
    () => Boolean(amountWei && amountWei > 0n && allowance < amountWei),
    [allowance, amountWei],
  );

  const ensureConnected = () => {
    if (!address) {
      throw new Error('Підключіть MetaMask перед дією.');
    }
    if (!isSepolia) {
      throw new Error('Переключіть мережу на Sepolia.');
    }
  };

  const refreshAll = async () => {
    if (!address) return;
    try {
      const provider = getProvider();
      const usdb = getUsdbContract(provider);
      const banana = getBananaBetContract(provider);

      const [balUsdb, balBanana, userAllowance] = await Promise.all([
        usdb.balanceOf(address) as Promise<bigint>,
        banana.balanceOf(address) as Promise<bigint>,
        usdb.allowance(address, BANANABET_CONTRACT_ADDRESS) as Promise<bigint>,
      ]);

      setUsdbBalance(balUsdb);
      setBananaBalance(balBanana);
      setAllowance(userAllowance);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Не вдалося оновити дані.');
    }
  };

  useEffect(() => {
    void refreshAll();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [address]);

  const approveIfNeeded = async (required: bigint) => {
    if (allowance >= required) return;
    const signer = await getSigner();
    const usdb = getUsdbContract(signer);
    const tx = await usdb.approve(BANANABET_CONTRACT_ADDRESS, MAX_APPROVE_AMOUNT);
    setTxHash(tx.hash);
    await tx.wait(1);
    const updated = await usdb.allowance(address, BANANABET_CONTRACT_ADDRESS);
    setAllowance(updated);
  };

  const handleApprove = async () => {
    setError(null);
    setTxHash(null);
    if (!amountWei || amountWei <= 0n) {
      setError('Введіть коректну суму для approve.');
      return;
    }

    try {
      ensureConnected();
      setIsLoading(true);
      await approveIfNeeded(amountWei);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Помилка approve.');
    } finally {
      setIsLoading(false);
    }
  };

  const handleDeposit = async () => {
    setError(null);
    setTxHash(null);
    if (!amountWei || amountWei <= 0n) {
      setError('Введіть коректну суму для депозиту.');
      return;
    }

    try {
      ensureConnected();
      setIsLoading(true);

      await approveIfNeeded(amountWei);

      const signer = await getSigner();
      const banana = getBananaBetContract(signer);
      const tx = await banana.deposit(amountWei);
      setTxHash(tx.hash);
      await tx.wait(1);
      await refreshAll();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Помилка депозиту.');
    } finally {
      setIsLoading(false);
    }
  };

  const handleWithdraw = async () => {
    setError(null);
    setTxHash(null);
    if (!amountWei || amountWei <= 0n) {
      setError('Введіть коректну суму для виводу.');
      return;
    }

    try {
      ensureConnected();
      setIsLoading(true);

      const signer = await getSigner();
      const banana = getBananaBetContract(signer);
      const tx = await banana.withdraw(amountWei);
      setTxHash(tx.hash);
      await tx.wait(1);
      await refreshAll();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Помилка виводу.');
    } finally {
      setIsLoading(false);
    }
  };

  const canTransact = Boolean(address && isSepolia && !isLoading && amountWei && amountWei > 0n);

  return (
    <section style={{ marginTop: 24, padding: 16, border: '1px solid #e0e0e0', borderRadius: 12 }}>
      <h2>USDb ↔ BananaBet</h2>
      <p style={{ marginTop: 4, color: '#555' }}>
        Approve один раз, далі — депозит/вивід без зайвих транзакцій.
      </p>

      {!address && (
        <button onClick={connect} style={{ marginTop: 12 }}>
          Підключити MetaMask
        </button>
      )}

      {address && (
        <>
          <div style={{ display: 'grid', gap: 12, marginTop: 12 }}>
            <label style={{ display: 'grid', gap: 4 }}>
              Сума (USDb)
              <input
                type="text"
                inputMode="decimal"
                placeholder="0.0"
                value={amountInput}
                onChange={(e) => setAmountInput(e.target.value)}
                style={{ padding: 8, borderRadius: 8, border: '1px solid #ccc' }}
              />
            </label>

            <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
              {needsApprove && (
                <button onClick={handleApprove} disabled={!canTransact}>
                  Approve USDb
                </button>
              )}
              <button onClick={handleDeposit} disabled={!canTransact}>
                Deposit
              </button>
              <button onClick={handleWithdraw} disabled={!canTransact}>
                Withdraw
              </button>
            </div>

            <div style={{ padding: 12, border: '1px solid #eee', borderRadius: 8 }}>
              <p>
                <strong>USDb balance:</strong> {formatUnits(usdbBalance, TOKEN_DECIMALS)}
              </p>
              <p>
                <strong>BananaBet balance:</strong> {formatUnits(bananaBalance, TOKEN_DECIMALS)}
              </p>
              <p>
                <strong>Allowance:</strong> {formatUnits(allowance, TOKEN_DECIMALS)}
              </p>
            </div>

            {txHash && (
              <p style={{ color: '#2d7' }}>
                Tx sent: <span style={{ wordBreak: 'break-all' }}>{txHash}</span>
              </p>
            )}

            {error && <p style={{ color: 'crimson' }}>{error}</p>}
          </div>
        </>
      )}
    </section>
  );
}

export default DepositWithdrawPanel;

