#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
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
ENTRYPOINT ["dotnet", "Doppler.Currency.dll"]