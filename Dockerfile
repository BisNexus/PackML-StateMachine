# Use the official .NET 10 SDK image
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build

# Set the working directory inside the container
WORKDIR /src

# Copy everything into the container
COPY . .

# Restore dependencies
RUN dotnet restore

# Build the solution (optional, but recommended for faster test runs)
RUN dotnet build --no-restore

# Run tests (change the path to your test project if needed)
# This will output results to the console and also save a TRX file in /src/TestResults
CMD ["dotnet", "test", "PackML-StateMachine.Tests/PackML-StateMachine.Tests.csproj", "--no-build"]