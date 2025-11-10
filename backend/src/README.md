# LMS - Backend
.NET 8 API designed for Loan Management.

## Project:
- **.NET 8**
- **Clean Architecture**
- **Entity Framework**
- **Serilog**
- **Docker**
- **Swagger**

## LMS Backend - How to build and run

The application runs with Docker, so you just need to execute (in `src` folder) the command:  
```sh
docker compose up -d
```

## LMS Migrations
To generate the migrations, execute the following inside the `src` folder:
```sh
dotnet ef migrations add InitialCreate --project LMS.Infrastructure --startup-project LMS.WebApi

dotnet ef database update --project LMS.Infrastructure --startup-project LMS.WebApi
```

## Tests 
To run all tests with Coverage execute in `src` folder:  
```sh
dotnet test LMS.Services.Tests --collect:"XPlat Code Coverage" --results-directory LMS.Services.Tests/TestResults --settings LMS.Services.Tests/coverlet.runsettings
```
The results will be generated inside `LMS.Services.Tests/TestResults`

To run without Coverage execute:
```sh
dotnet test LMS.Services.Tests
```

## Endpoints
- API: http://localhost:5000
- Swagger: http://localhost:5000/swagger