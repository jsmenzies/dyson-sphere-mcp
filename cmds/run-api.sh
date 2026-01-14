#!/bin/bash
# Script to run the Python API server

PORT=8000

echo "Checking for existing server on port $PORT..."
PID=$(lsof -t -i:$PORT)

if [ ! -z "$PID" ]; then
    echo "Stopping existing server (PID: $PID)..."
    kill -9 $PID
    sleep 1
fi

echo "Starting API server with uv..."
cd api && uv run server.py
