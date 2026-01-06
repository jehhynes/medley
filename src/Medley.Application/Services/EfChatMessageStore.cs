using System.Text.Json;
using Medley.Application.Interfaces;
using Medley.Domain.Enums;
using Microsoft.Agents.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using DomainChatMessage = Medley.Domain.Entities.ChatMessage;

namespace Medley.Application.Services;

/// <summary>
/// Entity Framework-backed chat message store for persistent conversation history
/// Implements Microsoft Agent Framework's ChatMessageStore pattern
/// </summary>
public sealed class EfChatMessageStore : ChatMessageStore
{
    private readonly IRepository<DomainChatMessage> _messageRepository;
    private readonly IUnitOfWork _unitOfWork;
    public Guid ConversationId { get; }

    public EfChatMessageStore(
        IRepository<DomainChatMessage> messageRepository,
        IUnitOfWork unitOfWork,
        JsonElement serializedStoreState,
        JsonSerializerOptions? jsonSerializerOptions = null)
    {
        _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

        // Deserialize the conversation ID from the thread state
        if (serializedStoreState.ValueKind is JsonValueKind.String)
        {
            var conversationIdString = serializedStoreState.GetString();
            if (!string.IsNullOrEmpty(conversationIdString) && Guid.TryParse(conversationIdString, out var guid))
            {
                ConversationId = guid;
            }
        }
    }

    /// <summary>
    /// Add chat messages to the store
    /// </summary>
    public override async Task AddMessagesAsync(
        IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        // Get the latest message timestamp to ensure proper ordering
        var lastMessage = await _messageRepository.Query()
            .Where(m => m.ConversationId == ConversationId)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var lastDate = lastMessage?.CreatedAt ?? DateTimeOffset.UtcNow;

        foreach (var aiMessage in messages)
        {
            var messageRole = MapChatRoleToMessageRole(aiMessage.Role);

            // For user messages, check if one already exists (to avoid duplicates from Agent Framework)
            DomainChatMessage? existingMessage = null;
            if (aiMessage.Role == ChatRole.User)
            {
                // Find the most recent user message without Agent Framework metadata
                existingMessage = await _messageRepository.Query()
                    .Where(m => m.ConversationId == ConversationId 
                             && m.Role == ChatMessageRole.User
                             && string.IsNullOrEmpty(m.SerializedMessage)) // Hasn't been updated by Agent Framework yet
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (existingMessage != null)
            {
                // Update the existing user message with Agent Framework metadata
                existingMessage.SerializedMessage = JsonSerializer.Serialize(aiMessage);
                
                await _messageRepository.SaveAsync(existingMessage);
            }
            else
            {
                // Create new message (for assistant, system, tool messages)
                
                // Determine the creation date with proper ordering
                DateTimeOffset createdAt;
                if (aiMessage.CreatedAt.HasValue && aiMessage.CreatedAt.Value > lastDate)
                {
                    createdAt = aiMessage.CreatedAt.Value;
                }
                else
                {
                    // Use last date + 1 ms to maintain order
                    createdAt = lastDate.AddMilliseconds(1);
                }
                lastDate = createdAt;

                var chatMessage = new DomainChatMessage
                {
                    ConversationId = ConversationId,
                    Role = messageRole,
                    Content = aiMessage.Text ?? string.Empty,
                    CreatedAt = createdAt,
                    SerializedMessage = JsonSerializer.Serialize(aiMessage),
                    UserId = null // Assistant/system/tool messages don't have a user
                };

                // Use the message ID from the AI message if available, or generate one for tool messages
                if (aiMessage.Role == ChatRole.Tool)
                {
                    // Tool call messages don't have unique IDs, so generate one
                    chatMessage.Id = Guid.NewGuid();
                }
                else if (!string.IsNullOrEmpty(aiMessage.MessageId) && Guid.TryParse(aiMessage.MessageId, out var messageId))
                {
                    chatMessage.Id = messageId;
                }

                await _messageRepository.SaveAsync(chatMessage);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Get recent messages from the store (in ascending chronological order)
    /// </summary>
    public override async Task<IEnumerable<Microsoft.Extensions.AI.ChatMessage>> GetMessagesAsync(
        CancellationToken cancellationToken = default)
    {
        var messages = await _messageRepository.Query()
            .Where(m => m.ConversationId == ConversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        return messages
           .Select(msg =>
           {
               // Try to deserialize the serialized message first
               if (!string.IsNullOrEmpty(msg.SerializedMessage))
               {
                   try
                   {
                       var deserializedMessage = JsonSerializer.Deserialize<ChatMessage>(msg.SerializedMessage);
                       if (deserializedMessage != null)
                       {
                           return deserializedMessage;
                       }
                   }
                   catch (JsonException)
                   {
                       // Fall through to manual creation
                   }
               }

               // Fallback: manually create ChatMessage from domain entity
               var chatRole = MapMessageRoleToChatRole(msg.Role);
               return new ChatMessage(chatRole, msg.Content)
               {
                   MessageId = msg.Id.ToString(),
                   CreatedAt = msg.CreatedAt
               };
           })
           .ToList();
    }

    /// <summary>
    /// Serialize the store state (the conversation ID)
    /// </summary>
    public override JsonElement Serialize(JsonSerializerOptions? jsonSerializerOptions = null)
    {
        return JsonSerializer.SerializeToElement(ConversationId.ToString());
    }

    /// <summary>
    /// Map Microsoft.Extensions.AI ChatRole to domain ChatMessageRole
    /// </summary>
    private static ChatMessageRole MapChatRoleToMessageRole(ChatRole role)
    {
        return role.Value switch
        {
            "user" => ChatMessageRole.User,
            "assistant" => ChatMessageRole.Assistant,
            "system" => ChatMessageRole.System,
            "tool" => ChatMessageRole.Tool,
            _ => ChatMessageRole.Assistant
        };
    }

    /// <summary>
    /// Map domain ChatMessageRole to Microsoft.Extensions.AI ChatRole
    /// </summary>
    private static ChatRole MapMessageRoleToChatRole(ChatMessageRole messageRole)
    {
        return messageRole switch
        {
            ChatMessageRole.User => ChatRole.User,
            ChatMessageRole.Assistant => ChatRole.Assistant,
            ChatMessageRole.System => ChatRole.System,
            ChatMessageRole.Tool => ChatRole.Tool,
            _ => ChatRole.Assistant
        };
    }
}
