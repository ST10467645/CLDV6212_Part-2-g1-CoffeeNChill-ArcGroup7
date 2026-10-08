# Docker Commands Reference — CoffeeNChill

| Task | Command |
|---|---|
| Start Azurite | `docker run -d --name coffeenchill-azurite -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite` |
| Build Functions image | `docker build -t st10482062/coffeenchill-functions:v1.0 .` |
| Push Functions image | `docker push st10482062/coffeenchill-functions:v1.0` |
| Run Functions container | `docker run -p 7071:80 -e AzureWebJobsStorage="UseDevelopmentStorage=true" st10482062/coffeenchill-functions:v1.0` |
| Re-tag Azurite image | `docker tag mcr.microsoft.com/azure-storage/azurite st10482062/coffeenchill-azurite:v1.0` |
| Push Azurite image | `docker push st10482062/coffeenchill-azurite:v1.0` |

## Part 2: Docker Compose and Docker Hub v2.0

### Compose commands
```
docker compose config                  # check the file for errors
docker compose up                      # start Azurite and Functions (live logs)
docker compose up -d                   # start in the background
docker compose pull                    # download the latest v2.0 images (run after a rebuild)
docker compose ps                      # list the compose containers
docker compose logs functions          # show the Functions logs
docker compose logs functions | findstr /c:"Host.Functions."   # list the loaded functions (Windows)
docker compose down                    # stop and remove containers and network (keeps the volume)
docker compose down -v                 # DANGER: also deletes the Azurite data volume
```

### Network and volume checks
```
docker network ls
docker network inspect NETWORK_NAME
docker volume ls
```
Replace NETWORK_NAME with the real name from `docker network ls`. Do not type angle brackets.

### Tag and push the Azurite v2.0 image
```
docker login
docker tag st10482062/coffeenchill-azurite:v1.0 st10482062/coffeenchill-azurite:v2.0
docker push st10482062/coffeenchill-azurite:v2.0
```

### Build and push the Functions v2.0 image
```
# run from the repo root, where the Dockerfile is
docker build --no-cache -t st10482062/coffeenchill-functions:v2.0 .
docker push st10482062/coffeenchill-functions:v2.0
```

### Remove and re-pull v2.0 images (shows Compose pulling from Docker Hub)
```
docker compose down
docker rmi st10482062/coffeenchill-functions:v2.0
docker rmi st10482062/coffeenchill-azurite:v2.0
docker compose up
```

### Poison queue demo
Set `ENABLE_POISON_TEST` to `"true"` in `docker-compose.yml`, then run `docker compose up -d`.
Failed messages move to `order-processing-queue-poison`.
Set it back to `"false"` before committing.

Hostname rule: inside the compose network use the service name `azurite`, never `localhost`.

Reference: Docker (2026e; 2026f; 2026g).