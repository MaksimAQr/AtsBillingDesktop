# AtsBillingDesktop
A small learning app using the Avalonia framework

**The application has been implemented:** 
- multi-file structure
- multiple inheritance
- MVVM pattern implemented
- Service functions are taken from the work from my [repository](https://github.com/MaksimAQr/PolytechProgrammingWorkshop)
- Implemented import and export of data to a JSON file
- It is possible to save in the database

How to compile project
1. Go to develelop branch
```sh
git checkout develop
```
2. Build the app
```sh
dotnet --version
dotnet clean
dotnet restore
dotnet publish -c Release -r osx-arm64 --self-contained true /p:PublishSingleFile=true
```


