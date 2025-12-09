FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["GAM106_ASM/GAM106_ASM.csproj", "GAM106_ASM/"]
RUN dotnet restore "GAM106_ASM/GAM106_ASM.csproj"
COPY . .
WORKDIR "/src/GAM106_ASM"
RUN dotnet build "GAM106_ASM.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GAM106_ASM.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GAM106_ASM.dll"]
