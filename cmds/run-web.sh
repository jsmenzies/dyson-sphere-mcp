#!/bin/bash
# Script to run the Svelte web frontend

PORT=5173

echo "Checking for existing web server on port $PORT..."
PID=$(lsof -t -i:$PORT)

if [ ! -z "$PID" ]; then
    echo "Stopping existing web server (PID: $PID)..."
    kill -9 $PID
    sleep 1
fi

echo "Starting web server with bun..."
cd web && bun run dev
