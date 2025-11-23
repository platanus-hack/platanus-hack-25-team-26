#!/bin/bash
cd "$(dirname "$0")"
ELECTRON_RUN_AS_NODE= exec npx electron .
