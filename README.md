# Movies API
.NET Core 6 API for Movies

## Restore DB
Only if you don't have the movie.db SQLite DB already, or you want to recreate it, you can delete the file and do the automated creation
```cmd
dotnet ef database update
```

If you don't have dotnet-ef run: 
  
```cmd
dotnet tool install --global dotnet-ef
```

## Run the API 
```cmd
dotnet run
```

Enter de local URL
- https://localhost:7239/swagger/index.html

Excute the data population end point if you want to clean the tables and insert demo data importe from movies.csv:

- CleanAndResedData

Wait a few minutes is a huge set of data will take a while to insert all data.