# NugetVersionChecker

## Options

- --help displays all options and syntax

## Example usage

### Output `csv` and `json`

This example will output `myproject.csv` and `myproject.json` in the current directory:

```bash
$ dotnet run -- --project /path/to/project/myproject.csproj -c . -j .

# Or, if you built the executable using AOT
$ NugetVersionChecker --project /path/to/project/myproject.csproj -c . -j .
```

### Output json to the console

```bash
$ dotnet run -- --project /path/to/project/myproject.csproj

# Or, if you built the executable using AOT
$ NugetVersionChecker --project /path/to/project/myproject.csproj
```

### Debug output

Set the LOGGING__LOGLEVEL__DEFAULT environment variable to `Debug` or `Trace` to see more output.

```bash
# All in one line when using bash
$ LOGGING__LOGLEVEL__DEFAULT=Debug dotnet run -- --project /path/to/project/myproject.csproj
```
