FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS restore
WORKDIR /src
COPY Doppler.Currency.sln ./
COPY Doppler.Currency/Doppler.Currency.csproj ./Doppler.Currency/Doppler.Currency.csproj
COPY CrossCutting/CrossCutting.csproj ./CrossCutting/CrossCutting.csproj
COPY Doppler.Currency.Test/Doppler.Currency.Test.csproj ./Doppler.Currency.Test/Doppler.Currency.Test.csproj
RUN dotnet restore

FROM restore AS build
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS test
RUN dotnet test

FROM build AS publish
RUN dotnet publish "Doppler.Currency/Doppler.Currency.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS final
WORKDIR /app
EXPOSE 80
COPY --from=publish /app/publish .
ARG version=unknown
# TODO: configure static files in the service and copy version.txt to the right folder
RUN echo $version > /app/version.txt
ENTRYPOINT ["dotnet", "Doppler.Currency.dll"]
