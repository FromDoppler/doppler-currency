# Doppler Currency

This App shows Usd currency by country

## Technology
- Net Core 3.1
- Swagger
- Polly
- AngleSharp
- Docker

## Install
Download https://dotnet.microsoft.com/download/dotnet-core/3.1

## Configs
Add Slack notification "SlackHook" key

## Available Scripts
  In the project directory, you can run:
  
  ### `dotnet run`
  Runs the app in the development mode.
  Open https://localhost:5001/swagger to view it in the browser.
  
  ### `dotnet build`
  Compile the changes.
  
  ### `dotnet test`
  Go to Doppler.Currency.Test folder.
  Run Unit and Integration Tests.
  
  ### `dotnet publish`
  Generate the publish folder
  You can distribute the contents of the publish folder to other platforms as long as they've already installed the dotnet runtime
  
  ## Class Diagram - Handler of the country
  
  ![DopplerCurrency](https://user-images.githubusercontent.com/6796523/75710217-e84c6700-5ca2-11ea-9885-337af625b01f.png)
  
  ## Links
  
 Options pattern https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-3.1
 HttpClientFactory https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-3.1
 Polly policies https://github.com/App-vNext/Polly/wiki/Polly-and-HttpClientFactory
  
