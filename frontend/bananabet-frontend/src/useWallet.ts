import { useCallback, useEffect, useMemo, useState, useRef } from 'react';
import { BrowserProvider, Contract, formatUnits } from 'ethers';
import { BANANABET_CONTRACT_ADDRESS, TOKEN_DECIMALS as CONTRACT_TOKEN_DECIMALS } from './services/contracts';

type Ethereum = {
  isMetaMask?: boolean;
  request: (request: { method: string; params?: any[] | Record<string, any> }) => Promise<any>;
  on?: (event: string, handler: (...args: any[]) => void) => void;
  removeListener?: (event: string, handler: (...args: any[]) => void) => void;
};

declare global {
  interface Window {
    ethereum?: Ethereum;
  }
}

const SEPOLIA_CHAIN_ID = 11155111;
const SEPOLIA_CHAIN_ID_HEX = '0xaa36a7';
const TOKEN_DECIMALS = CONTRACT_TOKEN_DECIMALS;
const BANANA_BET_ABI = ['function balances(address user) view returns (uint256)'];

export type WalletState = {
  address: string | null;
  chainId: number | null;
  balance: string | null;
  isSepolia: boolean;
  hasProvider: boolean;
  isConnecting: boolean;
  error: string | null;
  connect: () => Promise<void>;
  disconnect: () => void;
  refreshBalance: () => Promise<void>;
};

const ensureContractAddress = () => {
  if (!BANANABET_CONTRACT_ADDRESS) {
    throw new Error('Set VITE_BANANABET_CONTRACT_ADDRESS in your environment (.env) first.');
  }
  return BANANABET_CONTRACT_ADDRESS;
};

export function useWallet(): WalletState {
  const [address, setAddress] = useState<string | null>(null);
  const [chainId, setChainId] = useState<number | null>(null);
  const [balance, setBalance] = useState<string | null>(null);
  const [isConnecting, setIsConnecting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const hasProvider = useMemo(
    () => typeof window !== 'undefined' && Boolean(window.ethereum),
    [],
  );

  const getProvider = useCallback(() => {
    if (!window.ethereum) {
      throw new Error('MetaMask is not available in this browser.');
    }
    return new BrowserProvider(window.ethereum);
  }, []);

  const ensureSepolia = useCallback(
    async (provider: BrowserProvider) => {
      const network = await provider.getNetwork();
      const currentId = Number(network.chainId);
      setChainId(currentId);

      if (currentId === SEPOLIA_CHAIN_ID) {
        return true;
      }

      if (!window.ethereum?.request) {
        setError('Please switch MetaMask to the Sepolia network.');
        return false;
      }

      try {
        await window.ethereum.request({
          method: 'wallet_switchEthereumChain',
          params: [{ chainId: SEPOLIA_CHAIN_ID_HEX }],
        });

        const updated = await provider.getNetwork();
        const updatedId = Number(updated.chainId);
        setChainId(updatedId);

        return updatedId === SEPOLIA_CHAIN_ID;
      } catch (switchError) {
        setError('Switch to the Sepolia network in MetaMask to continue.');
        return false;
      }
    },
    [],
  );

  const refreshBalance = useCallback(
    async (accountOverride?: string) => {
      const userAddress = accountOverride ?? address;
      if (!userAddress) return;

      const provider = getProvider();
      const onSepolia = await ensureSepolia(provider);
      if (!onSepolia) return;

      try {
        const contract = new Contract(ensureContractAddress(), BANANA_BET_ABI, provider);
        const rawBalance: bigint = await contract.balances(userAddress);
        setBalance(formatUnits(rawBalance, TOKEN_DECIMALS));
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to read balance.');
      }
    },
    [address, ensureSepolia, getProvider],
  );

  const connect = useCallback(async () => {
    setIsConnecting(true);
    setError(null);

    try {
      const provider = getProvider();
      const accounts = (await provider.send('eth_requestAccounts', [])) as string[];
      const primary = accounts?.[0];

      if (!primary) {
        throw new Error('No MetaMask accounts available.');
      }

      setAddress(primary);

      const onSepolia = await ensureSepolia(provider);
      if (!onSepolia) return;

      localStorage.setItem('isWalletConnected', 'true');
      await refreshBalance(primary);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to connect wallet.');
    } finally {
      setIsConnecting(false);
    }
  }, [ensureSepolia, getProvider, refreshBalance]);

  const disconnect = useCallback(() => {
    localStorage.removeItem('isWalletConnected');
    setAddress(null);
    setBalance(null);
    setChainId(null);
    setError(null);
    // Если хотите тотально сбрасывать MetaMask connection (убрать разрешения)
    // можно раскомментировать следующую строчку
    // window.location.reload();
  }, []);

  useEffect(() => {
    const ethereum = window.ethereum;
    if (!ethereum?.on) return;

    const handleAccountsChanged = (...args: any[]) => {
      const accounts = args[0] as string[] | undefined;
      const next = accounts?.[0];
      setAddress(next ?? null);
      setBalance(null);

      if (next) {
        void refreshBalance(next);
      }
    };

    const handleChainChanged = (...args: any[]) => {
      const nextChainId = args[0] as string;
      const nextId = parseInt(nextChainId, 16);
      setChainId(nextId);

      if (nextId === SEPOLIA_CHAIN_ID && address) {
        void refreshBalance(address);
      } else if (nextId !== SEPOLIA_CHAIN_ID) {
        setError('Please switch to the Sepolia network.');
      }
    };

    ethereum.on('accountsChanged', handleAccountsChanged);
    ethereum.on('chainChanged', handleChainChanged);

    return () => {
      ethereum.removeListener?.('accountsChanged', handleAccountsChanged);
      ethereum.removeListener?.('chainChanged', handleChainChanged);
    };
  }, [address, refreshBalance]);

  // Persist connection on mount
  const checkedRef = useRef(false);
  useEffect(() => {
    if (checkedRef.current) return;
    checkedRef.current = true;

    const checkConnection = async () => {
      // Only reconnect automatically if we were connected before
      if (localStorage.getItem('isWalletConnected') !== 'true') return;
      
      try {
        const provider = getProvider();
        const accounts = await provider.send('eth_accounts', []);
        if (accounts && accounts.length > 0) {
           const primary = accounts[0];
           setAddress(primary);
           await ensureSepolia(provider);
           await refreshBalance(primary);
        }
      } catch (e) {
        // Ignore errors if just checking status
      }
    };
    checkConnection();
  }, [getProvider, ensureSepolia, refreshBalance]);

  const isSepolia = chainId === SEPOLIA_CHAIN_ID;

  return {
    address,
    chainId,
    balance,
    isSepolia,
    hasProvider,
    isConnecting,
    error,
    connect,
    disconnect,
    refreshBalance: () => refreshBalance(),
  };
}

export default useWallet;

