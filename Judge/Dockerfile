FROM mcr.microsoft.com/dotnet/sdk:3.1-alpine AS build
WORKDIR /build
COPY . .
RUN dotnet publish Judge/Judge.csproj -c Release -o /app

# Build runtime image
FROM docker:dind-rootless

USER root

RUN apk update
# .NET dependencies
RUN apk add bash icu-libs krb5-libs libgcc libintl libssl1.1 libstdc++ zlib
RUN apk add libgdiplus --allow-untrusted --repository https://dl-3.alpinelinux.org/alpine/edge/testing/
# .NET install
RUN mkdir -p /usr/share/dotnet \
    && ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet 
RUN wget https://dot.net/v1/dotnet-install.sh
RUN chmod +x dotnet-install.sh
RUN ./dotnet-install.sh -c 3.1 --runtime dotnet --install-dir /usr/share/dotnet
RUN dotnet --info

COPY --from=build /app /app

ENTRYPOINT dockerd-entrypoint.sh > /dev/null 2>&1 & dotnet /app/Judge.dll