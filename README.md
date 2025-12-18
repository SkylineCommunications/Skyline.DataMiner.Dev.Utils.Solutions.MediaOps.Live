# Skyline.DataMiner.MediaOps.Live

This documentation describes how to use the public API exposed by `Skyline.DataMiner.MediaOps.Live`.
The API is intended to be used when developing custom solutions based on the MediaOps.LIVE solution.

## Installation

Add the NuGet package to your solution:

```bash
dotnet add package Skyline.DataMiner.MediaOps.Live
```

Depending on your project type, one of the following additional packages is also required:
- Automation scripts: `Skyline.DataMiner.MediaOps.Live.Automation`
- Protocols: `Skyline.DataMiner.MediaOps.Live.Protocols`
- GQI Ad-hoc Data Sources and custom operators: `Skyline.DataMiner.MediaOps.Live.GQI`

> [!NOTE]
> This library targets `.NET Framework 4.8`.

## Documentation

- **[Getting Started](Documentation/Getting%20Started.md)** - Installation and basic usage
- **[Examples](Documentation/Examples.md)** - Common usage patterns
- **[Advanced Topics](Documentation/Advanced%20Topics.md)** - Caching, Subscriptions, validation, and more
- **[Orchestration](Documentation/Orchestration.md)** - Orchestration concepts and usage

## About DataMiner

DataMiner is a transformational platform that provides vendor-independent control and monitoring of devices and services. Out of the box and by design, it addresses key challenges such as security, complexity, multi-cloud, and much more. It has a pronounced open architecture and powerful capabilities enabling users to evolve easily and continuously.

The foundation of DataMiner is its powerful and versatile data acquisition and control layer. With DataMiner, there are no restrictions to what data users can access. Data sources may reside on premises, in the cloud, or in a hybrid setup.

A unique catalog of 7000+ connectors already exists. In addition, you can leverage DataMiner Development Packages to build your own connectors (also known as "protocols" or "drivers").

> **Note**
> See also: [About DataMiner](https://aka.dataminer.services/about-dataminer).

## About Skyline Communications

At Skyline Communications, we deal with world-class solutions that are deployed by leading companies around the globe. Check out [our proven track record](https://aka.dataminer.services/about-skyline) and see how we make our customers' lives easier by empowering them to take their operations to the next level.
