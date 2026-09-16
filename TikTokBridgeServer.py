"""
TikTok Live Bridge Server
Provides local HTTP API for Unity to check if streamer is live and stream chat comments (e.g. typing 'a').
Runs on http://127.0.0.1:8765
"""

import sys
import json
import asyncio
import threading
from http.server import HTTPServer, BaseHTTPRequestHandler
from urllib.parse import urlparse, parse_qs

try:
    from TikTokLive import TikTokLiveClient
    from TikTokLive.events import ConnectEvent, CommentEvent, GiftEvent
    TIKTOKLIVE_AVAILABLE = True
except ImportError:
    TIKTOKLIVE_AVAILABLE = False

current_client = None
current_task = None
event_queue = []
event_lock = threading.Lock()
loop = None

def get_event_loop():
    global loop
    if loop is None:
        loop = asyncio.new_event_loop()
        t = threading.Thread(target=loop.run_forever, daemon=True)
        t.start()
    return loop

async def _check_is_live(username):
    if not TIKTOKLIVE_AVAILABLE:
        return False, "TikTokLive library not available"
    try:
        clean_user = username.replace("@", "").strip()
        client = TikTokLiveClient(unique_id=clean_user)
        is_live = await client.is_live()
        return is_live, ""
    except Exception as e:
        return False, str(e)

def _get_avatar_url(user):
    if not user:
        return ""
    for attr in ['avatar', 'avatar_thumb', 'avatar_medium', 'avatar_large', 'profile_picture']:
        obj = getattr(user, attr, None)
        if obj:
            if hasattr(obj, 'urls') and obj.urls:
                return obj.urls[0]
            if isinstance(obj, str) and obj.startswith('http'):
                return obj
            if isinstance(obj, list) and len(obj) > 0 and isinstance(obj[0], str):
                return obj[0]
    return ""

async def _run_tiktok_client(username):
    global current_client
    clean_user = username.replace("@", "").strip()
    current_client = TikTokLiveClient(unique_id=clean_user)

    @current_client.on(ConnectEvent)
    async def on_connect(event):
        print(f"[Bridge] Connected to TikTok Live: @{clean_user}")

    @current_client.on(CommentEvent)
    async def on_comment(event):
        msg = event.comment.strip()
        user = event.user.unique_id
        nick = event.user.nickname or user
        avatar_url = _get_avatar_url(event.user)
        print(f"[Bridge] Chat: @{user}: {msg} (avatar: {bool(avatar_url)})")
        if msg.lower() == "a":
            with event_lock:
                event_queue.append({
                    "type": "chat_a",
                    "username": user,
                    "nickname": nick,
                    "message": msg,
                    "avatar_url": avatar_url
                })

    @current_client.on(GiftEvent)
    async def on_gift(event):
        user = event.user.unique_id
        gift = event.gift.name
        avatar_url = _get_avatar_url(event.user)
        print(f"[Bridge] Gift: @{user} sent {gift}")
        with event_lock:
            event_queue.append({
                "type": "gift",
                "username": user,
                "gift_name": gift,
                "avatar_url": avatar_url
            })

    try:
        await current_client.start()
    except Exception as e:
        print(f"[Bridge] Client stopped or error: {e}")

class BridgeRequestHandler(BaseHTTPRequestHandler):
    def _send_cors(self):
        self.send_header('Access-Control-Allow-Origin', '*')
        self.send_header('Access-Control-Allow-Methods', 'GET, POST, OPTIONS')
        self.send_header('Access-Control-Allow-Headers', 'Content-Type')

    def do_OPTIONS(self):
        self.send_response(200)
        self._send_cors()
        self.end_headers()

    def do_GET(self):
        parsed = urlparse(self.path)
        path = parsed.path
        qs = parse_qs(parsed.query)

        if path == "/health":
            self.send_response(200)
            self.send_header('Content-Type', 'application/json')
            self._send_cors()
            self.end_headers()
            self.wfile.write(json.dumps({"status": "ok", "tiktok_lib": TIKTOKLIVE_AVAILABLE}).encode('utf-8'))
            return

        if path in ("/gifts", "/api/gifts"):
            self.send_response(200)
            self.send_header('Content-Type', 'application/json')
            self._send_cors()
            self.end_headers()
            gifts_data = []

            # 1. Try loading from StreamingAssets cache (710 gifts from TikTok API)
            cache_paths = [
                os.path.join(os.path.dirname(__file__), "Assets", "StreamingAssets", "tiktok_gifts_cache.json"),
                os.path.join(os.path.dirname(__file__), "tiktok_gifts_cache.json")
            ]
            for cp in cache_paths:
                if os.path.exists(cp):
                    try:
                        with open(cp, "r", encoding="utf-8") as f:
                            gifts_data = json.load(f)
                            break
                    except Exception:
                        pass

            # 2. Live client gift_info fallback
            if not gifts_data and current_client and hasattr(current_client, 'gift_info') and current_client.gift_info:
                try:
                    for g in current_client.gift_info.get('gifts', []):
                        gifts_data.append({
                            "id": str(g.get("id", "")),
                            "name": g.get("name", ""),
                            "diamonds": g.get("diamond_count", 1),
                            "icon_url": g.get("image", {}).get("url_list", [""])[0] if isinstance(g.get("image"), dict) else ""
                        })
                except Exception:
                    pass

            # 3. Built-in Top 20 Popular TikTok Gifts with real CDN URLs
            if not gifts_data:
                gifts_data = [
                    {"id": "5655", "name": "Rose", "diamonds": 1, "icon_url": "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/eba3a9bb85c33e017f3648eaf88d7189~tplv-obj.png"},
                    {"id": "5269", "name": "TikTok", "diamonds": 1, "icon_url": "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/802a21ae29f9fae5abe3693de9f874bd~tplv-obj.png"},
                    {"id": "6247", "name": "Heart", "diamonds": 1, "icon_url": "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/dd300fd35a757d751301fba862a258f1~tplv-obj.png"},
                    {"id": "5487", "name": "Finger Heart", "diamonds": 5, "icon_url": "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/a4c4dc437fd3a6632aba149769491f49.png~tplv-obj.png"},
                    {"id": "19314", "name": "Panda", "diamonds": 5, "icon_url": "https://p16-webcast.tiktokcdn.com/img/alisg/webcast-sg/resource/a96c32f3272df1905eb3f3d51b53308b.png~tplv-obj.png"},
                    {"id": "15199", "name": "Ice Cream", "diamonds": 1, "icon_url": "https://p16-webcast.tiktokcdn.com/img/alisg/webcast-sg/resource/7f784d1ec7b26d7d8cfd05faede11d76.png~tplv-obj.png"},
                    {"id": "5879", "name": "Doughnut", "diamonds": 30, "icon_url": "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/4e7ad6bdf0a1d860c538f38026d4e812~tplv-obj.png"},
                    {"id": "6104", "name": "Cap", "diamonds": 99, "icon_url": "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/6c2ab2da19249ea570a2ece5e3377f04~tplv-obj.png"},
                    {"id": "5509", "name": "Sunglasses", "diamonds": 199, "icon_url": "https://p16-webcast.tiktokcdn.com/img/alisg/webcast-sg/08af67ab13a8053269bf539fd27f3873.png~tplv-obj.png"},
                    {"id": "59450", "name": "Boxing Gloves", "diamonds": 299, "icon_url": "https://p16-webcast.tiktokcdn.com/img/alisg/webcast-sg/resource/3b77922c8b290b899ae70b6c0e84e437.png~tplv-obj.png"},
                    {"id": "6267", "name": "Corgi", "diamonds": 299, "icon_url": "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/148eef0884fdb12058d1c6897d1e02b9~tplv-obj.png"},
                    {"id": "5566", "name": "Mishka Bear", "diamonds": 100, "icon_url": "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/d78ed6496fd57286b42ac033acbee299.png~tplv-obj.png"},
                    {"id": "7168", "name": "Money Gun", "diamonds": 500, "icon_url": "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/e0589e95a2b41970f0f30f6202f5fce6~tplv-obj.png"},
                    {"id": "5897", "name": "Swan", "diamonds": 699, "icon_url": "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/97a26919dbf6afe262c97e22a83f4bf1~tplv-obj.png"},
                    {"id": "6064", "name": "GG", "diamonds": 1, "icon_url": "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/3f02fa9594bd1495ff4e8aa5ae265eef~tplv-obj.png"},
                    {"id": "6090", "name": "Fireworks", "diamonds": 1088, "icon_url": "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/9494c8a0bc5c03521ef65368e59cc2b8~tplv-obj.png"},
                    {"id": "6437", "name": "Garland", "diamonds": 199, "icon_url": "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/bdbdd8aeb2b69c173a3ef666e63310f3~tplv-obj.png"},
                    {"id": "6820", "name": "Whale", "diamonds": 2150, "icon_url": "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/46fa70966d8e931497f5289060f9a794~tplv-obj.png"},
                    {"id": "5765", "name": "Motorcycle", "diamonds": 2988, "icon_url": "https://p16-webcast.tiktokcdn.com/img/alisg/webcast-sg/motor_icon_green.png~tplv-obj.png"},
                    {"id": "13061", "name": "Sports Car", "diamonds": 4999, "icon_url": "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/resource/201304005bf8f86b533b97ffa88f1abe.png~tplv-obj.png"}
                ]

            self.wfile.write(json.dumps({"gifts": gifts_data}).encode('utf-8'))
            return

        if path == "/check_live":
            username = qs.get("username", [""])[0]
            if not username:
                self.send_response(400)
                self.send_header('Content-Type', 'application/json')
                self._send_cors()
                self.end_headers()
                self.wfile.write(json.dumps({"success": False, "message": "Missing username"}).encode('utf-8'))
                return

            lp = get_event_loop()
            future = asyncio.run_coroutine_threadsafe(_check_is_live(username), lp)
            try:
                is_live, err = future.result(timeout=10)
                self.send_response(200)
                self.send_header('Content-Type', 'application/json')
                self._send_cors()
                self.end_headers()
                self.wfile.write(json.dumps({
                    "success": True,
                    "username": username,
                    "is_live": is_live,
                    "error": err
                }).encode('utf-8'))
            except Exception as e:
                self.send_response(500)
                self.send_header('Content-Type', 'application/json')
                self._send_cors()
                self.end_headers()
                self.wfile.write(json.dumps({"success": False, "is_live": False, "error": str(e)}).encode('utf-8'))
            return

        if path == "/events":
            with event_lock:
                events = list(event_queue)
                event_queue.clear()
            self.send_response(200)
            self.send_header('Content-Type', 'application/json')
            self._send_cors()
            self.end_headers()
            self.wfile.write(json.dumps({"success": True, "events": events}).encode('utf-8'))
            return

        self.send_response(404)
        self.end_headers()

    def do_POST(self):
        parsed = urlparse(self.path)
        path = parsed.path
        qs = parse_qs(parsed.query)

        if path == "/start":
            username = qs.get("username", [""])[0]
            if not username:
                self.send_response(400)
                self.send_header('Content-Type', 'application/json')
                self._send_cors()
                self.end_headers()
                self.wfile.write(json.dumps({"success": False, "message": "Missing username"}).encode('utf-8'))
                return

            global current_task, current_client
            lp = get_event_loop()
            if current_client:
                try:
                    current_client.stop()
                except:
                    pass

            with event_lock:
                event_queue.clear()

            current_task = asyncio.run_coroutine_threadsafe(_run_tiktok_client(username), lp)

            self.send_response(200)
            self.send_header('Content-Type', 'application/json')
            self._send_cors()
            self.end_headers()
            self.wfile.write(json.dumps({"success": True, "message": f"Started listening for @{username}"}).encode('utf-8'))
            return

        if path == "/stop":
            if current_client:
                try:
                    current_client.stop()
                except:
                    pass
            self.send_response(200)
            self.send_header('Content-Type', 'application/json')
            self._send_cors()
            self.end_headers()
            self.wfile.write(json.dumps({"success": True, "message": "Stopped"}).encode('utf-8'))
            return

        self.send_response(404)
        self.end_headers()

    def log_message(self, format, *args):
        pass

def run_server(port=8765):
    server = HTTPServer(('127.0.0.1', port), BridgeRequestHandler)
    print(f"[TikTokBridge] Server running on http://127.0.0.1:{port}")
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()

if __name__ == "__main__":
    port = 8765
    if len(sys.argv) > 1:
        port = int(sys.argv[1])
    run_server(port)
