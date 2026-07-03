#!/usr/bin/env python3
"""
Automated Integration & API Test Runner for BloodRing Backend.
Tests Auth, Profile, Matchmaking, Anti-Cheat, Cloud Save, Store, Missions, and Leaderboards.
"""

import subprocess
import time
import urllib.request
import urllib.error
import json
import sys
import os

print("Installing backend node_modules if missing...")
os.system("cd backend && npm install --no-audit --no-fund --silent")

PORT = 5050
SERVER_CMD = ["node", "backend/server.js"]

print(f"Starting BloodRing backend server on port {PORT}...")
env = os.environ.copy()
env["PORT"] = str(PORT)
proc = subprocess.Popen(SERVER_CMD, env=env, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
time.sleep(3)

def req(endpoint, method="GET", data=None, token=None):
    url = f"http://localhost:{PORT}{endpoint}"
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    
    body = json.dumps(data).encode("utf-8") if data else None
    request = urllib.request.Request(url, data=body, headers=headers, method=method)
    try:
        with urllib.request.urlopen(request, timeout=5) as resp:
            return resp.status, json.loads(resp.read().decode("utf-8"))
    except urllib.error.HTTPError as e:
        return e.code, json.loads(e.read().decode("utf-8"))
    except Exception as e:
        return 0, {"error": str(e)}

try:
    print("1. Testing Root Health Check...")
    status, res = req("/")
    assert status == 200, f"Expected 200, got {status}: {res}"
    print("   [OK] Root health check passed.")

    print("2. Testing Guest Auth...")
    status, res = req("/api/auth/guest", method="POST", data={"device_id": "TEST_DEVICE_001"})
    assert status in [200, 201] and "token" in res, f"Guest login failed: {status} {res}"
    token = res["token"]
    print("   [OK] Guest login passed. Token acquired.")

    print("3. Testing Profile Retrieval...")
    status, res = req("/api/profile/", method="GET", token=token)
    assert status == 200 and "userId" in res, f"Profile fetch failed: {status} {res}"
    print("   [OK] Profile retrieved successfully.")

    print("4. Testing Matchmaking (HOST and JOIN)...")
    status, res = req("/api/match/matchmake", method="POST", data={"action": "HOST", "joinCode": "ROOM777", "gameMode": "CLASSIC", "region": "US"}, token=token)
    assert status in [200, 201] and "lobbyId" in res, f"Host lobby failed: {status} {res}"
    print(f"   [OK] Hosted lobby successfully. Lobby ID: {res.get('lobbyId')}")

    status, res = req("/api/match/matchmake", method="POST", data={"action": "JOIN", "gameMode": "CLASSIC", "region": "US"}, token=token)
    assert status == 200 and "lobbyId" in res, f"Join lobby failed: {status} {res}"
    print(f"   [OK] Joined match successfully. Lobby ID: {res.get('lobbyId')}")

    print("5. Testing Match Result Submission & Rewards...")
    status, res = req("/api/match/result", method="POST", data={"kills": 5, "placement": 1, "damageDealt": 1200, "matchDuration": 600, "gameMode": "CLASSIC", "isRanked": True}, token=token)
    assert status == 200 and "earnedCoins" in res, f"Result submission failed: {status} {res}"
    print(f"   [OK] Match results verified. Earned Coins: {res.get('earnedCoins')}, New Rank Tier: {res.get('newRankTier')}")

    print("6. Testing Store & Missions...")
    status, res = req("/api/store/", method="GET", token=token)
    assert status == 200, f"Store fetch failed: {status} {res}"
    status, res = req("/api/store/missions", method="GET", token=token)
    assert status == 200, f"Missions fetch failed: {status} {res}"
    print("   [OK] Store & Missions retrieved successfully.")

    print("7. Testing Leaderboards...")
    status, res = req("/api/leaderboard/", method="GET")
    assert status == 200, f"Leaderboard fetch failed: {status} {res}"
    print("   [OK] Leaderboards retrieved successfully.")

    print("8. Testing Anti-Cheat Telemetry Log...")
    status, res = req("/api/match/anticheat", method="POST", data={"violationType": "SPEED_HACK_SUSPICION", "details": "Player speed exceeded 15 m/s"}, token=token)
    assert status == 200, f"Anti-cheat logging failed: {status} {res}"
    print("   [OK] Anti-cheat telemetry logged.")

    print("9. Testing Cloud Save Sync...")
    status, res = req("/api/match/cloudsave", method="POST", data={"saveData": '{"loadout": "AK47", "skin": "Gold"}'}, token=token)
    assert status == 200, f"Cloud save sync failed: {status} {res}"
    print("   [OK] Cloud save synced successfully.")

    print("\n================================================================================")
    print("ALL 9 BACKEND INTEGRATION & SERVER AGENT TESTS PASSED WITH 100% SUCCESS!")
    print("================================================================================")

finally:
    proc.terminate()
    proc.wait()
