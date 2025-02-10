FROM mcr.microsoft.com/dotnet/sdk:7.0 AS dotnet-build
RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM maven:3.8.6-openjdk-11-slim AS java-build
RUN mvn dependency:go-offline
RUN mvn package

FROM python:3.9-slim AS python-build
RUN pip install --no-cache-dir -r requirements.txt

CMD ["bash"]