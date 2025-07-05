#!/bin/bash

# Wait for PostgreSQL to be ready
echo "Waiting for PostgreSQL to be ready..."
while ! nc -z postgres 5432; do
  sleep 1
done
echo "PostgreSQL is ready!"

# Wait a bit more to ensure the database is fully initialized
sleep 5

# Start the application
exec dotnet ProcurementAPI.dll 