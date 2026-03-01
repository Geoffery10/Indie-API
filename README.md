# Indie API

![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/Geoffery10/Indie-API/tests.yml?label=tests)

A straightforward backend API built with ASP.NET Core that powers [indie.geoffery10.com](https://indie.geoffery10.com) and provides content down to the frontend.

## Status

Website badges:
![Status Website](https://up.geoffery10.com/api/badge/47/status?upColor=%23008e45&downColor=%23c6c49f&pendingColor=%23a2e029)
![Uptime Website](https://up.geoffery10.com/api/badge/47/uptime)

API badges:
![Status API](https://up.geoffery10.com/api/badge/49/status?upColor=%23008e45&downColor=%23c6c49f&pendingColor=%23a2e029)
![Uptime API](https://up.geoffery10.com/api/badge/49/uptime)

## Features

The API acts as a data provider with endpoints catering to different sections of the site:

- **Articles / Blogs / Projects**: Serves markdown-based articles, separated into projects and blogs.
- **Art**: Integrates with Immich to serve art assets.
- **Home Assistant**: Interacts with a local Home Assistant instance.
- **Bible**: Dedicated endpoints for serving Bible-related content.

## Tech Stack

- **Backend**: [Indie API](https://github.com/Geoffery10/Indie-API) - C# & .NET Core Minimal APIs.
- **Frontend**: [Indie Site](https://github.com/Geoffery10/Indie-Site) - HTML5, Vanilla CSS3, JavaScript.
- **Integrations**: Includes specific services interacting with Immich and Home Assistant via HTTP clients.
