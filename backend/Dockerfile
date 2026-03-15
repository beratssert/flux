FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CleanArchitecture.sln", "./"]
COPY ["CleanArchitecture/CleanArchitecture.Application/CleanArchitecture.Core.csproj", "CleanArchitecture/CleanArchitecture.Application/"]
COPY ["CleanArchitecture/CleanArchitecture.Infrastructure/CleanArchitecture.Infrastructure.csproj", "CleanArchitecture/CleanArchitecture.Infrastructure/"]
COPY ["CleanArchitecture/CleanArchitecture.WebApi/CleanArchitecture.WebApi.csproj", "CleanArchitecture/CleanArchitecture.WebApi/"]
COPY ["Tests/CleanArchitecture.Infrastructure.Tests/CleanArchitecture.Infrastructure.Tests.csproj", "Tests/CleanArchitecture.Infrastructure.Tests/"]
RUN dotnet restore "CleanArchitecture.sln"

COPY . .
WORKDIR "/src/CleanArchitecture/CleanArchitecture.WebApi"
RUN dotnet build "CleanArchitecture.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CleanArchitecture.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CleanArchitecture.WebApi.dll"]