#!/bin/sh
set -eu

# Named volumes are owned by root; the app user must be able to write SQLite.
mkdir -p /app/data
if [ "$(id -u)" = "0" ]; then
  uid="${APP_UID:-1654}"
  chown -R "$uid:$uid" /app/data
  exec gosu "$uid" dotnet Rbac.Sample.Api.dll "$@"
fi

exec dotnet Rbac.Sample.Api.dll "$@"
