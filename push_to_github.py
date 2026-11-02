#!/usr/bin/env python3
"""
Automated Git & GitHub DevOps Deployment Script
Repository: drone-flight-simulator
Zero External Dependencies (Standard Python Library Only)
"""

import sys
import os
import json
import getpass
import subprocess
import urllib.request
import urllib.error

REPO_NAME = "drone-flight-simulator"
REPO_DESC = "3D Open World Drone Flight Simulator with GLTF Assets, AABB Collisions, and WebGL Engine"

def run_cmd(cmd, check=True):
    """Executes a local shell command and returns output."""
    res = subprocess.run(cmd, shell=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
    if check and res.returncode != 0:
        print(f"[!] Command failed: {cmd}\n[Error]: {res.stderr.strip()}", file=sys.stderr)
    return res

def github_api_request(url, token, data=None, method="GET"):
    """Performs an authenticated request to the GitHub REST API."""
    headers = {
        "Authorization": f"token {token}",
        "Accept": "application/vnd.github.v3+json",
        "User-Agent": "Drone-Simulator-Deployer"
    }
    payload = json.dumps(data).encode("utf-8") if data else None
    req = urllib.request.Request(url, data=payload, headers=headers, method=method)
    try:
        with urllib.request.urlopen(req) as resp:
            return resp.status, json.loads(resp.read().decode("utf-8"))
    except urllib.error.HTTPError as e:
        error_body = e.read().decode("utf-8")
        try:
            err_json = json.loads(error_body)
            return e.code, err_json
        except Exception:
            return e.code, {"message": error_body}

def main():
    print("=" * 65)
    print(" 🚁 DEVOPS GITHUB DEPLOYMENT: 3D DRONE FLIGHT SIMULATOR")
    print("=" * 65)

    # 1. Prompt for GitHub Personal Access Token
    token = os.environ.get("GITHUB_TOKEN")
    if not token:
        print("\nEnter your GitHub Personal Access Token (PAT).")
        print("Note: Token requires 'repo' permissions to create and push.")
        token = getpass.getpass("GitHub PAT: ").strip()

    if not token:
        print("[X] Error: Token cannot be empty. Aborting.", file=sys.stderr)
        sys.exit(1)

    # 2. Verify Authenticated User
    print("\n[*] Authenticating with GitHub API...")
    status, user_data = github_api_request("https://api.github.com/user", token)
    if status != 200:
        print(f"[X] Authentication failed (HTTP {status}): {user_data.get('message')}", file=sys.stderr)
        sys.exit(1)

    username = user_data.get("login")
    print(f"[✓] Authenticated successfully as: @{username}")

    # 3. Create Remote Repository if it doesn't already exist
    print(f"[*] Checking/Creating repository '{REPO_NAME}'...")
    repo_url_api = f"https://api.github.com/repos/{username}/{REPO_NAME}"
    check_status, _ = github_api_request(repo_url_api, token)

    if check_status == 200:
        print(f"[i] Repository '{REPO_NAME}' already exists under @{username}.")
    else:
        create_payload = {
            "name": REPO_NAME,
            "description": REPO_DESC,
            "private": False,
            "has_issues": True,
            "has_projects": True,
            "has_wiki": False
        }
        create_status, create_resp = github_api_request("https://api.github.com/user/repos", token, data=create_payload, method="POST")
        if create_status in [201, 200]:
            print(f"[✓] Remote repository '{REPO_NAME}' created successfully.")
        else:
            print(f"[!] Warning on repo creation: {create_resp.get('message')}")

    # 4. Configure Local Git Remote and Push
    print("\n[*] Preparing Git stage and remote origin...")
    run_cmd("git init", check=False)
    run_cmd("git branch -M main", check=False)

    remote_authenticated_url = f"https://{token}@github.com/{username}/{REPO_NAME}.git"
    public_repo_url = f"https://github.com/{username}/{REPO_NAME}"

    # Remove existing remote if any
    run_cmd("git remote remove origin", check=False)
    run_cmd(f'git remote add origin "{remote_authenticated_url}"', check=True)

    # Stage files and commit if needed
    run_cmd("git add -A", check=True)
    commit_res = run_cmd('git commit -m "feat: initial commit of 3D Drone Flight Simulator with GLTF assets"', check=False)
    if commit_res.returncode == 0:
        print("[✓] Local commit generated.")
    else:
        print("[i] Working tree already committed.")

    # Push to GitHub
    print(f"[*] Pushing local branch to origin main at {public_repo_url}...")
    push_res = run_cmd("git push -u origin main --force", check=False)

    # Clean up token from remote origin url for local security
    run_cmd(f'git remote set-url origin "{public_repo_url}.git"', check=False)

    if push_res.returncode == 0:
        print("\n" + "=" * 65)
        print(" [✓] DEPLOYMENT SUCCESSFUL!")
        print(f" [✓] Live GitHub Repository: {public_repo_url}")
        print("=" * 65)
    else:
        print(f"\n[!] Push error output:\n{push_res.stderr}", file=sys.stderr)
        print("\n[i] Manual CLI Fallback:")
        print(f"    git remote set-url origin https://github.com/{username}/{REPO_NAME}.git")
        print("    git push -u origin main")

if __name__ == "__main__":
    main()
