import pandas as pd
import numpy as np

INPUT_FILE = "m.csv"
OUTPUT_FILE = "m_final_v2.csv"
WINDOW = 10
AVG_ELO = 1500.0

def prepare():
    print(f"🔄 Load {INPUT_FILE}")
    df = pd.read_csv(INPUT_FILE, low_memory=False)

    # DATE
    df['Date'] = pd.to_datetime(df.get('MatchDate', df.get('Date')), errors='coerce')
    df = df.sort_values('Date').reset_index(drop=True)

    # NUMERIC
    num_cols = [
        'OddHome', 'OddAway', 'OddDraw',
        'HomeElo', 'AwayElo',
        'HomeTarget', 'AwayTarget'
    ]
    for c in num_cols:
        if c in df.columns:
            df[c] = pd.to_numeric(df[c], errors='coerce')

    # =========================
    # P2P TARGET (MARKET)
    # =========================
    df = df.dropna(subset=['OddHome', 'OddAway', 'OddDraw'])

    p_h = 1 / df['OddHome']
    p_a = 1 / df['OddAway']
    p_d = 1 / df['OddDraw']

    total = p_h + p_a + p_d
    p_h_norm = p_h / total
    p_a_norm = p_a / total

    df['p_target_p2p'] = p_h_norm / (p_h_norm + p_a_norm)
    df['OddHome_P2P_Real'] = 1 / df['p_target_p2p']

    # =========================
    # ADJUSTED SHOTS
    # =========================
    df['Home_Adj_Shots'] = df['HomeTarget'] * (df['AwayElo'] / AVG_ELO)
    df['Away_Adj_Shots'] = df['AwayTarget'] * (df['HomeElo'] / AVG_ELO)

    home = df[['Date', 'HomeTeam', 'Home_Adj_Shots']].rename(
        columns={'HomeTeam': 'Team', 'Home_Adj_Shots': 'Adj'}
    )
    away = df[['Date', 'AwayTeam', 'Away_Adj_Shots']].rename(
        columns={'AwayTeam': 'Team', 'Away_Adj_Shots': 'Adj'}
    )

    all_games = pd.concat([home, away]).sort_values('Date')

    all_games['Roll_Adj'] = (
        all_games
        .groupby('Team')['Adj']
        .transform(lambda x: x.shift(1).rolling(WINDOW, min_periods=3).mean())
    )

    home_stats = all_games.rename(columns={'Team': 'HomeTeam', 'Roll_Adj': 'Home_Roll'})
    away_stats = all_games.rename(columns={'Team': 'AwayTeam', 'Roll_Adj': 'Away_Roll'})

    df = pd.merge_asof(df, home_stats, on='Date', by='HomeTeam')
    df = pd.merge_asof(df, away_stats, on='Date', by='AwayTeam')

    df['Adj_Shots_Diff'] = df['Home_Roll'] - df['Away_Roll']

    # =========================
    # ELO FEATURES
    # =========================
    df['Elo_Diff_Norm'] = (df['HomeElo'] - df['AwayElo']) / 400.0
    df['Elo_Signed_Sqrt'] = np.sign(df['Elo_Diff_Norm']) * np.sqrt(np.abs(df['Elo_Diff_Norm']))

    # =========================
    # FORM
    # =========================
    for c in ['Form3Home', 'Form3Away', 'Form5Home', 'Form5Away']:
        if c in df.columns:
            df[c] = pd.to_numeric(df[c], errors='coerce').fillna(0)

    df['Form3_Diff'] = df['Form3Home'] - df['Form3Away']
    df['Form5_Diff'] = df['Form5Home'] - df['Form5Away']

    final_cols = [
        'Date', 'HomeTeam', 'AwayTeam',
        'p_target_p2p', 'OddHome_P2P_Real',
        'Elo_Diff_Norm', 'Elo_Signed_Sqrt',
        'Adj_Shots_Diff',
        'Form3_Diff', 'Form5_Diff'
    ]

    df_final = df.dropna(subset=final_cols)[final_cols]
    df_final.to_csv(OUTPUT_FILE, index=False)

    print(f"✅ Saved {OUTPUT_FILE} ({len(df_final)} matches)")

if __name__ == "__main__":
    prepare()
