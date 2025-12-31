# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
LABEL stage=build-env
WORKDIR /app

# Copy 
COPY Directory.Packages.props /app/
COPY ./src/ /app/

RUN echo '#!/bin/sh\nfind /app -type d \( -name "bin" -o -name "obj" \) -exec rm -rf {} + 2>/dev/null || true' > /clean.sh && chmod +x /clean.sh

ARG GIT_COMMIT
ARG GIT_BRANCH

# Build modules
RUN for module in /app/Modules/*; do \
  dotnet build "$module" -c Release -p:SourceRevisionId=$GIT_COMMIT -p:GitBranch=$GIT_BRANCH; \
  done

# Build plugins
RUN for plugin in /app/Plugins/*; do \
  dotnet build "$plugin" -c Release -p:SourceRevisionId=$GIT_COMMIT -p:GitBranch=$GIT_BRANCH; \
  done

# Publish Web project
RUN dotnet publish /app/Web/Grand.Web/Grand.Web.csproj -c Release -o ./build/release -p:SourceRevisionId=$GIT_COMMIT -p:GitBranch=$GIT_BRANCH

# Copy module DLLs to published output in correct folder structure
RUN for module in /app/Modules/*; do \
      module_name=$(basename "$module"); \
      if [ -d "$module/bin/Release" ]; then \
        mkdir -p "./build/release/Modules/$module_name"; \
        cp -r "$module/bin/Release"/*/* "./build/release/Modules/$module_name/" 2>/dev/null || true; \
      fi; \
    done

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

EXPOSE 8080
WORKDIR /app
COPY --from=build-env /app/build/release .
ENTRYPOINT ["dotnet", "Grand.Web.dll"]
