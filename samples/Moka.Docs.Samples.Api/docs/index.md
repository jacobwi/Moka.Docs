---
title: SampleApi
layout: landing
description: A modern minimal API for .NET 9
order: 1
---

# SampleApi

A blazing-fast minimal API built with .NET 9 — featuring todo management, weather forecasting, and full OpenAPI documentation.

[Get Started](/guide/getting-started) [View API Reference](/api)

## Features

- **Todo Management** — Full CRUD operations with priority levels and due dates. Built with thread-safe in-memory storage.
- **Weather Forecasting** — Real-time current conditions and multi-day forecasts with detailed weather models.
- **OpenAPI Integration** — Auto-generated OpenAPI spec at `/openapi/v1.json` for seamless client generation.
- **Type-Safe Models** — Immutable records with `TodoItem`, `WeatherForecast`, `TodoPriority`, and `WeatherCondition` enums.
- **XML Documentation** — Every public type and member is fully documented with XML doc comments.
- **Zero Config** — Just `dotnet run` and you're up. No database, no external dependencies.

---

## Quick Start

```bash
dotnet run
```

The API will be available at `https://localhost:5001`. Visit `/openapi/v1.json` for the OpenAPI spec.
