# School Admission System

A school admission management system developed as an Assistant Software Engineer recruitment take-home assignment for Onnorokom Projukti Limited.

## Project Overview

The system manages school admission applications for classes and shifts with limited seat availability.

It supports temporary seat reservations, admission confirmation, hold expiration, and student application tracking while preventing seat overbooking.

## Technology Stack

| Layer | Technology |
|---|---|
| Backend | .NET 8+ (C#) |
| Database | PostgreSQL |
| Frontend | React 18+ |
| Frontend Tooling | Vite |
| Containerization | Docker Compose |

## Core Features

- Class and shift management
- Seat availability tracking
- Temporary admission seat holds
- Automatic hold expiration
- Admission confirmation
- Hold cancellation
- Student application tracking
- Concurrent reservation protection
- Persistent database storage

## Project Structure

- `backend/` — ASP.NET Core Web API and backend components
- `frontend/` — React application
- `docker-compose.yml` — Application and database orchestration

## Additional Documentation

- `ASSUMPTIONS.md` — Project assumptions and implementation decisions
- `AI_USAGE.md` — Documentation of AI assistance and verification

## Getting Started

The complete application will be configured to run using Docker Compose.

Run the following command from the repository root:

`docker compose up --build`

Detailed setup instructions will be added as development progresses.

## Backend URL

The backend API is expected to run at:

`http://localhost:8080`

## Assignment Information

- Company: Onnorokom Projukti Limited
- Assignment: School Admission System
- Position: Assistant Software Engineer

## Development Status

Repository initialization in progress.

The application implementation, testing, containerization, and final documentation will be completed incrementally.

## License

This repository is created for the recruitment assignment.
