#!/usr/bin/python3
# -*- coding: utf-8 -*-
import asyncio
import websockets
import json
import sys
from datetime import datetime

class Quote:
    def __init__(self, ticker, bid, ask, utcdt):
        self.ticker = ticker
        self.bid = float(bid)
        self.ask = float(ask)
        self.utcdt = datetime.strptime(utcdt, "%d-%m-%Y %H:%M:%S.%f").strftime("%d.%m.%Y %H:%M:%S")

    # печатаем объект котировок
    def __repr__(self):
        return f"{self.ticker}, {self.bid}, {self.ask}, {self.utcdt}"
#        return f"ticker={self.ticker}, bid={self.bid}, ask={self.ask}, utcdt={self.utcdt})"

class WebSocketClient:
    def __init__(self, url, token, reconnect_interval=5, ping_interval=10, ping_timeout=5):
        self.url = url
        self.token = token
        # интервал между попытками соединения
        self.reconnect_interval = reconnect_interval
        # интервал ping
        self.ping_interval = ping_interval
        # ожидание таймаута
        self.ping_timeout = ping_timeout
        self.websocket = None
        self.running = True
        self.sid = None  # Идентификатор сессии
        self.ping_id = 0  # Счетчик ping
        self.events = {}
        self.ping_task = None

    def on(self, event_name, callback):
        """ Подписка на события. """
        if event_name not in self.events:
            self.events[event_name] = []
        self.events[event_name].append(callback)

    def emit(self, event_name, *args, **kwargs):
        """ Вызов событий. """
        dprint(f"Emitting event: {event_name} with args: {args}")  # Debug print
        for callback in self.events.get(event_name, []):
            callback(*args, **kwargs)

    async def connect(self):
        while self.running:
            try:
                async with websockets.connect(self.url) as ws:
                    self.websocket = ws
                    self.emit("connected")
                    await self.authenticate()
                    self.ping_task = asyncio.create_task(self.ping_loop())
                    await self.listen()

            except Exception as e:
                self.emit("error", f"Connection error: {e}")
                dprint(f"Connection error: {e}, reconnecting in {self.reconnect_interval}s")
                self.sid = None  # Сбрасываем SID при разрыве соединения
                await asyncio.sleep(self.reconnect_interval)
                self.emit("reconnecting")

    async def authenticate(self):
        auth_payload = json.dumps({"msg": "open", "sid": self.token})
        await self.websocket.send(auth_payload)
        response = await self.websocket.recv()
        messages = json.loads(response)

        for data in messages:

            if data.get("msg") == "init" and "sid" in data:
                self.sid = data["sid"]
                dprint(f"Emitting authenticated event with SID: {self.sid}")  # Debug print
                self.emit("authenticated", self.sid)
                return

        self.emit("error", "Authentication failed, reconnecting...")
        dprint("Authentication failed, reconnecting...")
        self.sid = None

    async def listen(self):
        while self.running:
            try:
                message = await self.websocket.recv()
                messages = json.loads(message)

                for data in messages:
                    dprint(f"Received JSON: {data}")
                    if data.get("msg") == "ping" and "pid" in data:
                        self.emit("pong", data["pid"])
                        dprint(f"Received pong with PID: {data['pid']}")
                        continue

                    if data.get("msg") == "finish":
                        self.emit("closed")
                        dprint("Received finish command. Closing connection.")
                        self.running = False
                        await self.websocket.close()
                        return

                    if data.get("msg") == "quote":
                        quote = Quote(
                            ticker=data["ticker"],
                            bid=data["bid"],
                            ask=data["ask"],
                            utcdt=data["utcdt"]
                        )
                        self.emit("onquote", quote)
                        dprint(f"Received quote: {quote}")
                        continue

                    self.emit("message", data)
                    dprint(f"Received: {data}")

            except websockets.exceptions.ConnectionClosed:
                self.emit("disconnected")
                dprint("Connection closed, reconnecting...")
                self.sid = None  # Сбрасываем SID при разрыве соединения
                break

    # циклические пинги
    async def ping_loop(self):

        while self.running and self.websocket:
            if self.sid:
                self.ping_id += 1
                ping_payload = json.dumps({"msg": "ping", "sid": self.sid, "pid": f"{self.ping_id}"})
                dprint(f"Sent JSON: {ping_payload}")
                try:
                    await self.websocket.send(ping_payload)
                    print(f"Sent ping: {self.ping_id}")
                except:
                    break
            await asyncio.sleep(self.ping_interval)

    # подписка на тикеры
    async def subscribe(self, tickers):

        if self.sid:
            update_payload = json.dumps({"msg": "update", "sid": self.sid, "tickers": tickers})
            await self.websocket.send(update_payload)
            dprint(f"Subscribed to tickers: {tickers}")
        else:
            dprint("Cannot subscribe: No session ID available")

    def run(self):
        asyncio.run(self.connect())

# если в командной строке указали DEBUG
def dprint(ddata):
    if ( (len(sys.argv) > 3) and (sys.argv[3] == "DEBUG") ):
        print(f"{ddata}")

def main():
    print("Quotes WebSocket client test")

    # проверим только, что есть обязательные параметры - URL и ключ
    # но про DEBUG не забывайте
    if len(sys.argv) < 3:
        print(f"Usage: {sys.argv[0]} URL KEY {{DEBUG}}")
        exit()

    print(f"Starting with WebSocket: {sys.argv[1]}, auth key: {sys.argv[2]}")
    print(f"Quotes output format: ticker, bid, ask, utcdt")

    client = WebSocketClient(sys.argv[1], sys.argv[2])

    # показываем результаты событий
    client.on("onquote", lambda quote: print(f"Quote received: {quote}"))
    client.on("error", lambda err: print(f"Error: {err}"))
    client.on("pong", lambda pid: print(f"Pong received for PID: {pid}"))

    def on_authenticated(auth):
        dprint(f"Authenticated event received with SID: {auth}")
        asyncio.create_task(client.subscribe(["XAURUB", "EURUSD", "USDJPY", "gold"]))

    client.on("authenticated", on_authenticated)
    client.run()

if __name__ == "__main__":
    main()
