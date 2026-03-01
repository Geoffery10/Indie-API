# Indie API

![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/Geoffery10/Indie-API/tests.yml?label=tests)

A straightforward backend API built with ASP.NET Core that powers [indie.geoffery10.com](https://indie.geoffery10.com) and provides content down to the frontend.

## Features

The API acts as a data provider with endpoints catering to different sections of the site:

- **Articles / Blogs / Projects**: Serves markdown-based articles, separated into projects and blogs.
- **Art**: Integrates with Immich to serve art assets.
- **Home Assistant**: Interacts with a local Home Assistant instance.
- **Bible**: Dedicated endpoints for serving Bible-related content.

## Tech Stack

- **C# & .NET**: Built leveraging ASP.NET Core Minimal APIs.
- **Integrations**: Includes specific services interacting with Immich and Home Assistant via HTTP clients.
