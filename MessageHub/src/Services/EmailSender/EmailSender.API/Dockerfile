FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Services/EmailSender/EmailSender.API/EmailSender.API.csproj", "Services/EmailSender/EmailSender.API/"]
COPY ["Services/EmailSender/EmailSender.Domain/EmailSender.Domain.csproj", "Services/EmailSender/EmailSender.Domain/"]
COPY ["Services/EmailSender/EmailSender.Application/EmailSender.Application.csproj", "Services/EmailSender/EmailSender.Application/"]
COPY ["Services/EmailSender/EmailSender.Infrastructure/EmailSender.Infrastructure.csproj", "Services/EmailSender/EmailSender.Infrastructure/"]
COPY ["BuildingBlocks/MessageBroker/MessageBroker.Common/MessageBroker.Common.csproj", "BuildingBlocks/MessageBroker/MessageBroker.Common/"]
COPY ["BuildingBlocks/MessageBroker/MessageBroker.RabbitMQ/MessageBroker.RabbitMQ.csproj", "BuildingBlocks/MessageBroker/MessageBroker.RabbitMQ/"]
COPY ["BuildingBlocks/MessageBroker/MessageBroker.Kafka/MessageBroker.Kafka.csproj", "BuildingBlocks/MessageBroker/MessageBroker.Kafka/"]

RUN dotnet restore "Services/EmailSender/EmailSender.API/EmailSender.API.csproj"
COPY . .
WORKDIR "/src/Services/EmailSender/EmailSender.API"
RUN dotnet build "EmailSender.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EmailSender.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EmailSender.API.dll"]