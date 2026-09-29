# Docker Commands Reference — CoffeeNChill

| Task | Command |
|---|---|
| Start Azurite | `docker run -d --name coffeenchill-azurite -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite` |
| Build Functions image | `docker build -t st10482062/coffeenchill-functions:v1.0 .` |
| Push Functions image | `docker push st10482062/coffeenchill-functions:v1.0` |
| Run Functions container | `docker run -p 7071:80 -e AzureWebJobsStorage="UseDevelopmentStorage=true" st10482062/coffeenchill-functions:v1.0` |
| Re-tag Azurite image | `docker tag mcr.microsoft.com/azure-storage/azurite st10482062/coffeenchill-azurite:v1.0` |
| Push Azurite image | `docker push st10482062/coffeenchill-azurite:v1.0` |