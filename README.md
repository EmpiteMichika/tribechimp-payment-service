![alt text](http://empite.com/img/logo.png "Empite")
# Tribechimp API

#### Pre-requisits

| Requirment | Download Location |
| ------ | ------ |
| Dotnet core 2.1.3  | https://www.microsoft.com/net/download/dotnet-core/2.1 |
| MySQL | https://dev.mysql.com/downloads/ |
| Docker (for RabbitMQ) | https://docs.docker.com/docker-for-windows/install/ |

#### Install RabbitMQ

To install RabbitMQ, Frist install docker and then run the following command 

```sh
docker run -d -p 15672:15672 -p 5672:5672 --name rabbitmq rabbitmq:3-management
```

#### Micro-services and API Setup

Follow the following steps for each solution

> Step 01:

Switch to "develop" branch

> Step 02:

Navigate to "src/{web project folder}/Opt/Conf" folder and set correct Sql connection settings (Not required for API project because it doesn't use databases)

> Step 03:

Open a comand prompt or gitBash command prompt inside the web project root folder
Run the following Commands

```sh
dotnet restore -s https://www.myget.org/F/empite/auth/1f792da9-f6bd-4bd6-84cd-77833d9fc898/api/v3/index.json -s https://api.nuget.org/v3/index.json
dotnet run / dotnet watch run 
```
You're good to go!
Go to  https://localhost:5001/swagger/index.html to see the Swagger documentation of the API project
