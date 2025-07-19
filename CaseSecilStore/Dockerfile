FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# DÜZELTME: Alt dizin kullanımını kaldır
COPY ["CaseSecilStore.csproj", "."]
RUN dotnet restore "CaseSecilStore.csproj"
COPY . .
RUN dotnet build "CaseSecilStore.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CaseSecilStore.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CaseSecilStore.dll"]