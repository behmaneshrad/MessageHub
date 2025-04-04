FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Services/EmailReceiver/EmailReceiver.API/EmailReceiver.API.csproj", "Services/EmailReceiver/EmailReceiver.API/"]
COPY ["Services/EmailReceiver/EmailReceiver.Domain/EmailReceiver.Domain.csproj", "Services/EmailReceiver/EmailReceiver.Domain/"]
COPY ["Services/EmailReceiver/EmailReceiver.Application/EmailReceiver.Application.csproj", "Services/EmailReceiver/EmailReceiver.Application/"]
COPY ["Services/EmailReceiver/EmailReceiver.Infrastructure/EmailReceiver.Infrastructure.csproj", "Services/EmailReceiver/EmailReceiver.Infrastructure/"]
COPY ["BuildingBlocks/MessageBroker/MessageBroker.Common/MessageBroker.Common.csproj", "BuildingBlocks/MessageBroker/MessageBroker.Common/"]
COPY ["BuildingBlocks/MessageBroker/MessageBroker.RabbitMQ/MessageBroker.RabbitMQ.csproj", "BuildingBlocks/MessageBroker/MessageBroker.RabbitMQ/"]
COPY ["BuildingBlocks/MessageBroker/MessageBroker.Kafka/MessageBroker.Kafka.csproj", "BuildingBlocks/MessageBroker/MessageBroker.Kafka/"]

RUN dotnet restore "Services/EmailReceiver/EmailReceiver.API/EmailReceiver.API.csproj"
COPY . .
WORKDIR "/src/Services/EmailReceiver/EmailReceiver.API"
RUN dotnet build "EmailReceiver.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EmailReceiver.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EmailReceiver.API.dll"]