docker buildx build -f LDTTeam.Authentication.Server/Dockerfile --platform linux/amd64,linux/arm64 -t asherslab/ldtteam-donator-auth:1.0 --push .
