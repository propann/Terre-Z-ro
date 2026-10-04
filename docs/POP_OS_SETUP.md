# Terre Zéro — Pop!_OS development setup

## Goal

Run the Godot .NET client, the Go world backend and Linux desktop location on a clean Pop!_OS workstation.

## Required tools

- Git
- .NET SDK 8
- Go 1.24 or newer
- Python 3
- Godot 4 .NET/Mono build
- GeoClue for permission-driven approximate desktop location

After cloning:

~~~bash
bash tools/check_popos_dev.sh
~~~

This script changes nothing; it only reports what is available.

## Repository

~~~bash
git clone https://github.com/propann/Terre-Z-ro.git
cd Terre-Z-ro
git pull origin main
~~~

## C# verification

~~~bash
dotnet restore godot_project/TerreZero.csproj
dotnet build godot_project/TerreZero.csproj
python3 tools/validate_godot_resources.py
~~~

## GeoClue

Terre Zéro never queries Linux desktop location before the player enables the permission toggle.

Current Linux adapter path:

~~~text
/usr/libexec/geoclue-2.0/demos/where-am-i
~~~

If available, the game reads approximate latitude, longitude and accuracy. If unavailable, configured fallback coordinates remain usable.

Deterministic development override:

~~~bash
export TERRE_ZERO_LATITUDE=45.75
export TERRE_ZERO_LONGITUDE=4.85
~~~

These variables are read only after location permission is granted.

## Go backend

~~~bash
cd godot_project/server_go
go mod tidy
go run .
~~~

Default API:

~~~text
http://127.0.0.1:8080
~~~

The backend supplies H3 validation, world cells, voxel sync and real-weather proxy/cache.

## Start Godot

Open:

~~~text
godot_project/project.godot
~~~

For real-world start selection enable `UseRemoteWorldData` on WorldBootstrap.

Flow:

1. grant desktop-location permission;
2. GeoClue resolves an approximate machine point;
3. choose a start within 5 km or 10 km;
4. backend validates it;
5. selected coordinates become the fixed H3 and weather anchor.

## Clean rebuild

If Godot appears to execute stale C# code:

~~~bash
rm -rf godot_project/.godot godot_project/bin godot_project/obj
dotnet restore godot_project/TerreZero.csproj
dotnet build godot_project/TerreZero.csproj
~~~

Then reopen `godot_project/project.godot`.

## Fast local world stack with Docker

From the repository root:

~~~bash
docker compose up -d --build
~~~

This starts only:

- PostgreSQL 16 + PostGIS;
- Terre Zéro Go backend.

No Web prototype is started.

Backend:

~~~text
http://127.0.0.1:8080
~~~

Check it:

~~~bash
curl http://127.0.0.1:8080/api/v1/health
~~~

The first PostGIS volume initialization automatically executes:

~~~text
godot_project/server_go/schema.sql
~~~

If the database volume already exists and the schema changed, re-apply it with:

~~~bash
docker compose exec postgis \
  psql -U terre_zero -d terre_zero -f /schema.sql
~~~

To reset the development database completely:

~~~bash
docker compose down -v
docker compose up -d --build
~~~

Logs:

~~~bash
docker compose logs -f backend
~~~

Stop:

~~~bash
docker compose down
~~~
