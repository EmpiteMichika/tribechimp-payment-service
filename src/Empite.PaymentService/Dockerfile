FROM microsoft/dotnet:2.1-aspnetcore-runtime
ARG source
WORKDIR /app
EXPOSE 80
EXPOSE 443
COPY ${source:-obj/Docker/publish} .
ENTRYPOINT ["dotnet", "Empite.TribechimpService.CoreService.dll"]
