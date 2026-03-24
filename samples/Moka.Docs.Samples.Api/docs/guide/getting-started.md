---
title: Getting Started
order: 1
---

# Getting Started with SampleApi

## Prerequisites

- .NET 9 SDK
- A REST client (curl, Postman, or similar)

## Running the API

```bash
cd samples/SampleApi
dotnet run
```

## Creating a Todo

```bash
curl -X POST https://localhost:5001/api/todos \
  -H "Content-Type: application/json" \
  -d '{"title": "Buy groceries", "priority": "High"}'
```

Response:

```json
{
  "id": 1,
  "title": "Buy groceries",
  "isCompleted": false,
  "priority": "High",
  "dueDate": null,
  "createdAt": "2026-03-19T10:30:00Z"
}
```

## Checking the Weather

```bash
curl https://localhost:5001/api/weather/forecast?days=3
```

## Models

The API uses these key types:

- `TodoItem` — The main todo record with ID, title, priority, and due date
- `TodoPriority` — Enum: `Low`, `Medium`, `High`, `Critical`
- `WeatherForecast` — Daily forecast with high/low temps and conditions
- `WeatherCondition` — Enum: `Sunny`, `PartlyCloudy`, `Cloudy`, `Rainy`, etc.
