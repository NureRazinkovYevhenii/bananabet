import { BrowserProvider, Contract } from 'ethers';
import type { ContractRunner } from 'ethers';

export const USDB_CONTRACT_ADDRESS = import.meta.env.VITE_USDB_CONTRACT_ADDRESS ?? '';
export const BANANABET_CONTRACT_ADDRESS = import.meta.env.VITE_BANANABET_CONTRACT_ADDRESS ?? '';

// BananaUSD uses 6 decimals (see BananaUSD.sol)
export const TOKEN_DECIMALS = 6;
export const MAX_APPROVE_AMOUNT = 1_000_000n * 10n ** 6n; // 1M tokens

export const USDB_ABI = [
  'function approve(address spender, uint256 amount) returns (bool)',
  'function allowance(address owner, address spender) view returns (uint256)',
  'function balanceOf(address owner) view returns (uint256)',
] as const;

export const BANANABET_ABI = [
  'function deposit(uint256 amount)',
  'function withdraw(uint256 amount)',
  'function balances(address user) view returns (uint256)',
  'function placeBet(uint256 externalId, uint8 side, uint256 amount)',
  'function claim(uint256 externalId)',
] as const;

const assertAddress = (value: string, name: string) => {
  if (!value) {
    throw new Error(`Missing ${name}. Set it in .env (VITE_${name}).`);
  }
  return value;
};

export const getProvider = () => {
  if (!window.ethereum) {
    throw new Error('MetaMask is not available in this browser.');
  }
  return new BrowserProvider(window.ethereum);
};

export const getSigner = async () => {
  const provider = getProvider();
  return provider.getSigner();
};

export const getUsdbContract = (runner: ContractRunner) =>
  new Contract(
    assertAddress(USDB_CONTRACT_ADDRESS, 'USDB_CONTRACT_ADDRESS'),
    USDB_ABI,
    runner,
  );

export const getBananaBetContract = (runner: ContractRunner) =>
  new Contract(
    assertAddress(BANANABET_CONTRACT_ADDRESS, 'BANANABET_CONTRACT_ADDRESS'),
    BANANABET_ABI,
    runner,
  );
