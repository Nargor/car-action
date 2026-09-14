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
        print(f"[Bridge] Chat: @{user}: {msg}")
        if msg.lower() == "a":
            with event_lock:
                event_queue.append({
                    "type": "chat_a",
                    "username": user,
                    "nickname": nick,
                    "message": msg
                })

    @current_client.on(GiftEvent)
    async def on_gift(event):
        user = event.user.unique_id
        gift = event.gift.name
        print(f"[Bridge] Gift: @{user} sent {gift}")
        with event_lock:
            event_queue.append({
                "type": "gift",
                "username": user,
                "gift_name": gift
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
