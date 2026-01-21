# SystemPromptBuilder JSON Format

## Overview

The `SystemPromptBuilder` service outputs structured JSON format for all system prompts, providing better structure, type safety, and easier parsing.

## Usage

```csharp
var jsonPrompt = await systemPromptBuilder.BuildPromptAsync(
    articleId: articleGuid,
    planId: planGuid,
    promptType: PromptType.ArticlePlanImplementation
);
```

## JSON Output Structure

The JSON output follows this structure:

```json
{
  "template": {
    "type": "ArticlePlanImplementation",
    "content": "You are an AI assistant helping to improve articles..."
  },
  "article": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "Product Strategy Overview",
    "type": "Strategy Document",
    "summary": "A comprehensive overview of our product strategy",
    "status": "Draft",
    "content": "# Product Strategy\n\nOur product strategy focuses on..."
  },
  "plan": {
    "id": "7fa85f64-5717-4562-b3fc-2c963f66afa6",
    "instructions": "Enhance the product strategy document with customer insights",
    "fragments": [
      {
        "id": "9fa85f64-5717-4562-b3fc-2c963f66afa6",
        "title": "Customer Interview - Q4 2025",
        "summary": "Key insights from customer interviews",
        "content": "Customers expressed strong interest in...",
        "similarityScore": 87.5,
        "instructions": "Incorporate the customer pain points mentioned",
        "source": {
          "name": "Q4 Customer Research",
          "type": "Transcript"
        }
      }
    ]
  }
}
```

## Schema Details

### Root Object: `SystemPromptData`

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `template` | `TemplateData` | No | AI prompt template if specified |
| `article` | `ArticleData` | Yes | The article being worked on |
| `plan` | `PlanData` | No | Improvement plan if specified |

### `TemplateData`

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `type` | `string` | Yes | The prompt type enum value |
| `content` | `string` | Yes | The template content/instructions |

### `ArticleData`

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `id` | `string` (GUID) | Yes | Article unique identifier |
| `title` | `string` | Yes | Article title |
| `type` | `string` | No | Article type name |
| `summary` | `string` | No | Article summary |
| `status` | `string` | Yes | Article status (Draft, Published, etc.) |
| `content` | `string` | Yes | Current article content |

### `PlanData`

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `id` | `string` (GUID) | Yes | Plan unique identifier |
| `instructions` | `string` | Yes | Overall plan instructions |
| `fragments` | `FragmentData[]` | Yes | Array of included fragments |

### `FragmentData`

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `id` | `string` (GUID) | Yes | Fragment unique identifier |
| `title` | `string` | Yes | Fragment title |
| `summary` | `string` | No | Fragment summary |
| `content` | `string` | Yes | Fragment content |
| `similarityScore` | `number` | Yes | Similarity percentage (0-100) |
| `instructions` | `string` | No | Fragment-specific instructions |
| `source` | `SourceData` | No | Source information |

### `SourceData`

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | `string` | Yes | Source name |
| `type` | `string` | Yes | Source type (Transcript, Document, etc.) |

## Benefits of JSON Format

1. **Structured Data**: Easier to parse and process programmatically
2. **Type Safety**: Clear data types for each field
3. **Extensibility**: Easy to add new fields without breaking parsing
4. **Tooling Support**: Better IDE support and validation
5. **API Integration**: Simpler integration with REST APIs and other services
6. **Null Handling**: Explicit handling of optional fields

## Example: Parsing JSON Output

```csharp
using System.Text.Json;

var jsonPrompt = await systemPromptBuilder.BuildPromptAsync(articleId);
var promptData = JsonSerializer.Deserialize<SystemPromptData>(jsonPrompt);

// Access structured data
Console.WriteLine($"Article: {promptData.Article.Title}");
Console.WriteLine($"Status: {promptData.Article.Status}");

if (promptData.Plan != null)
{
    Console.WriteLine($"Plan has {promptData.Plan.Fragments.Count} fragments");
}
```
