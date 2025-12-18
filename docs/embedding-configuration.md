# Embedding Configuration Guide

This document explains how to configure the embedding provider for Medley.

## Overview

Medley supports two embedding providers:
- **Ollama** (default) - Local embedding using qwen3-embedding:4b
- **OpenAI** - Cloud-based embedding using text-embedding-3-large

## Configuration

The embedding configuration is located in `appsettings.json` under the `Embedding` section:

```json
{
  "Embedding": {
    "Provider": "Ollama",
    "Dimensions": 2000,
    "Ollama": {
      "BaseUrl": "http://localhost:11434",
      "Model": "qwen3-embedding:4b"
    },
    "OpenAI": {
      "ApiKey": "",
      "Model": "text-embedding-3-large"
    }
  }
}
```

### Configuration Options

#### Provider
- **Type**: `string`
- **Values**: `"Ollama"` or `"OpenAI"`
- **Default**: `"Ollama"`
- **Description**: Specifies which embedding provider to use

#### Dimensions
- **Type**: `integer`
- **Default**: `2000`
- **Description**: The dimension of the embedding vectors. Note that text-embedding-3-large supports up to 3072 dimensions, but we use 2000 for consistency with the database schema.

### Ollama Settings

#### BaseUrl
- **Type**: `string`
- **Default**: `"http://localhost:11434"`
- **Description**: The URL where your Ollama instance is running

#### Model
- **Type**: `string`
- **Default**: `"qwen3-embedding:4b"`
- **Description**: The Ollama model to use for embeddings

### OpenAI Settings

#### ApiKey
- **Type**: `string`
- **Required**: Yes (when using OpenAI provider)
- **Description**: Your OpenAI API key. Get one from https://platform.openai.com/api-keys

#### Model
- **Type**: `string`
- **Default**: `"text-embedding-3-large"`
- **Description**: The OpenAI embedding model to use

## Usage Examples

### Using Ollama (Default)

```json
{
  "Embedding": {
    "Provider": "Ollama",
    "Dimensions": 2000,
    "Ollama": {
      "BaseUrl": "http://localhost:11434",
      "Model": "qwen3-embedding:4b"
    }
  }
}
```

**Prerequisites:**
1. Install Ollama from https://ollama.ai
2. Pull the embedding model: `ollama pull qwen3-embedding:4b`
3. Ensure Ollama is running on port 11434

### Using OpenAI

```json
{
  "Embedding": {
    "Provider": "OpenAI",
    "Dimensions": 2000,
    "OpenAI": {
      "ApiKey": "sk-proj-...",
      "Model": "text-embedding-3-large"
    }
  }
}
```

**Prerequisites:**
1. Sign up for an OpenAI account at https://platform.openai.com
2. Create an API key
3. Add the API key to your configuration

**Important:** Never commit your API key to source control. Use environment variables or user secrets for production:

```bash
# Using environment variables
export Embedding__OpenAI__ApiKey="sk-proj-..."

# Or using .NET user secrets (recommended for development)
dotnet user-secrets set "Embedding:OpenAI:ApiKey" "sk-proj-..."
```

## Cost Considerations

### Ollama
- **Cost**: Free (runs locally)
- **Performance**: Depends on your hardware
- **Privacy**: Data stays on your machine

### OpenAI
- **Cost**: Pay per token (see https://openai.com/pricing)
- **Performance**: Fast, cloud-based
- **Privacy**: Data is sent to OpenAI's servers

For text-embedding-3-large, the pricing is approximately $0.13 per 1M tokens (as of Dec 2024).

## Switching Providers

To switch between providers:

1. Update the `Provider` field in `appsettings.json`
2. Ensure the required configuration is set (API key for OpenAI, or Ollama running locally)
3. Restart the application

**Note:** Existing embeddings in the database will not be regenerated. Only new fragments will use the new provider.

## Troubleshooting

### Ollama Issues

**Error: "Connection refused"**
- Ensure Ollama is running: `ollama serve`
- Check the BaseUrl is correct

**Error: "Model not found"**
- Pull the model: `ollama pull qwen3-embedding:4b`

### OpenAI Issues

**Error: "OpenAI API key is required"**
- Ensure you've set the `ApiKey` in configuration
- Check that the key is valid and not expired

**Error: "Rate limit exceeded"**
- You've exceeded your OpenAI API quota
- Wait or upgrade your OpenAI plan

## Architecture

The embedding system uses Microsoft.Extensions.AI abstractions, which provides a unified interface for different AI providers. The implementation:

1. `EmbeddingSettings.cs` - Configuration model
2. `DependencyInjection.cs` - Service registration based on provider
3. `EmbeddingGenerationJob.cs` - Background job that generates embeddings
4. `FragmentsApiController.cs` - Uses embeddings for semantic search

The system automatically generates embeddings for new fragments in the background using Hangfire.

