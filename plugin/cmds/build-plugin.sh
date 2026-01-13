#!/bin/bash
# Build the DSPMCP plugin and verify deployment

set -e

PLUGIN_DIR="/mnt/c/Users/James/git/dyson-sphere-mcp/plugin/src/DSPMCP"
DEPLOYED_DLL="/mnt/c/Users/James/AppData/Roaming/r2modmanPlus-local/DysonSphereProgram/profiles/Default/BepInEx/plugins/DSPMCP/DSPMCP.dll"

# Generate version from current timestamp (e.g., 1.12.1912)
# Format: 1.Day.HourMinute (valid for .NET)
VERSION=$(date +"1.%-d.%-H%M")

echo "Building plugin version $VERSION..."
cd "$PLUGIN_DIR"
dotnet.exe build /p:Version="$VERSION"

echo ""
echo "Verifying deployment..."
if [ -f "$DEPLOYED_DLL" ]; then
    echo "Plugin deployed successfully:"
    ls -la "$DEPLOYED_DLL"
else
    echo "ERROR: Plugin DLL not found at expected location"
    exit 1
fi
