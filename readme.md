# denPusher

## Overview

`denPusher` is a CLI tool developed with .NET 8, intended for packaging into a tar archive, compressing using Zstandard, and/or uploading files to a web API. This tool simplifies the process of sending large amounts of data over the network by compressing the data locally before transmission. It's particularly useful for automating backups, batch processing of data for analysis, updating resources on a server, or uploading biaries for further installation.


It uses several libraries to facilitate its operations:
- **Cocona**: For easy command line interface creation and argument parsing.
- **Newtonsoft.Json**: For JSON data manipulation, crucial for web API communication.
- **ZstdNet**: For data compression using the Zstandard algorithm, optimizing upload efficiency.

## Features

- Packs using TAR and compresses using ZStandard;
- Uploads the compressed archive directly to a web API;
- Supports token-based authentication for secure API access;

## Prerequisites

- .NET 8 Runtime 

## Installation

To install `denPusher`, follow these steps:

1. Clone the repository:
```bash
git clone https://github.com/buchmiet/denPusher.git
cd denPusher
```

Restore NuGet packages:

```bash
dotnet restore
 ```

This will install Cocona, Newtonsoft.Json, ZstdNet, and other dependencies.

Build the project:

 ```bash
dotnet build
```

## Usage

### Tar Command

To create a tar archive from a source directory:

```bash
dotnet run --project denPusher tar <destination> [source directory]
```

1. destination: The output tar file name. This parameter is required.
2. source directory: The source directory to tar. If not specified, defaults to the current directory.

### Pack Command

To pack files into a tar archive and compress it with Zstandard:

```bash
dotnet run --project denPusher pack <destination> [source directory] [-c compression level]
```
1. destination: The output tar file name. This parameter is required.
2. source directory: The source directory to tar. If not specified, defaults to the current directory.
3. -c: Optional compression level using Zstandard, ranging from 1 (lowest compression) to 100 (highest compression). The default is 100.

### Upload Command

To pack, compress and upload a directory or file to a web API with optional authentication:

 ```bash
dotnet run --project denPusher --upload <API_URL> [SOURCE_DIRECTORY]
```

### Optional Parameters
- login (l): API endpoint for authentication, specifying only the endpoint part;
- username (u): Username for API authentication;
- password (p): Password for API authentication;
- compression (c): Sets the optional compression level using Zstandard, ranging from 1 (lowest compression) to 100 (highest compression), with a default of 100.
- version (v): Specifies the version of the application binaries, if required by the web API;

For authentication and to specify the compression level:

 ```bash
 dotnet run --project denPusher --upload --url <API_URL> [SOURCE_DIRECTORY] --login "LOGIN_ENDPOINT" --username "USERNAME" --password "PASSWORD" --compression 50 --version "0.8"
 ```

### Examples

```bash
dotnet run --project denPusher upload http://example.com/api/upload ./data
```

```bash
dotnet run --project denPusher upload http://example.com/api/upload ./data -l login -u user -p pass -c 75 -v 1.0
```

## Contributing
We appreciate contributions of any kind. To contribute:

1. Fork the repository.
2. Create a new branch for your feature or bug fix.
3. Commit your changes.
4. Push your branch and open a pull request.
5. License

denPusher is released under the MIT License. See the LICENSE file for more details.

### Version history
13/02/2024:  0.8 Initial release;