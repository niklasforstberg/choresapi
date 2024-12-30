FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ChoresApi.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY localhost+1.pem ./
COPY localhost+1-key.pem ./

ENV ASPNETCORE_URLS=https://+:7165;http://+:5165
ENV ASPNETCORE_HTTPS_PORT=7165
EXPOSE 7165
EXPOSE 5165

ENTRYPOINT ["dotnet", "ChoresApi.dll"] 