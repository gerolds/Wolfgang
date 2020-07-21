FROM mcr.microsoft.com/dotnet/core/runtime:3.1
COPY bin/Release/netcoreapp3.1/ Wolfgang/
WORKDIR /Wolfgang
ENTRYPOINT ["dotnet", "Wolfgang.dll"]