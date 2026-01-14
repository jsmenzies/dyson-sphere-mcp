#!/bin/bash
# Script to stop all running servers

API_PORT=8000
WEB_PORT=5173
CHROME_PORT=9222

stop_port() {
    local port=$1
    local name=$2
    PID=$(lsof -t -i:$port)
    if [ ! -z "$PID" ]; then
        echo "Stopping $name on port $port (PID: $PID)..."
        kill -9 $PID
    else
        echo "$name on port $port is not running."
    fi
}

stop_port $API_PORT "API Server"
stop_port $WEB_PORT "Web Server"
stop_port $CHROME_PORT "Google Chrome"
