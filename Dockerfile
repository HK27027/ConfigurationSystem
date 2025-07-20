FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Önce solution ve proje dosyalarını kopyala
COPY ["CaseSecilStore.sln", "."]
COPY ["Library/Library.csproj", "Library/"]
COPY ["CaseSecilStore/CaseSecilStore.csproj", "CaseSecilStore/"]

# Bağımlılıkları restore et
RUN dotnet restore "CaseSecilStore.sln"

# Tüm kaynak kodlarını kopyala
COPY . .

# Projeyi derle
WORKDIR "/src/CaseSecilStore"
RUN dotnet build "CaseSecilStore.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CaseSecilStore.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CaseSecilStore.dll"]