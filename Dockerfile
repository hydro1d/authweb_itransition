FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["AuthWeb.csproj", "./"]
RUN dotnet restore "AuthWeb.csproj"
COPY . .
RUN dotnet publish "AuthWeb.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENV DOTNET_hostBuilder__reloadConfigOnChange=false
ENTRYPOINT ["dotnet", "AuthWeb.dll"]
