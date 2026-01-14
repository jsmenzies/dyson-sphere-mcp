#!/bin/bash
# Check status of servers

API_PORT=8000
WEB_PORT=5173
CHROME_PORT=9222

check_port() {
    local port=$1
    local name=$2
    PID=$(lsof -t -i:$port)
    if [ ! -z "$PID" ]; then
        echo "✅ $name is running on port $port (PID: $PID)"
    else
        echo "❌ $name is NOT running on port $port"
    fi
}

check_port $API_PORT "API Server"
check_port $WEB_PORT "Web Server"
check_port $CHROME_PORT "Google Chrome"
