export type ImbalanceResult = {
  side: 'Home' | 'Away' | 'Balanced';
  betterSide: 'Home' | 'Away' | null; // куди вигідніше ставити, щоб вирівняти
  missingStake: number; // скільки потрібно поставити на betterSide, щоб збалансувати
  liabilityDiff: number; // різниця у потенційних виплатах (homeLiability vs awayLiability)
};

export const computeOrderbookImbalance = (params: {
  totalHome: number;
  totalAway: number;
  oddsHome: number;
  oddsAway: number;
}): ImbalanceResult => {
  const { totalHome, totalAway, oddsHome, oddsAway } = params;

  const homeLiability = totalHome * oddsHome;
  const awayLiability = totalAway * oddsAway;

  if (homeLiability === awayLiability) {
    return { side: 'Balanced', betterSide: null, missingStake: 0, liabilityDiff: 0 };
  }

  if (homeLiability > awayLiability) {
    const diff = homeLiability - awayLiability;
    const missingStake = oddsAway > 0 ? diff / oddsAway : 0;
    return {
      side: 'Home',
      betterSide: 'Away',
      missingStake,
      liabilityDiff: diff,
    };
  }

  const diff = awayLiability - homeLiability;
  const missingStake = oddsHome > 0 ? diff / oddsHome : 0;
  return {
    side: 'Away',
    betterSide: 'Home',
    missingStake,
    liabilityDiff: diff,
  };
};

