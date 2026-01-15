#!/bin/bash
# Script to run the Python API server

PORT=8000

# Check for mock argument
if [ "$1" == "mock" ]; then
    echo "Enabling mock mode..."
    export DSP_USE_MOCK=true
fi

echo "Checking for existing server on port $PORT..."
PID=$(lsof -t -i:$PORT)

if [ ! -z "$PID" ]; then
    echo "Stopping existing server (PID: $PID)..."
    kill -9 $PID
    sleep 1
fi

echo "Starting API server with uv..."
cd api && uv run server.py
