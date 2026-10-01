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
docker compose config      # check the file for errors
docker compose up          # start Azurite and Functions
docker compose ps          # list the compose containers
docker compose down        # stop and remove containers and network (keeps the volume)
docker compose down -v     # DANGER: also deletes the Azurite data volume
```

### Network and volume checks
```
docker network ls
docker network inspect <network-name>
docker volume ls
```

### Tag and push v2.0 images
```
docker login
docker tag st10482062/coffeenchill-azurite:v1.0 st10482062/coffeenchill-azurite:v2.0
docker push st10482062/coffeenchill-azurite:v2.0
```

Hostname rule: inside the compose network use the service name `azurite`, never `localhost`.

Reference: Docker (2026e; 2026g).