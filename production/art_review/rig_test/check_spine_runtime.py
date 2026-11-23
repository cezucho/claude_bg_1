"""Loads spine/viewer/index.html in headless Chromium through a local server; screenshots each animation."""
import os, sys, threading, http.server, functools, time
from playwright.sync_api import sync_playwright
HERE = os.path.dirname(os.path.abspath(__file__))
H = functools.partial(http.server.SimpleHTTPRequestHandler, directory=os.path.join(HERE, sys.argv[1] if len(sys.argv) > 1 else "spine"))
H.log_message = lambda *a: None
srv = http.server.ThreadingHTTPServer(("127.0.0.1", 8765), H); threading.Thread(target=srv.serve_forever, daemon=True).start()
with sync_playwright() as pw:
    b = pw.chromium.launch(channel="msedge", args=["--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist"]); pg = b.new_page(viewport={"width": 820, "height": 760})
    logs = []; pg.on("console", lambda m: logs.append(m.type + ": " + m.text)); pg.on("pageerror", lambda e: logs.append("pageerror: " + str(e)))
    pg.goto("http://127.0.0.1:8765/viewer/index.html")
    pg.wait_for_function("window.spineReady || window.spineError", timeout=30000)
    print("error:", pg.evaluate("window.spineError")); print("status:", pg.inner_text("#st"))
    for a, t in [(a, 0.3) for a in (sys.argv[2].split(",") if len(sys.argv) > 2 else ["idle", "cast", "hit"])]:
        pg.evaluate(f"player.setAnimation('{a}', true);")
        time.sleep(1.4)
        pg.screenshot(path=os.path.join(HERE, "previews", f"_runtime_{sys.argv[1] if len(sys.argv) > 1 else 'spine'}_{a}.png"))
    print("\n".join(logs[-10:]))
    b.close()
srv.shutdown()
