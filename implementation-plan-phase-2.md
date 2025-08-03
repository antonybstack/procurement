# C#/.NET AI Stack for a Procurement POC

This README outlines a modern, containerized AI stack built entirely on C# and .NET for a procurement suggestion service Proof of Concept (POC). The stack is designed for a local development environment (e.g., M4 Mac Mini) and prioritizes simplicity and integration with existing technologies.

## 🚀 Overview

The goal is to create a service that, given a natural language request for a part, suggests potential suppliers. The architecture uses a **Retrieval-Augmented Generation (RAG)** pattern, which is a powerful and efficient way to ground a Large Language Model (LLM) in your specific database data.

The entire stack is containerized for easy setup and deployment using Docker Compose.

## 📦 Stack Components

| Component | Technology | Description |
| :--- | :--- | :--- |
| **Backend** | `C# / ASP.NET 9` | The core application logic and API endpoints. This service orchestrates the entire AI workflow, from receiving a user query to generating a response. |
| **LLM Framework** | `Microsoft Semantic Kernel` | The C# equivalent of frameworks like LangChain. It handles the complex RAG workflow, including retrieving context from the database and constructing the final prompt for the LLM. |
| **Vector Database** | `PostgreSQL` with `pgvector` | Your existing database, extended with the `pgvector` extension to enable efficient semantic search. This is where both your raw data and its vector embeddings are stored. |
| **LLM Runtime** | `Ollama` | A containerized, user-friendly service for running open-source LLMs locally on Apple Silicon. It provides a simple API that our C# backend will call. |
| **LLM Model** | `Llama 3 8B` | A powerful, small-footprint LLM that runs efficiently on the M4 Mac Mini. These models are capable of high-quality reasoning and text generation for our use case. |
| **LLM Client** | `OllamaSharp` NuGet package | A dedicated C# client library for communicating with the Ollama API, used for generating embeddings and getting text completions. |
| **Containerization** | `Docker Compose` | Manages and orchestrates all the services (PostgreSQL, Ollama, and your ASP.NET app) for a seamless development experience. |

## 🏗️ Workflow

### 1. Data Ingestion (Initial Setup)

This is a one-time or periodic process to prepare your data for the RAG system.

1.  **Retrieve Data:** The `.NET` service queries your `PostgreSQL` database (using EF Core) to fetch relevant supplier and part information.
2.  **Generate Embeddings:** The service uses the `OllamaSharp` client to send the text data (e.g., supplier capabilities, certifications) to the `Ollama` container, which returns a vector embedding for each piece of text.
3.  **Store Vectors:** The service uses EF Core to persist these vector embeddings in a dedicated `pgvector` column within your `PostgreSQL` database.

### 2. Suggestion Generation (On User Request)

This is the real-time process triggered by a user in the Angular front-end.

1.  **Receive Query:** A user submits a query (e.g., "Suggest suppliers for an aluminum 7075 bracket with AS9100 certification") to your `.NET` API.
2.  **Query Embedding:** The `.NET` service uses the `OllamaSharp` client to generate a vector embedding for the user's query.
3.  **Semantic Search:** The service queries the `pgvector` column in `PostgreSQL` with the user's query embedding, retrieving the most semantically relevant supplier records. This is the **"Retrieval"** step of RAG.
4.  **Augment Prompt:** The `Microsoft Semantic Kernel` takes the user's original query and combines it with the retrieved supplier data. This creates a detailed, contextual prompt (e.g., "Based on the following supplier data... recommend suppliers for an aluminum 7075 bracket...").
5.  **Generate Response:** The service sends this augmented prompt to the `Ollama` container. The LLM (`Mistral` or `Llama`) generates a natural language response based *only* on the provided context. This is the **"Generation"** step of RAG.
6.  **Return Result:** The `.NET` service returns the generated suggestion to the Angular front-end.

## 🔧 Implementation Guide

1.  **Docker Compose:** Create a `docker-compose.yml` file to spin up `PostgreSQL` (with `pgvector` enabled) and `Ollama`.
2.  **C# Project:** Set up an ASP.NET 9 project.
3.  **NuGet Packages:** Add `Microsoft.SemanticKernel`, `OllamaSharp`, and `Npgsql.EntityFrameworkCore.PostgreSQL`.
4.  **EF Core:** Configure EF Core to connect to your `PostgreSQL` container and work with the `pgvector` column type.
5.  **Service Logic:** Implement a service that encapsulates the RAG workflow described above, using `OllamaSharp` to call the Ollama container and `Semantic Kernel` to manage the prompt and completion steps.
6.  **API Endpoint:** Create a simple API endpoint that accepts a user query and returns the AI-generated suggestion.
7.  

## 📝 Notes

- the `ai-service` will be deprecated--we are not using python anymore.
- SqlCoder model will be deprecated in favor of the Ollama model.
- the `AiRecommendationsController` will be deprecated.
- Do not rely on cloud providers such as Azure. Everything should be self-contained, open-source, and run locally on a M4 Mac Mini, or a Linux server.
- `OllamaSharp` is a C# client library for the Ollama API. It is used to generate embeddings and get text completions from the Ollama container.
  - https://github.com/awaescher/OllamaSharp
- .NET AI resources:
  - https://learn.microsoft.com/en-us/dotnet/ai/semantic-kernel-dotnet-overview
  - https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/chat-local-model
  - https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/chat-local-model
  - https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/ai-templates