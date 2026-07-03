#!/usr/bin/env python3
import json
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LIVEOPS_DIR = os.path.join(ROOT, "LiveOps")
ANALYTICS_DIR = os.path.join(ROOT, "Analytics")
os.makedirs(LIVEOPS_DIR, exist_ok=True)
os.makedirs(ANALYTICS_DIR, exist_ok=True)

live_events = {
    "season": {"id": "S1_2026", "title": "Season 1: Blood Ring Awakening", "startDate": "2026-07-01", "endDate": "2026-09-30", "tierCount": 50},
    "events": [
        {"id": "EVT_001", "name": "Weekend Double XP Royale", "type": "XP_BOOST", "multiplier": 2.0, "status": "ACTIVE"},
        {"id": "EVT_002", "name": "Clan Wars Championship", "type": "TOURNAMENT", "rewardPool": "50000 BloodCoins + Apex Legend Trophy", "status": "UPCOMING"},
        {"id": "EVT_003", "name": "Cyber Beatmaster Login Frenzy", "type": "LOGIN_REWARDS", "durationDays": 7, "status": "ACTIVE"}
    ],
    "battlePass": {
        "priceCoins": 950,
        "instantRewards": ["Character: DJNeon", "Skin: CyberVortex AK47", "Emote: Breakdance"],
        "maxLevel": 100
    }
}
with open(os.path.join(LIVEOPS_DIR, "live_events_schedule.json"), "w") as f:
    json.dump(live_events, f, indent=2)

store_rotation = {
    "featuredBundle": {"id": "BND_APEX_WOLF", "name": "Armored Jeep Wolf & Shadow Pack", "priceDiamonds": 1200, "discountPercent": 25},
    "luckySpin": {
        "costDiamonds": 50,
        "prizes": [
            {"item": "Legendary AWM Phantom", "oddsPercent": 1.5},
            {"item": "Character: Zero", "oddsPercent": 5.0},
            {"item": "500 BloodCoins", "oddsPercent": 30.0},
            {"item": "Rare Weapon Crate", "oddsPercent": 63.5}
        ]
    },
    "dailyDeals": [
        {"item": "10x Smoke Grenades", "priceCoins": 200},
        {"item": "Boost Serum (Speed)", "priceCoins": 350}
    ]
}
with open(os.path.join(LIVEOPS_DIR, "store_rotation_config.json"), "w") as f:
    json.dump(store_rotation, f, indent=2)

retention_config = {
    "dailyLoginStreak": [
        {"day": 1, "reward": "100 BloodCoins"},
        {"day": 2, "reward": "2x Frag Grenade Crate"},
        {"day": 3, "reward": "250 BloodCoins"},
        {"day": 4, "reward": "3-Day Double XP Voucher"},
        {"day": 5, "reward": "500 BloodCoins"},
        {"day": 6, "reward": "Rare Emote: Victory Flex"},
        {"day": 7, "reward": "50 Diamonds + Legendary Mystery Box"}
    ],
    "returnToGameBonus": {"daysOffline": 14, "reward": "Welcome Back Heroic Crate + 1000 Coins"}
}
with open(os.path.join(LIVEOPS_DIR, "retention_rewards.json"), "w") as f:
    json.dump(retention_config, f, indent=2)

telemetry_schema = {
    "schemaVersion": "2.0.0",
    "events": {
        "match_start": {"fields": ["match_id", "user_id", "game_mode", "region", "is_ranked", "timestamp"]},
        "match_end": {"fields": ["match_id", "user_id", "placement", "kills", "damage_dealt", "duration_sec", "xp_earned", "coins_earned"]},
        "combat_kill": {"fields": ["match_id", "killer_id", "victim_id", "weapon_used", "distance_meters", "is_headshot"]},
        "power_activate": {"fields": ["match_id", "user_id", "power_type", "location_x", "location_z"]},
        "vehicle_enter": {"fields": ["match_id", "user_id", "vehicle_type", "seat_index"]}
    }
}
with open(os.path.join(ANALYTICS_DIR, "telemetry_schema.json"), "w") as f:
    json.dump(telemetry_schema, f, indent=2)

economy_schema = {
    "schemaVersion": "2.0.0",
    "events": {
        "currency_flow": {"fields": ["user_id", "currency_type", "amount", "flow_type", "reason", "balance_after"]},
        "store_purchase": {"fields": ["user_id", "item_id", "cost_amount", "currency_type", "timestamp"]},
        "lucky_spin": {"fields": ["user_id", "cost_diamonds", "prize_won", "timestamp"]}
    }
}
with open(os.path.join(ANALYTICS_DIR, "economy_events_schema.json"), "w") as f:
    json.dump(economy_schema, f, indent=2)

kpi_dashboard = {
    "targetKPIs": {
        "D1_Retention": ">= 45.0%",
        "D7_Retention": ">= 22.0%",
        "D30_Retention": ">= 10.0%",
        "Target_FPS": "60.0 FPS stable on mid-tier Android (Snapdragon 720G+)",
        "Max_Memory_MB": "<= 850 MB",
        "Load_Time_Sec": "<= 3.5 sec from Launch to Lobby"
    },
    "automatedAlerts": ["FPS drop below 50 in 3+ consecutive sessions", "Matchmaking queue time exceeding 15 sec", "API rate limit violations spike > 5%"]
}
with open(os.path.join(ANALYTICS_DIR, "retention_kpi_dashboard.json"), "w") as f:
    json.dump(kpi_dashboard, f, indent=2)

print("Successfully generated production configurations in /LiveOps and /Analytics.")
