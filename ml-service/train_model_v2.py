import pandas as pd
import numpy as np
import joblib
import json

from sklearn.preprocessing import StandardScaler
from sklearn.linear_model import ElasticNet
from sklearn.metrics import mean_absolute_error, log_loss

DATA_FILE = "m_final_v2.csv"
TEST_SIZE = 5000
EPS = 1e-6

def continuous_log_loss(y_true, y_pred, eps=1e-6):
    y_pred = np.clip(y_pred, eps, 1 - eps)
    return -np.mean(
        y_true * np.log(y_pred) +
        (1 - y_true) * np.log(1 - y_pred)
    )

def logit(p):
    return np.log(p / (1 - p))

def sigmoid(x):
    return 1 / (1 + np.exp(-x))

def train():
    df = pd.read_csv(DATA_FILE)
    df['Date'] = pd.to_datetime(df['Date'])
    df = df.sort_values('Date').reset_index(drop=True)

    features = [
        'Elo_Diff_Norm',
        'Elo_Signed_Sqrt',
        'Adj_Shots_Diff',
        'Form3_Diff',
        'Form5_Diff'
    ]

    X = df[features]
    p = df['p_target_p2p'].clip(EPS, 1 - EPS)
    y = logit(p)

    X_train, X_test = X.iloc[:-TEST_SIZE], X.iloc[-TEST_SIZE:]
    y_train, y_test = y.iloc[:-TEST_SIZE], y.iloc[-TEST_SIZE:]
    df_test = df.iloc[-TEST_SIZE:].copy()

    scaler = StandardScaler()
    X_train_s = scaler.fit_transform(X_train)
    X_test_s = scaler.transform(X_test)

    model = ElasticNet(
        alpha=0.01,
        l1_ratio=0.2,
        max_iter=10000,
        random_state=42
    )

    model.fit(X_train_s, y_train)

    pred_logit = model.predict(X_test_s)
    pred_prob = sigmoid(pred_logit)
    pred_odd = 1 / pred_prob

    mae = mean_absolute_error(df_test['p_target_p2p'], pred_prob)
    ll = continuous_log_loss(df_test['p_target_p2p'].values, pred_prob)

    mask = df_test['OddHome_P2P_Real'] < 20
    odds_err = np.abs(pred_odd[mask] - df_test['OddHome_P2P_Real'][mask]).mean()

    print("\n" + "=" * 45)
    print("🏆 ML v2 RESULTS")
    print("=" * 45)
    print(f"📉 MAE:        {mae:.6f}")
    print(f"📊 LogLoss:    {ll:.6f}")
    print(f"💰 Odds Error: {odds_err:.4f}")

    coef_df = pd.DataFrame({
        'Feature': features,
        'Weight': model.coef_,
        'Abs': np.abs(model.coef_)
    }).sort_values('Abs', ascending=False)

    print("\n⚖️ FEATURE IMPORTANCE:")
    print(coef_df[['Feature', 'Weight']].to_string(index=False))

    print("\n🔍 LAST 25 MATCHES")
    print("-" * 115)
    print(f"{'Date':<11} | {'Match':<30} | {'Market':<6} | {'Model':<6} | {'Diff':<6} | {'Prob%':<6}")
    print("-" * 115)

    df_test['Model_Odd'] = pred_odd
    df_test['Model_Prob'] = pred_prob

    for _, r in df_test.tail(25).iterrows():
        print(
            f"{r['Date'].date()} | "
            f"{r['HomeTeam']} vs {r['AwayTeam']:<20} | "
            f"{r['OddHome_P2P_Real']:<6.2f} | "
            f"{r['Model_Odd']:<6.2f} | "
            f"{(r['Model_Odd'] - r['OddHome_P2P_Real']):<+6.2f} | "
            f"{r['Model_Prob']*100:<5.1f}%"
        )

    joblib.dump(model, "model_v2.pkl")
    joblib.dump(scaler, "scaler_v2.pkl")

    with open("model_info_v2.json", "w") as f:
        json.dump({
            "model": "ElasticNet(log-odds)",
            "features": features,
            "train_size": len(X_train),
            "test_size": len(X_test),
            "mae": mae,
            "log_loss": ll,
            "odds_error": odds_err
        }, f, indent=2)

    print("\n💾 Artifacts saved")

if __name__ == "__main__":
    train()
