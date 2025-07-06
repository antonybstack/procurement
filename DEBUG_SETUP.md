# Debug Setup Guide for Procurement API

This guide explains how to set up debugging for the .NET API running in Docker containers using VS Code.

## Prerequisites

- VS Code with C# extension installed
- Docker and Docker Compose installed
- .NET 9.0 SDK installed locally (for building)

## Quick Start

### 1. Start the Regular Environment

```bash
# Start the regular environment
docker-compose up -d

# Check that services are running
docker-compose ps
```

### 2. Open VS Code and Set Breakpoints

1. Open the project in VS Code
2. Navigate to any controller file (e.g., `ProcurementAPI/Controllers/QuotesController.cs`)
3. Set breakpoints by clicking in the left margin next to line numbers
4. The breakpoints will appear as red dots

### 3. Start Debugging

1. Open the Debug panel in VS Code (Ctrl+Shift+D or Cmd+Shift+D)
2. Select "Debug API in Docker Container" from the dropdown
3. Click the green play button or press F5
4. VS Code will prompt you to select a process - choose the dotnet process

### 4. Test the API

1. Open your browser to http://localhost:5001/swagger
2. Try any API endpoint
3. When the code hits your breakpoint, execution will pause
4. You can inspect variables, step through code, etc.

## Debug Configurations

The `.vscode/launch.json` file contains two debug configurations:

### 1. Debug API in Docker Container
- **Purpose**: Attach to a running API process in the container
- **Use Case**: When the API is already running and you want to debug
- **How to Use**: 
  1. Start the regular environment
  2. Select this configuration
  3. Choose the dotnet process when prompted

### 2. Attach to Running Container
- **Purpose**: Alternative way to attach to a running container
- **Use Case**: When the first attach method doesn't work
- **How to Use**: Same as the first configuration

## Development Workflow

### Starting Development

```bash
# Start the regular environment
docker-compose up -d

# Watch logs
docker-compose logs -f procurement-api
```

### Making Code Changes

1. Edit your C# code in VS Code
2. Rebuild the container to apply changes:
   ```bash
   docker-compose build procurement-api
   docker-compose up -d
   ```
3. Set breakpoints and debug as needed

### Stopping Development

```bash
# Stop the environment
docker-compose down
```

## Troubleshooting

### Container Name Issues

If you get an error about the container name, check the actual container name:

```bash
docker ps
```

The container should be named `procurement_api`. If it's different, update the container name in `.vscode/launch.json`:

```json
"pipeArgs": [
    "exec",
    "-i",
    "procurement_api"
]
```

### Process Not Found

If VS Code can't find the dotnet process:

1. Make sure the API container is running:
   ```bash
   docker-compose ps
   ```

2. Check if the API is actually running:
   ```bash
   docker-compose logs procurement-api
   ```

3. Try restarting the container:
   ```bash
   docker-compose restart procurement-api
   ```

### Breakpoints Not Hitting

1. Make sure the API container is running and stable
2. Verify you're hitting the correct endpoint
3. Check that the container isn't restarting

### Source File Mapping Issues

If breakpoints show as "unbound":

1. Check the `sourceFileMap` in `.vscode/launch.json`
2. Verify the paths match your actual file structure
3. Try rebuilding the container:
   ```bash
   docker-compose build procurement-api
   docker-compose up -d
   ```

## Useful Commands

```bash
# Start environment
docker-compose up -d

# Stop environment
docker-compose down

# View logs
docker-compose logs -f procurement-api

# Rebuild API container
docker-compose build procurement-api

# Restart API container
docker-compose restart procurement-api

# Check container status
docker-compose ps

# Access container shell
docker exec -it procurement_api /bin/bash
```

## Next Steps

1. Set up your first breakpoint in a controller
2. Test the debugging workflow
3. Explore the VS Code debugging features
4. Customize the configuration for your needs

Happy debugging! 🐛 