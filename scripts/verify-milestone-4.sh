#!/usr/bin/env bash

set -euo pipefail

dotnet build CtrlEat.slnx
dotnet test CtrlEat.slnx

cd src/User.Client
npm ci
npm run build
