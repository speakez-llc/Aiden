# Aiden - The AI Admin 🤖
This project is a showcase for "taming complexity" using *adaptive* machine learning. It simulates a real-world use case from a project in our portfolio. In full, it demonstrates our view of how analytic and operational systems are melding. 

<p align="center">
    <img src='img/Aiden_logo.jpg' width=50%>
</p>

Everyone *talks* about AI, but Rower is putting forward a working example of what we view as a cohesive new class of software. These applications forge-weld together what previously was thought of as the distinct domains of "analytics" and "operational" systems into a new form centered on intelligent tooling and human-centered design.

## Summary 📰
"Aiden" is a focused proof-of-concept - a structured set of algorithms that performs analysis of data that assists a systems admin in a mission-critical role. It reviews incoming network telemetry and discovers threat-actor patterns. With its ability to analyze data "in flight" it is able to rapidly offer responses that prompt the human operator to review findings and confirm the mitigation strategy. 

## Technology 📐
Even though this only a proof of concept, we plan to show how we build and deploy a full multi-targeting codebase based on .NET technologies. We use Avalonia to deploy versions to desktop, mobile and web, and *under the hood* it also leverages a blend of well-established tools intertwined with some of the most recent developments in AI and machine learning. The key technology we're using for "Aiden" is Microsoft's [Semantic Kernel](https://github.com/microsoft/semantic-kernel), which can be thought of as an orchestrator for large language models - but can also serve to connect a wide variety of tools. 

Aside from our current focus in .NET, it should be noted that Semantic Kernel *also* has support for python and java. More can be learned in the documentation [here](https://learn.microsoft.com/en-us/semantic-kernel/overview/). 