# Article Assistant - User Stories

**Feature:** Article Assistant (Agentic Article Improvement)  
**Epic:** AI-Powered Article Enhancement with Human Approval  
**Version:** 1.0  
**Date:** December 30, 2024

---

## Story Organization

Stories are organized into **Epics** and ordered by dependencies. Each story includes:
- **ID**: Unique identifier
- **Title**: Brief description
- **As a/I want/So that**: User story format
- **Acceptance Criteria**: Testable requirements
- **Dependencies**: Required prior stories
- **Estimation**: T-shirt size (XS, S, M, L, XL)
- **Notes**: Implementation guidance

---

## Epic 1: Foundation & Data Model

### AS-001: Version Type and Parent Tracking

**As a** system administrator  
**I want** article versions to support type classification and parent relationships  
**So that** we can maintain parallel version chains for user and AI drafts

**Acceptance Criteria:**
- [ ] `ArticleVersion` entity has `VersionType` enum field (User | AI)
- [ ] `ArticleVersion` entity has `ParentVersionId` nullable Guid field
- [ ] `ArticleVersion` entity has `ChangeMessage` string field for commit messages
- [ ] Database migration created and tested
- [ ] Foreign key relationship established for ParentVersionId
- [ ] Indexes created for performance (ParentVersionId, VersionType)

**Dependencies:** None  
**Estimation:** S  
**Notes:** This is pure schema work, no business logic yet

---

### AS-002: Conversation Storage Schema

**As a** developer  
**I want** a database schema for storing article conversations  
**So that** we can persist conversation history between users and the AI assistant

**Acceptance Criteria:**
- [ ] `ArticleConversation` entity created with fields:
  - Id, ArticleId, Status (Active/Complete/Cancelled)
  - CreatedAt, CompletedAt, CompletedByUserId
- [ ] `ConversationMessage` entity created with fields:
  - Id, ConversationId, MessageType (User/Assistant/System/ToolExecution)
  - Content, UserId, CreatedAt, Metadata (JSON)
- [ ] Foreign key relationships established
- [ ] Database migration created and tested
- [ ] Indexes created (ArticleId, ConversationId, CreatedAt)

**Dependencies:** None  
**Estimation:** M  
**Notes:** Conversation message metadata stores tool execution results as JSON

---

### AS-003: Subject and Fragment-Subject Relationship

**As a** developer  
**I want** a many-to-many relationship between fragments and subjects  
**So that** fragments can belong to multiple subjects and vice versa

**Acceptance Criteria:**
- [ ] `Subject` entity created with fields:
  - Id, Name, Description, CreatedByType (System/LLM), CreatedAt
- [ ] `FragmentSubject` join entity created with fields:
  - FragmentId, SubjectId, ConversationId, Confidence, CreatedAt
- [ ] Foreign key relationships established
- [ ] Database migration created and tested
- [ ] Indexes created for efficient querying

**Dependencies:** AS-002 (needs ConversationId)  
**Estimation:** S  
**Notes:** Phase 1 will have 1:1 relationship in practice, but architecture supports many-to-many

---

## Epic 2: Version Management Services

### AS-004: Version Message Auto-Generation

**As a** user  
**I want** every article version to have an auto-generated commit message  
**So that** I can understand what changed in each version

**Acceptance Criteria:**
- [ ] `IVersionMessageGenerator` interface created
- [ ] `VersionMessageGenerator` service implemented with simple template logic
- [ ] Generates messages for user edits: "Edited {sections} ({+/-} words)"
- [ ] Generates messages for AI drafts: "Added {topics} from {N} fragments (+{words} words)"
- [ ] Generates messages for draft acceptance: "Accepted AI draft v{X}: {summary}"
- [ ] Unit tests for message generation
- [ ] Integration test showing messages created on version save

**Dependencies:** AS-001  
**Estimation:** M  
**Notes:** Phase 1 uses simple templates; Phase 2 will add LLM generation

---

### AS-005: AI Draft Version Creation

**As a** system  
**I want** to create AI draft versions linked to user versions  
**So that** AI suggestions don't pollute the official version history

**Acceptance Criteria:**
- [ ] `ArticleVersionService.CreateAIDraftVersionAsync()` method implemented
- [ ] Creates version with VersionType = AI
- [ ] Links to parent user version via ParentVersionId
- [ ] Assigns version number (increments from parent)
- [ ] Auto-generates commit message
- [ ] Returns draft version info
- [ ] Unit tests for version chain creation
- [ ] Integration test showing v3 → v3.1 (AI) chain

**Dependencies:** AS-001, AS-004  
**Estimation:** M  
**Notes:** Includes logic to calculate display version number (v3.1, v3.2)

---

### AS-006: Draft Acceptance and Promotion

**As a** user  
**I want** to accept an AI draft and make it the current article version  
**So that** approved improvements become part of the official article

**Acceptance Criteria:**
- [ ] `ArticleVersionService.AcceptDraftVersionAsync()` method implemented
- [ ] Creates new User version with AI draft content
- [ ] Updates Article.Content to new version content
- [ ] Generates acceptance commit message
- [ ] Marks AI draft chain as inactive/merged
- [ ] Returns new user version info
- [ ] Unit tests for acceptance flow
- [ ] Integration test showing v3.2 (AI) → v4 (User) promotion

**Dependencies:** AS-005  
**Estimation:** M  
**Notes:** This is a critical flow - ensure version integrity

---

### AS-007: Version Chain Retrieval

**As a** developer  
**I want** to retrieve full version chains with parent relationships  
**So that** UI can display version history correctly

**Acceptance Criteria:**
- [ ] `ArticleVersionService.GetVersionChainAsync()` method implemented
- [ ] Returns versions ordered by creation
- [ ] Includes parent relationships
- [ ] Calculates display version numbers (v3, v3.1, v3.2)
- [ ] Filters by VersionType if requested
- [ ] Unit tests for chain retrieval
- [ ] Integration test with complex chain (v1→v2→v3→v3.1→v3.2→v4)

**Dependencies:** AS-005  
**Estimation:** M  
**Notes:** Display logic walks up chain to find User parent for numbering

---

## Epic 3: Conversation Management Services

### AS-008: Conversation Repository

**As a** developer  
**I want** basic CRUD operations for conversations  
**So that** we can persist and retrieve conversation data

**Acceptance Criteria:**
- [ ] `IConversationRepository` interface created
- [ ] `ConversationRepository` implementation with methods:
  - CreateConversationAsync
  - GetConversationAsync
  - GetConversationsByArticleIdAsync
  - AddMessageAsync
  - UpdateConversationStatusAsync
- [ ] Unit tests for all operations
- [ ] Integration tests with real database

**Dependencies:** AS-002  
**Estimation:** M  
**Notes:** Standard repository pattern

---

### AS-009: Conversation State Management

**As a** system  
**I want** to manage conversation states and transitions  
**So that** conversations progress through proper workflow stages

**Acceptance Criteria:**
- [ ] `ConversationStateManager` service implemented
- [ ] Validates state transitions (Active → Complete/Cancelled)
- [ ] Prevents multiple active conversations per article
- [ ] Tracks who completed conversation and when
- [ ] Unit tests for all state transitions
- [ ] Handles edge cases (cancellation, errors)

**Dependencies:** AS-008  
**Estimation:** S  
**Notes:** State machine: No Conversation → Active → Complete/Cancelled

---

### AS-010: Conversation History Retrieval

**As a** user  
**I want** to view past conversations for an article  
**So that** I can see what improvements were made previously

**Acceptance Criteria:**
- [ ] `GetConversationHistoryAsync(articleId)` method implemented
- [ ] Returns conversations ordered by creation date (newest first)
- [ ] Includes message count and summary info
- [ ] Filters to complete/cancelled only (not active)
- [ ] Supports pagination
- [ ] Unit tests for retrieval
- [ ] Returns empty list if no history

**Dependencies:** AS-008  
**Estimation:** S  
**Notes:** Read-only view of past conversations

---

## Epic 4: Fragment-Subject Services

### AS-011: Simple Subject Extraction (Phase 1)

**As a** system  
**I want** to extract subjects from fragments using their summaries  
**So that** users can review and approve content by topic

**Acceptance Criteria:**
- [ ] `IFragmentSubjectService` interface created
- [ ] `ExtractSubjectsAsync(fragments)` method implemented
- [ ] Uses Fragment.Summary as subject name (1:1 mapping)
- [ ] Creates or reuses Subject entities
- [ ] Creates FragmentSubject join records
- [ ] Handles duplicate summaries (same subject for multiple fragments)
- [ ] Unit tests with sample fragments
- [ ] Returns subjects with fragment associations

**Dependencies:** AS-003  
**Estimation:** M  
**Notes:** Phase 1 simple implementation; Phase 2 will add LLM clustering

---

### AS-012: Fragment-Subject Selection Validation

**As a** system  
**I want** to validate fragment and subject selections  
**So that** user selections are consistent with the many-to-many relationship

**Acceptance Criteria:**
- [ ] `ValidateSelectionsAsync(fragmentIds, subjectIds)` method implemented
- [ ] Ensures selected fragments belong to at least one selected subject
- [ ] Ensures selected subjects have at least one selected fragment
- [ ] Returns validation errors if inconsistent
- [ ] Unit tests for valid and invalid selections
- [ ] Handles edge case of no selections

**Dependencies:** AS-011  
**Estimation:** S  
**Notes:** Enforces bidirectional auto-deselection rules

---

## Epic 5: Agent Tools Framework

### AS-013: Agent Tool Interface and Registry

**As a** developer  
**I want** a framework for defining and registering agent tools  
**So that** the AI assistant can call functions to perform actions

**Acceptance Criteria:**
- [ ] `IAgentTool` interface created with:
  - Name, Description, ParametersSchema properties
  - ExecuteAsync method
- [ ] `AgentToolRegistry` service for tool registration
- [ ] `AgentToolExecutor` service for tool invocation
- [ ] Handles tool errors gracefully
- [ ] Unit tests for framework
- [ ] Sample "echo" tool for testing

**Dependencies:** None  
**Estimation:** M  
**Notes:** Foundation for all tool implementations

---

### AS-014: Search Fragments Tool

**As a** AI assistant  
**I want** to search for fragments related to a query  
**So that** I can find relevant content to improve articles

**Acceptance Criteria:**
- [ ] `SearchFragmentsTool` implements IAgentTool
- [ ] Accepts parameters: query, limit (default 10), threshold (default 0.7)
- [ ] Generates embedding for query using existing service
- [ ] Calls FragmentRepository.FindSimilarAsync
- [ ] Returns fragments with similarity scores
- [ ] Includes fragment metadata (title, summary, category, source)
- [ ] Unit tests with mocked repository
- [ ] Integration test with real fragment search

**Dependencies:** AS-013  
**Estimation:** M  
**Notes:** Reuses existing fragment search infrastructure

---

### AS-015: Create Draft Version Tool

**As a** AI assistant  
**I want** to create a draft version with selected fragments  
**So that** users can review improvements before applying

**Acceptance Criteria:**
- [ ] `CreateDraftVersionTool` implements IAgentTool
- [ ] Accepts parameters: fragment_ids[], subject_ids[]
- [ ] Validates selections using FragmentSubjectService
- [ ] Composes content from selected fragments
- [ ] Calls ArticleVersionService.CreateAIDraftVersionAsync
- [ ] Formats fragments with proper attribution
- [ ] Returns version info and summary
- [ ] Unit tests with mocked services
- [ ] Integration test showing full draft creation

**Dependencies:** AS-013, AS-005, AS-012  
**Estimation:** L  
**Notes:** Complex tool - handles content composition and formatting

---

### AS-016: Get Article Content Tool

**As a** AI assistant  
**I want** to retrieve current article content  
**So that** I can understand context when making suggestions

**Acceptance Criteria:**
- [ ] `GetArticleContentTool` implements IAgentTool
- [ ] Accepts parameter: article_id
- [ ] Returns article title, content, summary, type
- [ ] Returns current version number
- [ ] Handles article not found error
- [ ] Unit tests with mocked repository
- [ ] Integration test with real article

**Dependencies:** AS-013  
**Estimation:** S  
**Notes:** Simple read-only tool

---

### AS-017: List Versions Tool

**As a** AI assistant  
**I want** to list article versions with commit messages  
**So that** I can help users navigate version history

**Acceptance Criteria:**
- [ ] `ListVersionsTool` implements IAgentTool
- [ ] Accepts parameter: article_id
- [ ] Returns user versions only (not AI drafts)
- [ ] Includes version number, commit message, author, date
- [ ] Orders by version number descending (newest first)
- [ ] Unit tests with mocked service
- [ ] Integration test showing version list

**Dependencies:** AS-013, AS-007  
**Estimation:** S  
**Notes:** Helps user understand article evolution

---

## Epic 6: Agentic Loop Orchestration

### AS-018: Agent Service Foundation

**As a** developer  
**I want** a service to orchestrate the agentic loop  
**So that** the AI assistant can reason and take actions

**Acceptance Criteria:**
- [ ] `IArticleAssistantService` interface created
- [ ] `ArticleAssistantService` basic implementation
- [ ] Manages conversation context
- [ ] Builds prompts with tool definitions
- [ ] Calls AI service (BedrockAiService)
- [ ] Handles AI responses
- [ ] Unit tests with mocked AI service
- [ ] No tool calling yet (just conversation)

**Dependencies:** AS-008, AS-013  
**Estimation:** L  
**Notes:** Foundation for agentic behavior

---

### AS-019: Tool Calling Loop Implementation

**As a** system  
**I want** the agent to detect and execute tool calls  
**So that** the AI can perform actions on behalf of users

**Acceptance Criteria:**
- [ ] Detects when AI wants to call a tool (parses response)
- [ ] Executes tool via AgentToolExecutor
- [ ] Adds tool result to conversation context
- [ ] Continues loop until AI has final response
- [ ] Implements max iteration limit (10) to prevent infinite loops
- [ ] Saves tool executions as conversation messages
- [ ] Unit tests for loop behavior
- [ ] Integration test showing multi-tool workflow

**Dependencies:** AS-018  
**Estimation:** L  
**Notes:** Core agentic behavior - critical implementation

---

### AS-020: Process User Message API

**As a** user  
**I want** to send a message to the assistant  
**So that** I can request article improvements

**Acceptance Criteria:**
- [ ] `ProcessUserMessageAsync(conversationId, message, userId)` method
- [ ] Validates conversation exists and is active
- [ ] Saves user message to conversation
- [ ] Triggers agentic loop
- [ ] Returns immediately (async processing)
- [ ] Queues Hangfire job for AI processing
- [ ] Unit tests for API method
- [ ] Integration test showing message handling

**Dependencies:** AS-019  
**Estimation:** M  
**Notes:** Entry point for user interactions

---

## Epic 7: Background Job Processing

### AS-021: Agent Conversation Hangfire Job

**As a** system  
**I want** agent processing to run in background  
**So that** users aren't blocked waiting for AI responses

**Acceptance Criteria:**
- [ ] `AgentConversationJob` Hangfire job created
- [ ] Accepts conversationId parameter
- [ ] Loads conversation context
- [ ] Calls ArticleAssistantService.ProcessUserMessageAsync
- [ ] Handles errors gracefully
- [ ] Updates conversation status on completion/error
- [ ] Job registered in Hangfire configuration
- [ ] Unit tests with mocked services
- [ ] Integration test showing job execution

**Dependencies:** AS-020  
**Estimation:** M  
**Notes:** Enables non-blocking AI processing

---

### AS-022: Job Cancellation Support

**As a** user  
**I want** to cancel an in-progress AI operation  
**So that** I can stop work if I change my mind

**Acceptance Criteria:**
- [ ] `CancelConversationAsync(conversationId)` method implemented
- [ ] Cancels associated Hangfire job
- [ ] Updates conversation status to Cancelled
- [ ] Conversation can be resumed later
- [ ] Unit tests for cancellation
- [ ] Integration test showing cancel → resume flow

**Dependencies:** AS-021  
**Estimation:** S  
**Notes:** Uses Hangfire's CancellationToken support

---

## Epic 8: SignalR Real-Time Updates

### AS-023: Agent Status Notifications

**As a** user  
**I want** to see real-time updates when the AI is working  
**So that** I know progress is being made

**Acceptance Criteria:**
- [ ] ArticleHub extended with AgentStatus event
- [ ] Broadcasts status to article group: "Searching fragments...", "Creating draft..."
- [ ] Includes timestamp and status type
- [ ] Sent at key points in agentic loop
- [ ] Unit tests for hub methods
- [ ] Integration test showing SignalR broadcast

**Dependencies:** AS-019  
**Estimation:** S  
**Notes:** Reuses existing ArticleHub infrastructure

---

### AS-024: Tool Execution Notifications

**As a** user  
**I want** to see what tools the AI is using  
**So that** I understand what actions are being taken

**Acceptance Criteria:**
- [ ] ArticleHub extended with AgentToolExecution event
- [ ] Broadcasts tool name, success/failure, summary
- [ ] Includes execution time
- [ ] Sent after each tool execution
- [ ] Unit tests for notifications
- [ ] Integration test showing multiple tool executions

**Dependencies:** AS-023  
**Estimation:** S  
**Notes:** Provides transparency into agent actions

---

### AS-025: Conversation Message Sync

**As a** user  
**I want** to see new messages in real-time when collaborating  
**So that** I can work with teammates on article improvements

**Acceptance Criteria:**
- [ ] ArticleHub extended with ConversationMessage event
- [ ] Broadcasts new messages to all users viewing article
- [ ] Includes message content, user info, timestamp
- [ ] Supports user and assistant messages
- [ ] Unit tests for message sync
- [ ] Integration test with multiple connected clients

**Dependencies:** AS-023  
**Estimation:** M  
**Notes:** Enables multi-user collaboration

---

## Epic 9: UI Components - Assistant Panel

### AS-026: Assistant Panel Component Skeleton

**As a** developer  
**I want** a Vue component for the assistant panel  
**So that** users can interact with the AI assistant

**Acceptance Criteria:**
- [ ] `assistant-panel.js` Vue component created
- [ ] Props: articleId
- [ ] Data: messages, loading, conversationId
- [ ] Template with message area and input
- [ ] Basic styling
- [ ] Component registered in articles page
- [ ] Replaces existing ChatPanel component

**Dependencies:** None (UI foundation)  
**Estimation:** M  
**Notes:** Starting point for assistant UI

---

### AS-027: Send Message Functionality

**As a** user  
**I want** to type and send messages to the assistant  
**So that** I can request article improvements

**Acceptance Criteria:**
- [ ] Input field for message text
- [ ] Send button (disabled when empty or loading)
- [ ] Calls API: POST /api/articles/{id}/assistant/messages
- [ ] Optimistically adds user message to UI
- [ ] Disables input while AI is working
- [ ] Shows loading indicator
- [ ] Handles API errors gracefully
- [ ] Enter key sends message

**Dependencies:** AS-026  
**Estimation:** S  
**Notes:** Basic message sending flow

---

### AS-028: Message Display with Attribution

**As a** user  
**I want** to see conversation messages with names  
**So that** I know who said what

**Acceptance Criteria:**
- [ ] Messages display with minimal attribution (name only)
- [ ] User messages show user name
- [ ] Assistant messages show "Assistant"
- [ ] Messages ordered chronologically
- [ ] Auto-scroll to latest message
- [ ] Distinguishes user vs assistant visually
- [ ] Handles long messages with word wrap

**Dependencies:** AS-027  
**Estimation:** S  
**Notes:** Clean, simple message display

---

### AS-029: Tool Execution Progress Display

**As a** user  
**I want** to see when the AI is using tools  
**So that** I understand what's happening behind the scenes

**Acceptance Criteria:**
- [ ] Tool execution messages displayed inline
- [ ] Shows tool name and status (working/complete)
- [ ] Displays checkmarks for completed tools
- [ ] Shows timing information
- [ ] Updates in real-time via SignalR
- [ ] Collapsed/minimal view (not overwhelming)

**Dependencies:** AS-028, AS-024  
**Estimation:** M  
**Notes:** Phase 1 shows everything; Phase 2 can add collapsible detail

---

### AS-030: History Button and Modal

**As a** user  
**I want** to view past conversations for an article  
**So that** I can reference previous improvements

**Acceptance Criteria:**
- [ ] [📜 History (N)] button in panel header
- [ ] Shows count of past conversations
- [ ] Opens modal/drawer with conversation list
- [ ] Each item shows date, summary, message count
- [ ] Click item shows full conversation (read-only)
- [ ] Close button to dismiss
- [ ] Handles no history gracefully

**Dependencies:** AS-028, AS-010  
**Estimation:** M  
**Notes:** Read-only historical view

---

### AS-031: New Conversation Button

**As a** user  
**I want** to start a new conversation after completing one  
**So that** I can make additional improvements separately

**Acceptance Criteria:**
- [ ] [+ New Conversation] button shown when conversation complete
- [ ] Disabled when conversation active
- [ ] Confirms before clearing current conversation
- [ ] Creates new conversation via API
- [ ] Clears message area
- [ ] Resets UI state
- [ ] Handles errors gracefully

**Dependencies:** AS-028  
**Estimation:** S  
**Notes:** Allows fresh start after approval

---

### AS-032: Cancel Button

**As a** user  
**I want** to cancel an in-progress AI operation  
**So that** I can stop unwanted work

**Acceptance Criteria:**
- [ ] [🗑️ Cancel] button shown when AI is working
- [ ] Calls cancellation API
- [ ] Updates UI immediately
- [ ] Shows "Cancelled" status
- [ ] User can send new message to resume
- [ ] Handles cancellation errors

**Dependencies:** AS-028, AS-022  
**Estimation:** S  
**Notes:** Gives user control over long operations

---

## Epic 10: UI Components - Review Pane

### AS-033: Review Pane Component

**As a** developer  
**I want** a review pane component for fragment/subject approval  
**So that** users can select what content to include

**Acceptance Criteria:**
- [ ] `review-pane.js` Vue component created
- [ ] Two-column layout (Fragments | Subjects)
- [ ] Props: fragments, subjects, selections
- [ ] Emits selection-changed events
- [ ] Basic styling for desktop
- [ ] Mobile: stacked vertical layout
- [ ] Component can be shown as tab in center pane

**Dependencies:** None (UI component)  
**Estimation:** L  
**Notes:** Complex UI component with bidirectional selection logic

---

### AS-034: Fragment Selection List

**As a** user  
**I want** to see and select fragments  
**So that** I can choose which content to include

**Acceptance Criteria:**
- [ ] Displays list of fragments with checkboxes
- [ ] Shows fragment title, summary, match %
- [ ] Shows associated subjects as tags
- [ ] All fragments checked by default
- [ ] Click checkbox toggles selection
- [ ] [Preview] button on each fragment
- [ ] Updates subject selections based on rules
- [ ] Shows selection count at bottom

**Dependencies:** AS-033  
**Estimation:** M  
**Notes:** Left column of review pane

---

### AS-035: Subject Selection List

**As a** user  
**I want** to see and select subjects  
**So that** I can choose topics to include

**Acceptance Criteria:**
- [ ] Displays list of subjects with checkboxes
- [ ] Shows subject name and fragment count
- [ ] Shows associated fragment IDs as tags
- [ ] All subjects checked by default
- [ ] Click checkbox toggles selection
- [ ] Updates fragment selections based on rules
- [ ] Shows selection count at bottom

**Dependencies:** AS-033  
**Estimation:** M  
**Notes:** Right column of review pane

---

### AS-036: Bidirectional Auto-Deselection Logic

**As a** user  
**I want** selections to stay consistent  
**So that** I don't select orphaned fragments or subjects

**Acceptance Criteria:**
- [ ] Unchecking fragment: if all subjects deselected → auto-deselect fragment
- [ ] Unchecking subject: if all fragments deselected → auto-deselect subject
- [ ] Works in both directions simultaneously
- [ ] Provides visual feedback during auto-deselection
- [ ] Unit tests for selection logic
- [ ] Integration test with complex selections

**Dependencies:** AS-034, AS-035  
**Estimation:** M  
**Notes:** Critical UX feature - must feel natural

---

### AS-037: Fragment Preview Modal

**As a** user  
**I want** to preview fragment content  
**So that** I can decide if it's relevant

**Acceptance Criteria:**
- [ ] Click [Preview] opens modal
- [ ] Shows full fragment content (rendered markdown)
- [ ] Shows source metadata (date, type, source name)
- [ ] [Close] button to dismiss
- [ ] Modal doesn't change selections
- [ ] Reuses existing Sources preview pattern
- [ ] Works on mobile

**Dependencies:** AS-034  
**Estimation:** S  
**Notes:** Reuse existing modal component from Sources

---

### AS-038: Create Draft from Review Pane

**As a** user  
**I want** to create an AI draft after approving selections  
**So that** the assistant can generate improvements

**Acceptance Criteria:**
- [ ] [✓ Create Draft] button at bottom
- [ ] Disabled if no selections
- [ ] Shows loading state when clicked
- [ ] Calls API with selected fragment/subject IDs
- [ ] Closes review pane on success
- [ ] Shows error message on failure
- [ ] Returns to assistant conversation view

**Dependencies:** AS-036  
**Estimation:** M  
**Notes:** Completes the approval workflow

---

## Epic 11: UI Components - Center Pane Tabs

### AS-039: Dynamic Tab Management

**As a** developer  
**I want** center pane to support dynamic tabs  
**So that** we can show current version, AI draft, and review pane

**Acceptance Criteria:**
- [ ] Tab component supports adding/removing tabs dynamically
- [ ] Default: single "Content" tab with editor
- [ ] Can add "AI Draft" tab when draft exists
- [ ] Can add "Review Selections" tab when needed
- [ ] Active tab persists during session
- [ ] Tabs show appropriate icons
- [ ] Mobile: tabs still accessible

**Dependencies:** None (UI foundation)  
**Estimation:** M  
**Notes:** Foundation for multi-tab center pane

---

### AS-040: AI Draft Tab with Diff View

**As a** user  
**I want** to see AI draft changes highlighted  
**So that** I can review what will be added/modified

**Acceptance Criteria:**
- [ ] "✨ AI Draft (v3.X)" tab appears when draft exists
- [ ] Shows draft content with diff highlighting
- [ ] Green highlighting for additions
- [ ] Red strikethrough for deletions
- [ ] Reuses existing version comparison rendering
- [ ] [Accept Draft] button shown
- [ ] [Discard Draft] button shown
- [ ] Tab badge shows word count change

**Dependencies:** AS-039, AS-006  
**Estimation:** L  
**Notes:** Reuses existing version diff rendering

---

### AS-041: Current Version Lock During Draft

**As a** user  
**I want** the current version to be read-only when AI draft exists  
**So that** changes don't conflict with pending draft

**Acceptance Criteria:**
- [ ] "Current Version" tab shows when draft exists
- [ ] Editor is read-only (disabled)
- [ ] Visual indication of locked state
- [ ] Tooltip explains why locked
- [ ] Edit warning shown if user clicks editor
- [ ] Returns to editable after draft accepted/discarded

**Dependencies:** AS-040  
**Estimation:** S  
**Notes:** Prevents editing conflicts

---

### AS-042: Accept Draft Action

**As a** user  
**I want** to accept an AI draft and apply it to my article  
**So that** improvements become part of the official article

**Acceptance Criteria:**
- [ ] [Accept Draft] button in AI Draft tab
- [ ] Confirmation dialog before accepting
- [ ] Calls accept API endpoint
- [ ] Shows loading during processing
- [ ] Updates article content on success
- [ ] Removes AI Draft tab
- [ ] Returns to single editable Content tab
- [ ] Updates version history
- [ ] Shows success message

**Dependencies:** AS-040, AS-006  
**Estimation:** M  
**Notes:** Critical user action - must be clear and safe

---

### AS-043: Discard Draft Action

**As a** user  
**I want** to discard an AI draft if I don't like it  
**So that** I can reject suggestions

**Acceptance Criteria:**
- [ ] [Discard Draft] button in AI Draft tab
- [ ] Confirmation dialog before discarding
- [ ] Marks AI draft chain as discarded
- [ ] Removes AI Draft tab
- [ ] Returns to single editable Content tab
- [ ] Conversation stays in history
- [ ] Can start new conversation

**Dependencies:** AS-040  
**Estimation:** S  
**Notes:** Allows rejecting suggestions

---

## Epic 12: UI Components - Tree View Integration

### AS-044: Conversation Status Icons in Tree

**As a** user  
**I want** to see conversation status icons next to articles  
**So that** I know which articles have active assistant work

**Acceptance Criteria:**
- [ ] Icon appears next to article title in tree view
- [ ] 🔄 icon when AI is working
- [ ] 👤 icon when waiting for user input
- [ ] ✅ icon when conversation complete
- [ ] No icon when no active conversation
- [ ] Icon priority: AI Working > Waiting > Approved
- [ ] Icon updates in real-time via SignalR
- [ ] Tooltip explains icon meaning

**Dependencies:** AS-025  
**Estimation:** M  
**Notes:** Provides at-a-glance status

---

### AS-045: Status Icon Real-Time Updates

**As a** system  
**I want** status icons to update across all users  
**So that** everyone sees current conversation state

**Acceptance Criteria:**
- [ ] SignalR event triggers icon update
- [ ] Updates happen without page refresh
- [ ] All connected users see same icon
- [ ] Icon changes animate smoothly
- [ ] Handles multiple articles with conversations
- [ ] Performance: doesn't slow down tree rendering

**Dependencies:** AS-044, AS-025  
**Estimation:** S  
**Notes:** Multi-user awareness

---

## Epic 13: API Endpoints

### AS-046: Send Message API Endpoint

**As a** user  
**I want** an API to send messages to the assistant  
**So that** the UI can trigger conversations

**Acceptance Criteria:**
- [ ] POST /api/articles/{id}/assistant/messages endpoint
- [ ] Accepts: { message: string }
- [ ] Creates conversation if first message
- [ ] Validates article exists and user has access
- [ ] Saves user message
- [ ] Queues Hangfire job
- [ ] Returns: { conversationId, messageId }
- [ ] Returns immediately (async processing)
- [ ] Integration tests for endpoint

**Dependencies:** AS-020, AS-021  
**Estimation:** M  
**Notes:** Primary user interaction endpoint

---

### AS-047: Get Conversation API Endpoint

**As a** user  
**I want** an API to retrieve conversation messages  
**So that** the UI can display conversation history

**Acceptance Criteria:**
- [ ] GET /api/articles/{id}/assistant/conversation endpoint
- [ ] Returns active conversation with all messages
- [ ] Returns 404 if no active conversation
- [ ] Includes message metadata (tool executions)
- [ ] Orders messages chronologically
- [ ] Supports pagination for long conversations
- [ ] Integration tests

**Dependencies:** AS-008  
**Estimation:** S  
**Notes:** Read conversation data

---

### AS-048: Get History API Endpoint

**As a** user  
**I want** an API to retrieve past conversations  
**So that** the UI can show conversation history

**Acceptance Criteria:**
- [ ] GET /api/articles/{id}/assistant/history endpoint
- [ ] Returns list of complete/cancelled conversations
- [ ] Each item includes summary info
- [ ] Orders by completion date (newest first)
- [ ] Supports pagination
- [ ] Returns empty array if no history
- [ ] Integration tests

**Dependencies:** AS-010  
**Estimation:** S  
**Notes:** Historical conversations

---

### AS-049: Approve Selections API Endpoint

**As a** user  
**I want** an API to submit approved fragment/subject selections  
**So that** the assistant can create a draft

**Acceptance Criteria:**
- [ ] POST /api/articles/{id}/assistant/approve endpoint
- [ ] Accepts: { conversationId, fragmentIds[], subjectIds[] }
- [ ] Validates selections using FragmentSubjectService
- [ ] Queues Hangfire job to create draft
- [ ] Returns immediately
- [ ] Handles validation errors
- [ ] Integration tests

**Dependencies:** AS-012, AS-015  
**Estimation:** M  
**Notes:** Approval workflow endpoint

---

### AS-050: Cancel Conversation API Endpoint

**As a** user  
**I want** an API to cancel in-progress conversations  
**So that** the UI can stop AI work

**Acceptance Criteria:**
- [ ] POST /api/articles/{id}/assistant/cancel endpoint
- [ ] Accepts: { conversationId }
- [ ] Cancels Hangfire job
- [ ] Updates conversation status
- [ ] Returns success/error
- [ ] Integration tests

**Dependencies:** AS-022  
**Estimation:** S  
**Notes:** Cancellation control

---

### AS-051: Accept Draft API Endpoint

**As a** user  
**I want** an API to accept AI drafts  
**So that** the UI can apply approved changes

**Acceptance Criteria:**
- [ ] POST /api/articles/{id}/versions/{versionId}/accept endpoint
- [ ] Validates draft version exists and is AI type
- [ ] Validates user has permission
- [ ] Calls AcceptDraftVersionAsync service
- [ ] Returns new user version info
- [ ] Broadcasts update via SignalR
- [ ] Integration tests

**Dependencies:** AS-006  
**Estimation:** M  
**Notes:** Critical workflow endpoint

---

### AS-052: New Conversation API Endpoint

**As a** user  
**I want** an API to start a new conversation  
**So that** I can begin fresh after completing one

**Acceptance Criteria:**
- [ ] POST /api/articles/{id}/assistant/new endpoint
- [ ] Validates no active conversation exists
- [ ] Creates new conversation record
- [ ] Returns conversationId
- [ ] Integration tests

**Dependencies:** AS-009  
**Estimation:** S  
**Notes:** Allows starting fresh

---

## Epic 14: Integration & Polish

### AS-053: Edit Warning When Draft Exists

**As a** user  
**I want** a warning if I try to edit while AI draft exists  
**So that** I don't create conflicts

**Acceptance Criteria:**
- [ ] Detects when user clicks in editor with active draft
- [ ] Shows modal warning dialog
- [ ] Explains situation clearly
- [ ] Offers: [View Draft] [Edit Anyway] [Cancel]
- [ ] Warning shows for all users (multi-user aware)
- [ ] Preference saved if user chooses "Don't show again"

**Dependencies:** AS-041  
**Estimation:** S  
**Notes:** Soft warning, not blocking

---

### AS-054: Empty State Messaging

**As a** user  
**I want** helpful empty states throughout the UI  
**So that** I know what to do when there's no data

**Acceptance Criteria:**
- [ ] Assistant panel: "Type a message..." placeholder
- [ ] History modal: "No past conversations" message
- [ ] Review pane: Should never be empty (comes from search)
- [ ] All empty states have consistent design
- [ ] Icons and text are friendly

**Dependencies:** Various UI components  
**Estimation:** S  
**Notes:** UX polish

---

### AS-055: Error Message Consistency

**As a** user  
**I want** friendly error messages  
**So that** I understand what went wrong without technical jargon

**Acceptance Criteria:**
- [ ] All error messages use user-friendly language
- [ ] No fragments found: helpful suggestions
- [ ] Network errors: "Please try again"
- [ ] System errors: "Contact support if persists"
- [ ] Errors logged server-side with full details
- [ ] Error messages consistent across UI

**Dependencies:** All components  
**Estimation:** S  
**Notes:** Final polish for user experience

---

### AS-056: End-to-End Integration Testing

**As a** QA engineer  
**I want** comprehensive integration tests  
**So that** the complete workflow is verified

**Acceptance Criteria:**
- [ ] Test: User sends message → AI searches → presents approval → creates draft → user accepts
- [ ] Test: Multi-user collaboration (two users, same article)
- [ ] Test: Cancellation and resumption
- [ ] Test: No fragments found scenario
- [ ] Test: Version chain integrity after acceptance
- [ ] Test: SignalR updates across clients
- [ ] All tests pass consistently

**Dependencies:** All prior stories  
**Estimation:** L  
**Notes:** Critical for release confidence

---

### AS-057: Performance Testing and Optimization

**As a** system administrator  
**I want** the assistant feature to perform well under load  
**So that** many users can use it simultaneously

**Acceptance Criteria:**
- [ ] Load test: 50 concurrent conversations
- [ ] Response time: UI updates within 200ms
- [ ] Search performance: <500ms for typical queries
- [ ] Draft creation: <2s for typical case
- [ ] Database queries optimized with indexes
- [ ] SignalR scales to 100+ concurrent users
- [ ] No memory leaks in long-running jobs

**Dependencies:** AS-056  
**Estimation:** M  
**Notes:** Performance validation

---

### AS-058: Documentation and User Guide

**As a** user  
**I want** documentation on how to use the assistant  
**So that** I can get the most value from the feature

**Acceptance Criteria:**
- [ ] User guide document created
- [ ] Screenshots of key workflows
- [ ] FAQ section with common questions
- [ ] Tips for effective prompts
- [ ] Troubleshooting section
- [ ] Published to help center or docs site

**Dependencies:** AS-056  
**Estimation:** M  
**Notes:** User-facing documentation

---

## Story Summary by Epic

### Epic 1: Foundation & Data Model (3 stories)
- AS-001 through AS-003
- **Estimated effort:** 2 sprints
- **Dependencies:** None (can start immediately)

### Epic 2: Version Management Services (4 stories)
- AS-004 through AS-007
- **Estimated effort:** 2 sprints
- **Dependencies:** Epic 1

### Epic 3: Conversation Management Services (3 stories)
- AS-008 through AS-010
- **Estimated effort:** 1.5 sprints
- **Dependencies:** Epic 1

### Epic 4: Fragment-Subject Services (2 stories)
- AS-011 through AS-012
- **Estimated effort:** 1 sprint
- **Dependencies:** Epic 1

### Epic 5: Agent Tools Framework (5 stories)
- AS-013 through AS-017
- **Estimated effort:** 2.5 sprints
- **Dependencies:** Epics 1, 2, 4

### Epic 6: Agentic Loop Orchestration (3 stories)
- AS-018 through AS-020
- **Estimated effort:** 2 sprints
- **Dependencies:** Epics 3, 5

### Epic 7: Background Job Processing (2 stories)
- AS-021 through AS-022
- **Estimated effort:** 1 sprint
- **Dependencies:** Epic 6

### Epic 8: SignalR Real-Time Updates (3 stories)
- AS-023 through AS-025
- **Estimated effort:** 1 sprint
- **Dependencies:** Epic 6

### Epic 9: UI Components - Assistant Panel (7 stories)
- AS-026 through AS-032
- **Estimated effort:** 2.5 sprints
- **Dependencies:** Epics 7, 8

### Epic 10: UI Components - Review Pane (6 stories)
- AS-033 through AS-038
- **Estimated effort:** 2.5 sprints
- **Dependencies:** Epic 4

### Epic 11: UI Components - Center Pane Tabs (5 stories)
- AS-039 through AS-043
- **Estimated effort:** 2 sprints
- **Dependencies:** Epic 2

### Epic 12: UI Components - Tree View Integration (2 stories)
- AS-044 through AS-045
- **Estimated effort:** 0.5 sprint
- **Dependencies:** Epic 8

### Epic 13: API Endpoints (7 stories)
- AS-046 through AS-052
- **Estimated effort:** 2 sprints
- **Dependencies:** Epics 2, 3, 4, 6

### Epic 14: Integration & Polish (6 stories)
- AS-053 through AS-058
- **Estimated effort:** 2 sprints
- **Dependencies:** All prior epics

---

## Total Estimation

**Total Stories:** 58  
**Estimated Total Effort:** ~23-25 sprints (assuming 2-week sprints)  
**Estimated Timeline:** 12-14 months for complete Phase 1 implementation

**Critical Path:** Epic 1 → Epic 2 → Epic 5 → Epic 6 → Epic 7 → Epic 9 → Epic 13 → Epic 14

**Parallel Work Opportunities:**
- Epics 3 and 4 can run parallel to Epic 2
- Epics 10, 11, 12 can run parallel to Epic 9
- Testing (Epic 14) can start before all features complete

---

## Recommended Sprint Planning

### Sprints 1-2: Foundation
- Epic 1: Foundation & Data Model
- Set up development environment

### Sprints 3-4: Core Services
- Epic 2: Version Management Services
- Epic 3: Conversation Management (parallel)

### Sprints 5-6: Tools Framework
- Epic 4: Fragment-Subject Services
- Epic 5: Agent Tools Framework

### Sprints 7-8: Agentic Core
- Epic 6: Agentic Loop Orchestration
- Epic 7: Background Jobs

### Sprints 9-10: Real-Time & Basic UI
- Epic 8: SignalR Updates
- Epic 9: Assistant Panel (start)

### Sprints 11-12: Complete UI
- Epic 9: Assistant Panel (finish)
- Epic 10: Review Pane

### Sprints 13-14: Tabs & Integration
- Epic 11: Center Pane Tabs
- Epic 12: Tree View Icons
- Epic 13: API Endpoints (start)

### Sprints 15-16: APIs & Polish
- Epic 13: API Endpoints (finish)
- Epic 14: Integration & Polish (start)

### Sprints 17-18: Testing & Release
- Epic 14: Integration & Polish (finish)
- Final testing and documentation
- Release preparation

---

## Notes for Implementation

### Testing Strategy
- **Unit tests:** Required for all services and complex logic
- **Integration tests:** Required for all API endpoints and workflows
- **E2E tests:** Required for critical user paths
- **Performance tests:** Required before release

### Code Review Requirements
- All stories require code review before merging
- UI components require design review
- API endpoints require security review
- Database changes require DBA review

### Definition of Done
- [ ] Code complete and tested
- [ ] Unit tests passing (>80% coverage)
- [ ] Integration tests passing
- [ ] Code reviewed and approved
- [ ] Documentation updated
- [ ] No critical bugs
- [ ] Deployed to staging environment
- [ ] Product owner acceptance

---

**Document Version:** 1.0  
**Last Updated:** December 30, 2024

