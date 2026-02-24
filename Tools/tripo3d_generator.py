#!/usr/bin/env python3
"""
Tripo3D Character Generator — Bulk image-to-3D conversion
Usage: python3 tripo3d_generator.py <image_url_or_path> <output_dir>
"""

import sys
import time
import requests
import json
import os
from pathlib import Path

TRIPO_API_KEY = "tsk_potWLVr_XhKHDY-vsAZ-Z-hi-NbCBSOaZlgmPgWa1tw"
TRIPO_API_BASE = "https://api.tripo3d.ai/v2/openapi"

HEADERS = {
    "Authorization": f"Bearer {TRIPO_API_KEY}",
    "Content-Type": "application/json"
}

def submit_image_to_model(image_path: str) -> str:
    """Upload image and submit image_to_model task"""
    print(f"[*] Uploading image: {image_path}")
    
    # Upload image
    with open(image_path, 'rb') as f:
        files = {'file': f}
        upload_resp = requests.post(
            f"{TRIPO_API_BASE}/upload",
            headers={"Authorization": f"Bearer {TRIPO_API_KEY}"},
            files=files,
            timeout=30
        )
    
    upload_resp.raise_for_status()
    upload_data = upload_resp.json()
    
    if upload_data.get("code") != 0:
        raise Exception(f"Upload failed: {upload_data}")
    
    image_key = upload_data['data']['image_key']
    print(f"[✓] Image uploaded: {image_key}")
    
    # Submit task
    print("[*] Submitting image_to_model task...")
    task_resp = requests.post(
        f"{TRIPO_API_BASE}/task",
        headers=HEADERS,
        json={
            "type": "image_to_model",
            "image_key": image_key
        },
        timeout=30
    )
    
    task_resp.raise_for_status()
    task_data = task_resp.json()
    
    if task_data.get("code") != 0:
        raise Exception(f"Task submission failed: {task_data}")
    
    task_id = task_data['data']['task_id']
    print(f"[✓] Task submitted: {task_id}")
    return task_id

def poll_task(task_id: str, max_wait: int = 600) -> dict:
    """Poll task until completion"""
    print(f"[*] Polling task {task_id}...")
    start = time.time()
    
    while time.time() - start < max_wait:
        resp = requests.post(
            f"{TRIPO_API_BASE}/task",
            headers=HEADERS,
            json={"task_id": task_id},
            timeout=30
        )
        
        resp.raise_for_status()
        data = resp.json()
        
        if data.get("code") != 0:
            raise Exception(f"Poll failed: {data}")
        
        status = data['data'].get('status')
        print(f"  Status: {status}")
        
        if status == "success":
            print(f"[✓] Task completed!")
            return data['data']
        elif status == "failed":
            raise Exception(f"Task failed: {data['data']}")
        
        time.sleep(5)
    
    raise Exception(f"Task timeout after {max_wait}s")

def download_models(task_data: dict, output_dir: str):
    """Download GLB and GLTF from task"""
    output_dir = Path(output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)
    
    models = task_data.get('model', {})
    
    for model_type in ['glb', 'gltf']:
        url = models.get(model_type)
        if not url:
            print(f"  No {model_type.upper()} available")
            continue
        
        print(f"[*] Downloading {model_type.upper()}...")
        resp = requests.get(url, timeout=60)
        resp.raise_for_status()
        
        filepath = output_dir / f"character.{model_type if model_type == 'glb' else 'gltf'}"
        with open(filepath, 'wb') as f:
            f.write(resp.content)
        
        print(f"[✓] Saved: {filepath} ({len(resp.content) / 1024 / 1024:.1f} MB)")

def main():
    if len(sys.argv) < 2:
        print("Usage: python3 tripo3d_generator.py <image_path> [output_dir]")
        print("Example: python3 tripo3d_generator.py agent_zero.jpg ./output")
        sys.exit(1)
    
    image_path = sys.argv[1]
    output_dir = sys.argv[2] if len(sys.argv) > 2 else "./tripo3d_output"
    
    if not os.path.exists(image_path):
        print(f"[!] Image not found: {image_path}")
        sys.exit(1)
    
    try:
        # Submit
        task_id = submit_image_to_model(image_path)
        
        # Poll
        task_data = poll_task(task_id)
        
        # Download
        download_models(task_data, output_dir)
        
        print(f"\n[✓] All done! Models saved to: {output_dir}")
        
    except Exception as e:
        print(f"[!] Error: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()
