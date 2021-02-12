#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["Doppler.Currency/Doppler.Currency.csproj", "Doppler.Currency/"]
COPY ["CrossCutting/CrossCutting.csproj", "CrossCutting/"]
RUN dotnet restore "Doppler.Currency/Doppler.Currency.csproj"
COPY . .
WORKDIR "/src/Doppler.Currency"
RUN dotnet build "Doppler.Currency.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Doppler.Currency.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ARG version=unknown
RUN echo $version > /app/wwwroot/version.txt
ENTRYPOINT ["dotnet", "Doppler.Currency.dll"]