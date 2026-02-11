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
    private readonly IRepository<Domain.Entities.ChatConversation> _conversationRepository;
    private readonly IUnitOfWork _unitOfWork;
    public Guid ConversationId { get; }

    public EfChatMessageStore(
        IRepository<DomainChatMessage> messageRepository,
        IRepository<Domain.Entities.ChatConversation> conversationRepository,
        IUnitOfWork unitOfWork,
        JsonElement serializedStoreState,
        JsonSerializerOptions? jsonSerializerOptions = null)
    {
        _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
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
        // Load conversation for required navigation property (once for all messages in batch)
        var conversation = await _conversationRepository.GetByIdAsync(ConversationId, cancellationToken);
        if (conversation == null)
        {
            throw new InvalidOperationException($"Conversation {ConversationId} not found");
        }

        // Get the latest user message timestamp to ensure proper ordering
        var lastMessageCreatedAt = await _messageRepository.Query()
            .Where(m => m.ConversationId == ConversationId && m.Role == ChatMessageRole.User)
            .OrderByDescending(m => m.CreatedAt)
            .Select(x => (DateTimeOffset?)x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var lastDate = lastMessageCreatedAt ?? DateTimeOffset.UtcNow;

        // Load all existing tool messages and extract their CallIds for duplicate detection
        var existingToolMessages = await _messageRepository.Query()
            .Where(m => m.ConversationId == ConversationId && m.Role == ChatMessageRole.Tool)
            .Select(m => m.SerializedMessage)
            .ToListAsync(cancellationToken);

        var existingCallIds = ExtractCallIdsFromMessages(existingToolMessages);

        foreach (var aiMessage in messages)
        {
            Guid messageId;
            // Use the message ID from the AI message if available, or generate one for tool messages
            if (!string.IsNullOrEmpty(aiMessage.MessageId) && Guid.TryParse(aiMessage.MessageId, out var parsedMessageId))
            {
                messageId = parsedMessageId;
            }
            else
            {
                messageId = Guid.NewGuid();
            }

            if (_messageRepository.Query().Where(x => x.Id == messageId).Any())
            {
                // Message with this ID already exists, skip to avoid duplicates. This is expected when streaming.
                continue;
            }

            var messageRole = MapChatRoleToMessageRole(aiMessage.Role);

            // For tool messages, check if any CallId already exists
            if (messageRole == ChatMessageRole.Tool)
            {
                var callIds = ExtractCallIdsFromMessage(aiMessage);
                if (callIds.Any(callId => existingCallIds.Contains(callId)))
                {
                    // This tool result already exists, skip to avoid duplicates
                    continue;
                }
                
                // Add the new CallIds to the set for subsequent messages in this batch
                foreach (var callId in callIds)
                {
                    existingCallIds.Add(callId);
                }
            }

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
                Id = aiMessage.Role == ChatRole.Tool ? Guid.NewGuid() : messageId, // Tool call messages don't have unique IDs, so generate one
                Conversation = conversation,
                Role = messageRole,
                Text = aiMessage.Text ?? string.Empty,
                CreatedAt = createdAt,
                SerializedMessage = JsonSerializer.Serialize(aiMessage),
                UserId = null // Assistant/system/tool messages don't have a user
            };

            await _messageRepository.Add(chatMessage);
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
               return new ChatMessage(chatRole, msg.Text)
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
        return JsonSerializer.SerializeToElement(ConversationId.ToString("N"));
    }

    /// <summary>
    /// Map Microsoft.Extensions.AI ChatRole to domain ChatMessageRole
    /// </summary>
    public static ChatMessageRole MapChatRoleToMessageRole(ChatRole? role)
    {
        if (role == null)
        {
            return ChatMessageRole.Assistant;
        }

        return role.Value.Value switch
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
    public static ChatRole MapMessageRoleToChatRole(ChatMessageRole messageRole)
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

    /// <summary>
    /// Extract CallIds from a collection of serialized messages
    /// </summary>
    private static HashSet<string> ExtractCallIdsFromMessages(IEnumerable<string?> serializedMessages)
    {
        var callIds = new HashSet<string>(StringComparer.Ordinal);
        
        foreach (var serialized in serializedMessages)
        {
            if (string.IsNullOrEmpty(serialized))
                continue;

            try
            {
                var message = JsonSerializer.Deserialize<ChatMessage>(serialized);
                if (message != null)
                {
                    var messageCallIds = ExtractCallIdsFromMessage(message);
                    foreach (var callId in messageCallIds)
                    {
                        callIds.Add(callId);
                    }
                }
            }
            catch (JsonException)
            {
                // Skip messages that can't be deserialized
            }
        }

        return callIds;
    }

    /// <summary>
    /// Extract CallIds from a single ChatMessage
    /// </summary>
    private static List<string> ExtractCallIdsFromMessage(Microsoft.Extensions.AI.ChatMessage message)
    {
        var callIds = new List<string>();
        
        if (message.Contents != null)
        {
            foreach (var content in message.Contents.OfType<FunctionResultContent>())
            {
                if (!string.IsNullOrEmpty(content.CallId))
                {
                    callIds.Add(content.CallId);
                }
            }
        }

        return callIds;
    }
}
