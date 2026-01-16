#!/bin/bash
# Script to run the Svelte web frontend

PORT=5173

echo "Checking for existing web server on port $PORT..."
PIDS=$(lsof -nP -i tcp:$PORT -sTCP:LISTEN 2>/dev/null | \
       grep -E 'bun|node' | \
       awk '{print $2}' | \
       sort -u)

if [ ! -z "$PIDS" ]; then
    echo "Stopping existing web server (PIDs: $PIDS)..."
    kill -9 $PIDS
    sleep 1
fi

echo "Starting web server with bun..."
cd web && bun run dev
#  >> /Users/james/git/dyson-sphere-mcp/web/tmp/mcp-web.log 2>&1 &
# echo "Web server started on port $PORT. Logs are being written to /Users/james/git/dyson-sphere-mcp/web/tmp/mcp-web.log"
