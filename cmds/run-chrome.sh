#!/bin/bash
# Script to run Google Chrome with remote debugging enabled for MCP usage

PORT=9222
USER_DATA_DIR="/tmp/chrome-profile-stable"

echo "Checking for existing Chrome instance on port $PORT..."
# Chrome often has multiple processes, so we check for anything listening on the port
PID=$(lsof -t -i:$PORT)

if [ ! -z "$PID" ]; then
    echo "Stopping existing Chrome debugger (PID: $PID)..."
    # Killing Chrome processes can be tricky, but -9 on the listener usually works for reset
    kill -9 $PID
    sleep 1
fi

echo "Starting Google Chrome with remote debugging on port $PORT..."
echo "User data directory: $USER_DATA_DIR"

# Run in background to allow script to exit if needed, or user can run this script with &
# /usr/bin/google-chrome --remote-debugging-port=$PORT --user-data-dir=$USER_DATA_DIR --no-first-run --no-default-browser-check
/Applications/Google\ Chrome.app/Contents/MacOS/Google\ Chrome --remote-debugging-port=$PORT --user-data-dir=$USER_DATA_DIR --no-first-run --no-default-browser-check
