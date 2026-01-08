import json
import joblib
import numpy as np
import pandas as pd
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field


MODEL_PATH = "model_v2.pkl"
SCALER_PATH = "scaler_v2.pkl"
MODEL_INFO_PATH = "model_info_v2.json"




EPS = 1e-6

FEATURES_ORDER = [
    "Elo_Diff_Norm",
    "Elo_Signed_Sqrt",
    "Adj_Shots_Diff",
    "Form3_Diff",
    "Form5_Diff"
]


app = FastAPI(
    title="BananaBet ML Odds Service",
    version="2.0.0",
    description="ElasticNet-based ML oracle for P2P betting odds"
)

model = None
scaler = None
model_info = {}


def sigmoid(x: float) -> float:
    return 1.0 / (1.0 + np.exp(-x))


@app.on_event("startup")
def load_artifacts():
    global model, scaler, model_info

    try:
        model = joblib.load(MODEL_PATH)
        scaler = joblib.load(SCALER_PATH)

        with open(MODEL_INFO_PATH, "r") as f:
            model_info = json.load(f)

        print("✅ ML artifacts loaded successfully")

    except Exception as e:
        print(f"❌ Failed to load ML artifacts: {e}")
        model = None
        scaler = None


class MatchFeatures(BaseModel):
    Elo_Diff_Norm: float = Field(..., alias="elo_Diff_Norm")
    Elo_Signed_Sqrt: float = Field(..., alias="elo_Signed_Sqrt")
    Adj_Shots_Diff: float = Field(..., alias="adj_Shots_Diff")
    Form3_Diff: float = Field(..., alias="form3_Diff")
    Form5_Diff: float = Field(..., alias="form5_Diff")

    class Config:
        allow_population_by_field_name = True

class PredictionResponse(BaseModel):
    home_win_prob: float
    away_win_prob: float
    fair_odd_home: float
    fair_odd_away: float


@app.get("/health")
def health():
    if model is None or scaler is None:
        return {"status": "error", "model_loaded": False}
    return {"status": "ok", "model_loaded": True}

@app.get("/model-info")
def get_model_info():
    if not model_info:
        raise HTTPException(status_code=500, detail="Model info not available")
    return model_info

@app.post("/predict", response_model=PredictionResponse)
def predict(data: MatchFeatures):
    if model is None or scaler is None:
        raise HTTPException(status_code=500, detail="Model not loaded")

    # INPUT → DATAFRAME
    input_df = pd.DataFrame([[ 
        getattr(data, f) for f in FEATURES_ORDER
    ]], columns=FEATURES_ORDER)

    # SCALE
    X_scaled = scaler.transform(input_df)

    # PREDICT LOG-ODDS
    logit = model.predict(X_scaled)[0]

    # LOGIT → PROBABILITY
    prob_home = sigmoid(logit)
    prob_home = float(np.clip(prob_home, EPS, 1 - EPS))
    prob_away = 1.0 - prob_home

    # FAIR ODDS
    odd_home = round(1.0 / prob_home, 2)
    odd_away = round(1.0 / prob_away, 2)

    return PredictionResponse(
        home_win_prob=round(prob_home, 4),
        away_win_prob=round(prob_away, 4),
        fair_odd_home=odd_home,
        fair_odd_away=odd_away
    )
