# Article Assistant - Product Requirements Document (PRD)

**Feature Name:** Article Assistant (Agentic Article Improvement)  
**Author:** Medley Development Team  
**Date:** December 30, 2025  
**Status:** Design Complete - Ready for Implementation  
**Epic:** Epic 6 - Intelligent Article Improvement  

---

## Executive Summary

The **Article Assistant** transforms Medley from a passive knowledge repository into an active intelligence partner that helps users enhance their articles through conversational AI collaboration. By combining semantic fragment search, intelligent subject clustering, and agentic tool calling, the Assistant enables users to rapidly improve documentation quality while maintaining full human control over all changes.

### Key Innovation

Unlike traditional "chat with AI" features, the Article Assistant is a true agent with tools that can:
- **Search** relevant fragments across the knowledge base
- **Analyze** content gaps and opportunities
- **Create** draft versions with tracked changes
- **Present** findings for human approval before any modifications

This human-in-the-loop design ensures trust, transparency, and quality while dramatically reducing the time needed to create comprehensive documentation.

---

## Goals and Success Metrics

### Primary Goals

1. **Reduce article enhancement time by 60%** - Enable users to find and incorporate relevant knowledge in minutes instead of hours
2. **Increase knowledge reuse by 80%** - Surface buried insights from fragments that would otherwise remain undiscovered
3. **Maintain 100% human control** - Ensure all AI suggestions require explicit approval before application
4. **Enable iterative refinement** - Support multi-turn conversations to progressively improve articles
5. **Build user trust through transparency** - Show all tool executions and reasoning in real-time

### Success Metrics (90 days post-launch)

- **Adoption:** 70% of active users try Article Assistant at least once
- **Engagement:** 40% of articles created include at least one Assistant-enhanced version
- **Efficiency:** Average time to enhance article with fragments reduced from 45 minutes to 15 minutes
- **Quality:** 85% of Assistant-created drafts are approved without modifications
- **Satisfaction:** Net Promoter Score (NPS) of 50+ for the Assistant feature

---

## User Problem Statement

### Current Pain Points

**Problem 1: Hidden Knowledge**
- Users know relevant information exists in fragments but spend 20-30 minutes searching manually
- Keyword search misses semantically related content
- No way to discover what knowledge exists on a topic

**Problem 2: Manual Fragment Integration**
- Copy-pasting fragments into articles is tedious and error-prone
- Attribution to sources is lost or inconsistently applied
- No systematic way to review and select multiple fragments

**Problem 3: Article Improvement Friction**
- Users don't know what's missing from their articles
- Incremental improvements require full context review each time
- No version history showing what changed and why

**Problem 4: Collaboration Overhead**
- Multiple people improving the same article creates conflicts
- No shared context about enhancement efforts
- Manual coordination required between team members

### Solution Overview

Article Assistant solves these problems through:
1. **Semantic Search** - Find relevant fragments by meaning, not keywords
2. **Approval Workflow** - Review and select fragments/subjects before integration
3. **Draft Versioning** - Create parallel AI draft chains that don't affect main content
4. **Conversation Context** - Multi-turn dialogue with full history and shared team visibility
5. **Transparent Operations** - Show all tool calls and reasoning in real-time

---

## Requirements

### Functional Requirements - Phase 1 (MVP)

#### FR-AA001: Article Assistant Tab
- **Priority:** P0 (Critical)
- **Description:** Add "Assistant" tab to right sidebar alongside "Versions" tab
- **Acceptance Criteria:**
  - Tab labeled "Assistant" with 💬 icon
  - Tab persists across article navigation
  - Empty state shows minimal input field with "Type a message..." placeholder
  - No example prompts or heavy onboarding (learn by doing)

#### FR-AA002: Per-Article Conversations
- **Priority:** P0 (Critical)
- **Description:** Each article maintains its own independent conversation thread
- **Acceptance Criteria:**
  - Switching articles switches to that article's conversation
  - Conversations persist forever (no expiry in Phase 1)
  - Conversation state independent across articles
  - No limit on conversation length

#### FR-AA003: Conversation Message Display
- **Priority:** P0 (Critical)
- **Description:** Display conversation messages with minimal attribution
- **Acceptance Criteria:**
  - Show user name above message
  - Show "Assistant" for AI messages
  - No avatars or complex UI (Phase 1)
  - Timestamp on hover
  - Auto-scroll to bottom on new message

#### FR-AA004: Conversation History Access
- **Priority:** P1 (High)
- **Description:** Users can access past completed conversations
- **Acceptance Criteria:**
  - [📜 History] button in Assistant panel header
  - Opens list of past conversations with dates
  - Past conversations are read-only
  - Can view but not edit or continue old conversations

#### FR-AA005: Tool - Search Fragments
- **Priority:** P0 (Critical)
- **Description:** Agent can search for semantically related fragments
- **Acceptance Criteria:**
  - `search_fragments(query, limit, threshold)` tool available
  - Uses embedding similarity via existing fragment repository
  - Returns fragments with similarity scores
  - Respects threshold parameter (default 0.7)
  - Shows search progress in conversation

#### FR-AA006: Tool - Get Article Content
- **Priority:** P0 (Critical)
- **Description:** Agent can read current article content for context
- **Acceptance Criteria:**
  - `get_article_content(article_id)` tool available
  - Returns current article title and content
  - Used for context in decision making
  - Read-only access

#### FR-AA007: Tool - Create Draft Version
- **Priority:** P0 (Critical)
- **Description:** Agent can create AI draft version with selected content
- **Acceptance Criteria:**
  - `create_draft_version(fragment_ids, subject_ids)` tool available
  - Creates new AI version with VersionType = AI
  - Sets ParentVersionId to current user version
  - Generates version commit message automatically
  - Returns version ID and summary

#### FR-AA008: Tool - List Versions
- **Priority:** P1 (High)
- **Description:** Agent can list article version history
- **Acceptance Criteria:**
  - `list_versions(article_id)` tool available
  - Returns user versions only (not AI drafts)
  - Includes version number, date, author, commit message
  - Shows current version indicator

#### FR-AA009: Subject Extraction (Phase 1)
- **Priority:** P0 (Critical)
- **Description:** Extract subjects from found fragments for approval workflow
- **Acceptance Criteria:**
  - Phase 1: Use Fragment.Summary as subject (1:1 mapping)
  - Each fragment maps to exactly one subject initially
  - Deduplication when summaries match
  - Architecture supports many-to-many for Phase 2

#### FR-AA010: Fragment & Subject Approval UI
- **Priority:** P0 (Critical)
- **Description:** Present fragments and subjects for user approval before draft creation
- **Acceptance Criteria:**
  - Assistant message shows: "X fragments | Y subjects found"
  - [✓ Approve All] button applies all immediately
  - [📋 Review] button opens review pane in middle panel
  - Review pane shows two-column layout: Fragments | Subjects
  - All items selected by default (opt-out model)
  - Independent checkboxes for fragments and subjects

#### FR-AA011: Many-to-Many Selection Logic
- **Priority:** P0 (Critical)
- **Description:** Handle bidirectional relationship between fragments and subjects
- **Acceptance Criteria:**
  - Fragments can belong to multiple subjects (Phase 2 ready)
  - Subjects can contain multiple fragments
  - Unchecking fragment removes it from all subjects
  - If fragment has no selected subjects left, auto-uncheck fragment
  - Unchecking subject removes it from all fragments
  - If subject has no selected fragments left, auto-uncheck subject
  - Live update of counts as selections change

#### FR-AA012: Review Pane Layout
- **Priority:** P0 (Critical)
- **Description:** Dedicated review interface for fragment/subject approval
- **Acceptance Criteria:**
  - Opens as new tab in middle pane: "📋 Review Selections"
  - Two-column layout: Fragments (left) | Subjects (right)
  - Mobile: Stacked vertical (Fragments above Subjects)
  - Each column has [Select All | None] buttons
  - Fragment cards show: title, summary, match %, category, date
  - Subject cards show: name, fragment count, fragment IDs
  - [Preview] button on fragments opens modal with full content
  - Bottom action bar: [✓ Create Draft] [✗ Cancel]
  - Selection summary: "X fragments, Y subjects selected, ~Z words"

#### FR-AA013: AI Draft Version Chain
- **Priority:** P0 (Critical)
- **Description:** Create parallel AI version chain separate from user versions
- **Acceptance Criteria:**
  - AI versions have ParentVersionId pointing to base user version
  - AI versions have VersionType = AI enum value
  - Only one active AI draft chain per article at a time
  - AI version numbering displays as v{parent}.{sequence} (e.g., v3.1, v3.2)
  - Internal numbering remains sequential integers
  - Display numbering walks parent chain to determine prefix

#### FR-AA014: Editor Locking During AI Draft
- **Priority:** P0 (Critical)
- **Description:** Lock current version when AI draft exists
- **Acceptance Criteria:**
  - Current Version tab is read-only when AI draft exists
  - Warning modal if user attempts to edit
  - Modal shows: "⚠️ Active AI Draft Exists - recommend review first"
  - Options: [View AI Draft] [Edit Anyway] [Cancel]
  - No hard lock - user can proceed with "Edit Anyway"

#### FR-AA015: AI Draft Viewing Tab
- **Priority:** P0 (Critical)
- **Description:** Show AI draft in separate tab with diff highlighting
- **Acceptance Criteria:**
  - Middle pane adds tab: "✨ AI Draft v{X.Y}" when draft exists
  - Shows draft content with diff highlighting (reuse existing version comparison)
  - Highlights: Green for additions, strikethrough for deletions, yellow for modifications
  - Header shows: version number, word count change, creation date
  - Tab badge shows "+XXX" word count difference
  - Clicking tab switches to draft view

#### FR-AA016: Draft Acceptance Flow
- **Priority:** P0 (Critical)
- **Description:** Allow user to approve AI draft and promote to user version
- **Acceptance Criteria:**
  - [✓ Accept Draft] button in Assistant panel after draft creation
  - Accepting creates new user version with draft content
  - Updates Article.Content to new version
  - Generates commit message: "Accepted AI draft v{X.Y}: {summary}"
  - Marks AI draft chain as complete
  - Conversation state changes to ✅ Complete
  - Returns to single-pane editor mode
  - Shows success toast: "Draft accepted as version {N}"

#### FR-AA017: Conversation State Tracking
- **Priority:** P0 (Critical)
- **Description:** Track and display conversation state in tree view
- **Acceptance Criteria:**
  - Icon in article tree shows conversation state:
    - 🔄 = AI is working (highest priority)
    - 👤 = Waiting for user response
    - ✅ = Draft approved (conversation complete)
    - (no icon) = No active conversation
  - Icons update in real-time via SignalR
  - Tooltip on hover explains state

#### FR-AA018: New Conversation Creation
- **Priority:** P1 (High)
- **Description:** Start fresh conversation after completing previous one
- **Acceptance Criteria:**
  - [+ New Conversation] button visible when conversation complete
  - Button disabled when conversation in progress
  - Clicking starts fresh conversation context
  - Previous conversation moved to history (read-only)
  - Default view when opening article with complete conversation

#### FR-AA019: Conversation State Machine
- **Priority:** P0 (Critical)
- **Description:** Enforce valid conversation state transitions
- **Acceptance Criteria:**
  - States: None → AI Working → Waiting for User → Complete
  - User cannot send messages while AI Working (Phase 1)
  - User can continue after draft approval (refine further)
  - User can start new conversation after approval
  - Cancellation returns to Waiting for User state

#### FR-AA020: Background Processing with Hangfire
- **Priority:** P0 (Critical)
- **Description:** Process agent conversations in background jobs
- **Acceptance Criteria:**
  - User message triggers Hangfire job
  - Job executes agentic loop with tool calling
  - Progress updates via SignalR to all connected users
  - Job handles tool execution and result processing
  - Job stores final response in conversation
  - Job updates conversation state

#### FR-AA021: Real-time Updates via SignalR
- **Priority:** P0 (Critical)
- **Description:** Broadcast agent progress to all users on same article
- **Acceptance Criteria:**
  - SignalR hub for article-scoped updates
  - Events: AgentStatus, ToolExecution, MessageReceived
  - All users on same article see same updates
  - Tool execution shows: tool name, status, summary
  - Progress updates: "Searching fragments...", "Creating draft..."
  - Updates appear in real-time without refresh

#### FR-AA022: Tool Execution Transparency (Phase 1)
- **Priority:** P0 (Critical)
- **Description:** Show all tool executions in conversation for development
- **Acceptance Criteria:**
  - Each tool call shown as expandable section
  - Shows: tool name, parameters, execution time, result summary
  - Format: "🔍 Searching fragments... ✓ Found 5 fragments (0.3s)"
  - Full transparency for debugging Phase 1
  - Can collapse/summarize in Phase 2

#### FR-AA023: Conversation Cancellation
- **Priority:** P1 (High)
- **Description:** Allow user to stop agent processing
- **Acceptance Criteria:**
  - [🗑️ Cancel] button visible when AI Working
  - Clicking kills Hangfire job immediately (hard stop)
  - Conversation remains in incomplete state
  - User can resume by sending new message
  - Toast message: "Conversation cancelled. Send message to continue."

#### FR-AA024: Multi-User Shared Conversations
- **Priority:** P1 (High)
- **Description:** All users on same article see same conversation
- **Acceptance Criteria:**
  - Conversation messages visible to all users
  - User names shown on each message
  - Real-time message sync via SignalR
  - State changes (AI working, draft created) visible to all
  - Any user can approve draft or send messages

#### FR-AA025: Version Commit Messages
- **Priority:** P0 (Critical)
- **Description:** Auto-generate descriptive commit message for every version
- **Acceptance Criteria:**
  - System feature (not agent tool)
  - Generates message on all version saves (user and AI)
  - Phase 1: Simple template-based messages
  - User version: "Edited {section} and added {topics}"
  - AI version: "Added {topics} from {N} fragments (+{words} words)"
  - Accept version: "Accepted AI draft v{X.Y}: {summary}"
  - Stored in ArticleVersion.ChangeMessage field
  - Displayed in version history

#### FR-AA026: Error Handling - No Results
- **Priority:** P1 (High)
- **Description:** Handle case when fragment search finds nothing
- **Acceptance Criteria:**
  - Message: "I couldn't find any fragments matching '{query}'"
  - Suggestions: "Try different keywords or broader terms"
  - User-friendly, no technical jargon
  - Allows user to refine search naturally

#### FR-AA027: Error Handling - System Failures
- **Priority:** P1 (High)
- **Description:** Graceful handling of system errors
- **Acceptance Criteria:**
  - User-friendly messages: "I encountered an issue. Please try again."
  - Technical details logged but not shown to user
  - Conversation remains recoverable
  - User can retry failed operation
  - Support team notified of errors

### Functional Requirements - Phase 2 (Future)

#### FR-AA101: LLM-Based Subject Clustering
- **Priority:** P2
- **Description:** Use LLM to intelligently cluster fragments into subjects
- **Rationale:** 
  - Single fragment can contain multiple subjects
  - Merge redundant subjects across fragments
  - Context-aware (knows what's in article vs what to add)
  - Action-oriented subjects: "Add X", "Enhance Y with Z"
- **Implementation:** LLM analyzes fragments + article context, returns structured subject assignments

#### FR-AA102: Enhanced Agent Tools
- **Priority:** P2
- **Description:** Additional tools for content intelligence
- **Tools:**
  - `analyze_article_gaps` - Identify missing topics and weak sections
  - `rewrite_section` - AI rewrite of specific section with style guidance
  - `suggest_outline` - Recommend structural improvements
  - `find_related_articles` - Suggest articles to reference/link
  - `expand_section` - Add depth to sparse sections

#### FR-AA103: Intelligent Version Messages
- **Priority:** P2
- **Description:** LLM-generated semantic commit messages
- **Example:** "Added OAuth 2.0 implementation guide with code examples and security considerations"
- **Benefit:** More descriptive than template-based messages

#### FR-AA104: Context Window Management
- **Priority:** P2
- **Description:** Handle long conversations that exceed LLM context limits
- **Strategies:** Sliding window, summary of old messages, smart pruning

#### FR-AA105: User Interruption
- **Priority:** P2
- **Description:** Allow sending messages while AI is working
- **Behavior:** Queue message or interrupt current operation

#### FR-AA106: Search/Filter in Review Pane
- **Priority:** P2
- **Description:** Filter fragments by confidence, category, date range, text search

#### FR-AA107: Progressive Disclosure UI
- **Priority:** P2
- **Description:** Show contextual tips as users learn features
- **Examples:** "💡 Tip: You can refine searches by asking for specific topics"

### Non-Functional Requirements

#### NFR-AA001: Performance - Agent Response Time
- **Requirement:** Agent loop completes in <15 seconds for typical operations
- **Rationale:** Users accept brief wait for intelligent operations
- **Measurement:** 95th percentile of tool execution + response generation

#### NFR-AA002: Performance - Fragment Search
- **Requirement:** Semantic search completes in <2 seconds for 10,000 fragments
- **Rationale:** Search is frequently used operation
- **Measurement:** Average query time with existing pgvector infrastructure

#### NFR-AA003: Performance - Draft Creation
- **Requirement:** Draft version creation completes in <3 seconds
- **Rationale:** Version operations should feel instant
- **Measurement:** Time from tool call to version save

#### NFR-AA004: Reliability - Hangfire Job Success
- **Requirement:** 99% of agent jobs complete successfully
- **Rationale:** High reliability needed for trust
- **Measurement:** Failed jobs / total jobs over 30 days

#### NFR-AA005: Scalability - Concurrent Conversations
- **Requirement:** Support 100 concurrent agent conversations
- **Rationale:** Enterprise users may have many active conversations
- **Measurement:** Load testing with 100 simultaneous jobs

#### NFR-AA006: Usability - Tool Transparency
- **Requirement:** Users understand what agent is doing at all times
- **Rationale:** Trust requires visibility
- **Measurement:** User testing feedback on clarity

#### NFR-AA007: Security - Conversation Isolation
- **Requirement:** Users only see conversations for articles they have access to
- **Rationale:** Respect existing article permissions
- **Measurement:** Security audit of SignalR groups and data access

#### NFR-AA008: Maintainability - Tool Addition
- **Requirement:** New agent tools can be added in <2 hours
- **Rationale:** Tool set will evolve based on usage
- **Measurement:** Time to implement and test new tool

---

## Data Model Changes

### New Entities

#### ArticleVersion (Extended)
```
Existing:
- Id (Guid)
- ArticleId (Guid)
- ContentSnapshot (string)
- ContentDiff (string, nullable)
- VersionNumber (int)
- CreatedById (Guid, nullable)
- CreatedAt (DateTimeOffset)

New Fields:
- ParentVersionId (Guid, nullable) - Points to version this was based on
- VersionType (enum: User | AI) - Distinguishes user vs AI versions
- ChangeMessage (string, nullable) - Auto-generated commit message
```

#### ArticleConversation (New)
```
- Id (Guid, PK)
- ArticleId (Guid, FK to Article)
- State (enum: Active | Complete | Cancelled)
- CreatedAt (DateTimeOffset)
- CompletedAt (DateTimeOffset, nullable)
- CreatedById (Guid, FK to User)
```

#### ConversationMessage (New)
```
- Id (Guid, PK)
- ConversationId (Guid, FK to ArticleConversation)
- MessageType (enum: User | Assistant | System | ToolExecution)
- Content (string)
- UserId (Guid, nullable, FK to User) - Null for Assistant messages
- CreatedAt (DateTimeOffset)
- Metadata (JSON, nullable) - Tool call details, etc.
```

#### FragmentSubject (New - Phase 1 infrastructure, Phase 2 usage)
```
- Id (Guid, PK)
- Name (string) - Subject name (e.g., "OAuth Implementation")
- Description (string, nullable) - AI-generated description (Phase 2)
- CreatedAt (DateTimeOffset)
```

#### FragmentSubjectMapping (New - Many-to-Many)
```
- FragmentId (Guid, FK to Fragment)
- SubjectId (Guid, FK to FragmentSubject)
- Confidence (double, nullable) - How confident is this mapping
- CreatedById (Guid, nullable) - Which agent/user created mapping
```

### Indexes

```sql
-- Performance critical queries
CREATE INDEX IX_ArticleVersion_ParentVersionId ON ArticleVersion(ParentVersionId);
CREATE INDEX IX_ArticleVersion_VersionType ON ArticleVersion(VersionType);
CREATE INDEX IX_ArticleConversation_ArticleId_State ON ArticleConversation(ArticleId, State);
CREATE INDEX IX_ConversationMessage_ConversationId_CreatedAt ON ConversationMessage(ConversationId, CreatedAt);
CREATE INDEX IX_FragmentSubjectMapping_FragmentId ON FragmentSubjectMapping(FragmentId);
CREATE INDEX IX_FragmentSubjectMapping_SubjectId ON FragmentSubjectMapping(SubjectId);
```

---

## User Journey

### Journey 1: First-Time Article Improvement

**Context:** User has created an article about "Authentication System" but it's incomplete. They want to enhance it with relevant fragments.

**Steps:**

1. **Initiate Conversation**
   - User opens article in editor
   - Clicks "Assistant" tab in right sidebar
   - Sees empty conversation with input field
   - Types: "Find fragments about OAuth implementation and add them to the article"
   - Clicks Send

2. **Agent Processing (Background)**
   - Hangfire job starts
   - Tree view shows 🔄 icon (AI Working)
   - Assistant panel shows: "🔍 Searching fragments..."
   - Tool executes: `search_fragments("OAuth implementation", 10, 0.7)`
   - Shows: "✓ Found 8 fragments (0.4s)"
   - Assistant shows: "🧠 Analyzing subjects..."
   - Extracts subjects from fragment summaries
   - Shows: "✓ Identified 4 subjects (0.2s)"

3. **Approval Request**
   - Tree view changes to 👤 icon (Waiting for User)
   - Assistant message: "I found 8 fragments covering 4 subjects that could enhance your article."
   - Shows summary card:
     ```
     8 Fragments | 4 Subjects
     Estimated: +520 words
     [✓ Approve All] [📋 Review]
     ```
   - User clicks [📋 Review] (wants to be selective)

4. **Review Interface**
   - Middle pane adds new tab: "📋 Review Selections"
   - Shows two-column layout:
     - Left: 8 fragments (all checked)
     - Right: 4 subjects (all checked)
   - User unchecks "JWT Token Handling" subject (too detailed)
   - System auto-unchecks fragments that only belonged to that subject
   - Selection updates: "7 fragments | 3 subjects | ~450 words"
   - User clicks [✓ Create Draft]

5. **Draft Creation**
   - Review tab closes
   - Assistant shows: "✏️ Creating draft version v3.1..."
   - Tool executes: `create_draft_version([fragment_ids], [subject_ids])`
   - Shows: "✓ Draft v3.1 created (1.1s)"
   - Assistant message: "Draft v3.1 is ready with content from 7 fragments. Added ~450 words covering OAuth implementation and security practices."
   - Middle pane adds tab: "✨ AI Draft v3.1 [+450]"

6. **Draft Review**
   - User clicks AI Draft tab
   - Sees article with new content highlighted in green
   - Reviews changes, looks good
   - Returns to Assistant tab
   - Clicks [✓ Accept Draft] button

7. **Completion**
   - New user version created (v4)
   - Article.Content updated
   - Commit message: "Accepted AI draft v3.1: Added OAuth implementation from 7 fragments"
   - AI Draft tab closes
   - Tree view shows ✅ icon (Complete)
   - Assistant shows: "Draft v4 applied successfully. [+ Start New Conversation]"

**Result:** User enhanced article in ~3 minutes vs 30 minutes manual searching and copying.

### Journey 2: Iterative Refinement

**Context:** User accepted a draft but wants to refine it further.

**Steps:**

1. User in Assistant tab with completed conversation showing
2. Types: "Make the security section less technical"
3. Tree view: ✅ → 🔄 (AI Working again)
4. Agent reads current article content
5. Agent creates new draft v4.1 with rewritten security section
6. Tree view: 🔄 → 👤 (Waiting for User)
7. User reviews, types: "Good, but add diagram references"
8. Agent creates v4.2 with diagram notes added
9. User accepts, becomes v5
10. Conversation remains open for further refinement OR user starts new conversation

**Result:** Progressive improvement through natural dialogue.

### Journey 3: Multi-User Collaboration

**Context:** Sarah starts article improvement, John joins to help.

**Steps:**

1. **Sarah's Actions:**
   - Opens article, starts Assistant conversation
   - "Find fragments about API design patterns"
   - Agent searches, presents 10 fragments for review

2. **John Joins:**
   - Opens same article
   - Sees Sarah's message in Assistant panel
   - Sees tree view: 👤 (Waiting for User)
   - Sees: "I found 10 fragments... [Approve All] [Review]"

3. **John Acts:**
   - Clicks [Review]
   - Unchecks 2 fragments he knows are outdated
   - Clicks [Create Draft]
   - Agent creates draft v2.1

4. **Both See:**
   - Draft v2.1 tab appears for both users
   - Tree view: 👤 (waiting for acceptance)
   - Either can accept the draft

5. **Sarah Accepts:**
   - Clicks [Accept Draft]
   - Both users see: "Draft v3 applied successfully"
   - Both can continue conversation or start new

**Result:** Seamless collaboration without coordination overhead.

---

## UX Design Principles

### 1. Transparency Through Visibility
- Show all tool executions with clear status indicators
- Explain what the agent is doing at each step
- Provide source attribution for all AI-generated content
- Make conversation history fully accessible

### 2. Human Control First
- All AI suggestions require explicit approval
- User can cancel operations at any time
- Draft versions don't affect main content until accepted
- Warning before potentially disruptive actions

### 3. Progressive Disclosure
- Start simple (just input field)
- Learn features through natural usage
- Contextual tips appear when relevant
- No heavy onboarding or tutorials upfront

### 4. Conversational Flexibility
- Natural language input (no rigid commands)
- Multi-turn dialogue for refinement
- Can continue after approval or start fresh
- Handles ambiguity gracefully

### 5. Collaborative by Default
- Shared conversations visible to team
- Real-time updates across users
- Clear attribution of who did what
- No coordination overhead

---

## Technical Architecture

### Component Overview

```
┌─────────────────────────────────────────────────────────┐
│                     Presentation Layer                   │
│  ┌────────────────┐  ┌──────────────────────────────┐  │
│  │ Assistant      │  │ Review Pane Component        │  │
│  │ Panel (Vue)    │  │ (Fragments + Subjects)       │  │
│  └────────────────┘  └──────────────────────────────┘  │
│          ↓ SignalR              ↓ HTTP API              │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│                   Application Layer                      │
│  ┌────────────────────────────────────────────────────┐ │
│  │ ArticleAgentService                                │ │
│  │ - ProcessUserMessageAsync()                        │ │
│  │ - ExecuteAgenticLoop()                             │ │
│  │ - ExecuteToolAsync()                               │ │
│  └────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────┐ │
│  │ Agent Tools (IArticleAgentTool)                    │ │
│  │ - SearchFragmentsTool                              │ │
│  │ - CreateDraftVersionTool                           │ │
│  │ - GetArticleContentTool                            │ │
│  │ - ListVersionsTool                                 │ │
│  └────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────┐ │
│  │ ConversationService                                │ │
│  │ - CreateConversationAsync()                        │ │
│  │ - AddMessageAsync()                                │ │
│  │ - UpdateStateAsync()                               │ │
│  └────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────┐ │
│  │ SubjectExtractionService                           │ │
│  │ - ExtractSubjectsFromFragments() [Phase 1]         │ │
│  │ - ExtractSubjectsWithLLM() [Phase 2]               │ │
│  └────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────┐ │
│  │ VersionMessageGenerator                            │ │
│  │ - GenerateMessageAsync()                           │ │
│  └────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│                  Infrastructure Layer                    │
│  ┌────────────────────────────────────────────────────┐ │
│  │ ArticleHub (SignalR)                               │ │
│  │ - AgentStatus, ToolExecution, MessageReceived      │ │
│  └────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────┐ │
│  │ Hangfire Jobs                                      │ │
│  │ - ProcessAgentConversationJob                      │ │
│  │ - Retries, logging, error handling                 │ │
│  └────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────┐ │
│  │ BedrockAiService (IAiProcessingService)            │ │
│  │ - Tool calling with Microsoft.Extensions.AI        │ │
│  └────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

### Key Interfaces

```csharp
// Core agent service
public interface IArticleAgentService
{
    Task<ConversationMessage> ProcessUserMessageAsync(
        Guid articleId, 
        string message, 
        Guid userId, 
        CancellationToken cancellationToken);
}

// Tool abstraction
public interface IArticleAgentTool
{
    string Name { get; }
    string Description { get; }
    JsonSchema ParametersSchema { get; }
    Task<ToolResult> ExecuteAsync(
        JsonElement parameters, 
        AgentContext context, 
        CancellationToken cancellationToken);
}

// Subject extraction (swappable implementation)
public interface ISubjectExtractionService
{
    Task<SubjectExtractionResult> ExtractSubjectsAsync(
        List<Fragment> fragments, 
        string articleContent, 
        CancellationToken cancellationToken);
}

// Version message generation
public interface IVersionMessageGenerator
{
    Task<string> GenerateMessageAsync(
        string? previousContent,
        string newContent,
        VersionMessageContext context);
}
```

### Agentic Loop Pseudocode

```
function ProcessUserMessage(message, articleId, userId):
    conversation = GetOrCreateConversation(articleId)
    AddMessage(conversation, message, userId)
    
    EnqueueHangfireJob(() => {
        UpdateConversationState(conversation, "AI Working")
        BroadcastStatus("AI Working")
        
        messages = BuildChatMessages(conversation.Messages)
        tools = GetAvailableTools()
        maxIterations = 10
        iteration = 0
        
        while iteration < maxIterations:
            iteration++
            
            // Call AI with tool definitions
            response = CallAIWithTools(messages, tools)
            
            if response.HasToolCalls:
                // Execute each tool
                foreach toolCall in response.ToolCalls:
                    BroadcastToolExecution(toolCall.Name, "executing")
                    result = ExecuteTool(toolCall, articleId, userId)
                    BroadcastToolExecution(toolCall.Name, "complete", result.Summary)
                    
                    // Add tool result to conversation
                    messages.Add(ToolResultMessage(toolCall.Id, result))
                
                // Continue loop to let AI process results
                continue
            
            // No more tool calls - AI has final response
            AddMessage(conversation, response.Content, "Assistant")
            UpdateConversationState(conversation, "Waiting for User")
            BroadcastMessage(response.Content)
            break
        
        if iteration >= maxIterations:
            AddMessage(conversation, "Max iterations reached", "System")
    })
```

### SignalR Events

```csharp
// Server → Client events
public interface IArticleHubClient
{
    Task AgentStatus(string status, DateTimeOffset timestamp);
    Task ToolExecution(string toolName, string status, string summary);
    Task MessageReceived(ConversationMessage message);
    Task ConversationStateChanged(string state);
    Task DraftVersionCreated(Guid versionId, string summary);
}

// Client → Server methods
public class ArticleHub : Hub<IArticleHubClient>
{
    Task JoinArticle(Guid articleId);
    Task LeaveArticle(Guid articleId);
    Task SendMessage(Guid articleId, string message);
}
```

---

## UI Component Specifications

### Assistant Panel Component (Vue)

**File:** `assistant-panel.js`

**Props:**
- `articleId` (String) - Current article ID
- `conversationState` (String) - Current conversation state

**Data:**
- `messages` (Array) - Conversation messages
- `newMessage` (String) - User input
- `isAIWorking` (Boolean) - Agent processing state
- `activeDraftId` (Guid) - Current AI draft version
- `historyVisible` (Boolean) - History panel state

**Methods:**
- `sendMessage()` - Send user message via SignalR
- `showHistory()` - Toggle history panel
- `startNewConversation()` - Begin fresh conversation
- `cancelConversation()` - Stop agent processing
- `acceptDraft()` - Approve AI draft version

**Computed:**
- `canSendMessage` - True if not AI working
- `canStartNew` - True if conversation complete
- `hasActiveDraft` - True if AI draft exists

### Review Pane Component (Vue)

**File:** `review-selections-panel.js`

**Props:**
- `fragments` (Array) - Found fragments with metadata
- `subjects` (Array) - Extracted subjects
- `initialMapping` (Object) - Fragment-subject relationships

**Data:**
- `selectedFragments` (Set) - Checked fragment IDs
- `selectedSubjects` (Set) - Checked subject IDs
- `fragmentSubjectMap` (Map) - Many-to-many relationships

**Methods:**
- `toggleFragment(id)` - Check/uncheck fragment
- `toggleSubject(id)` - Check/uncheck subject
- `selectAllFragments()` - Check all fragments
- `selectAllSubjects()` - Check all subjects
- `previewFragment(id)` - Show fragment modal
- `createDraft()` - Submit selections to API
- `cancel()` - Close review pane

**Computed:**
- `selectionSummary` - "X fragments, Y subjects, ~Z words"
- `estimatedWordCount` - Total words in selected fragments

**Watch:**
- `selectedFragments` - Auto-update subject selection on fragment change
- `selectedSubjects` - Auto-update fragment selection on subject change

### Version Display Updates

**Existing:** `versions-panel.js`

**Changes:**
- Filter to show only User versions (VersionType = User)
- Display commit messages under each version
- Highlight current version
- Click version shows comparison with previous

---

## API Endpoints

### POST /api/articles/{articleId}/assistant/message
**Description:** Send message to article assistant  
**Request:**
```json
{
  "message": "Find fragments about OAuth implementation"
}
```
**Response:**
```json
{
  "conversationId": "guid",
  "messageId": "guid",
  "status": "processing"
}
```

### GET /api/articles/{articleId}/assistant/conversation
**Description:** Get current conversation for article  
**Response:**
```json
{
  "id": "guid",
  "state": "Active",
  "messages": [
    {
      "id": "guid",
      "type": "User",
      "content": "Find fragments...",
      "userName": "John Smith",
      "createdAt": "2025-12-30T10:00:00Z"
    },
    {
      "id": "guid",
      "type": "Assistant",
      "content": "I found 8 fragments...",
      "createdAt": "2025-12-30T10:00:05Z",
      "metadata": {
        "toolExecutions": [
          {
            "tool": "search_fragments",
            "duration": 0.3,
            "result": "Found 8 fragments"
          }
        ]
      }
    }
  ]
}
```

### GET /api/articles/{articleId}/assistant/history
**Description:** Get past conversations for article  
**Response:**
```json
{
  "conversations": [
    {
      "id": "guid",
      "state": "Complete",
      "createdAt": "2025-12-28T14:00:00Z",
      "completedAt": "2025-12-28T14:15:00Z",
      "messageCount": 8,
      "summary": "Enhanced article with OAuth fragments"
    }
  ]
}
```

### POST /api/articles/{articleId}/assistant/cancel
**Description:** Cancel active agent processing  
**Response:**
```json
{
  "success": true,
  "message": "Conversation cancelled"
}
```

### POST /api/articles/{articleId}/assistant/new
**Description:** Start new conversation  
**Response:**
```json
{
  "conversationId": "guid",
  "message": "New conversation started"
}
```

### GET /api/articles/{articleId}/assistant/subjects
**Description:** Get subjects extracted from fragments  
**Query Params:** `fragmentIds` (comma-separated)  
**Response:**
```json
{
  "subjects": [
    {
      "id": "guid",
      "name": "OAuth 2.0 implementation details",
      "fragmentCount": 3,
      "fragmentIds": ["guid1", "guid2", "guid3"]
    }
  ],
  "mapping": {
    "fragmentId1": ["subjectId1", "subjectId2"],
    "fragmentId2": ["subjectId1"]
  }
}
```

### POST /api/articles/{articleId}/assistant/draft
**Description:** Create AI draft version from selections  
**Request:**
```json
{
  "fragmentIds": ["guid1", "guid2"],
  "subjectIds": ["guid1"]
}
```
**Response:**
```json
{
  "versionId": "guid",
  "versionNumber": "3.1",
  "wordCount": 450,
  "changeMessage": "Added OAuth implementation from 3 fragments"
}
```

### POST /api/articles/{articleId}/assistant/accept-draft
**Description:** Accept AI draft and promote to user version  
**Request:**
```json
{
  "draftVersionId": "guid"
}
```
**Response:**
```json
{
  "newVersionId": "guid",
  "versionNumber": 4,
  "message": "Draft accepted successfully"
}
```

---

## Testing Strategy

### Unit Tests

**ArticleAgentService Tests:**
- `ProcessUserMessageAsync_CreatesConversation_WhenNoneExists`
- `ProcessUserMessageAsync_EnqueuesHangfireJob`
- `ExecuteAgenticLoop_StopsAtMaxIterations`
- `ExecuteAgenticLoop_CallsToolsCorrectly`
- `ExecuteAgenticLoop_HandlesToolFailures`

**Tool Tests:**
- `SearchFragmentsTool_FindsRelevantFragments`
- `SearchFragmentsTool_RespectsThreshold`
- `CreateDraftVersionTool_CreatesAIVersion`
- `CreateDraftVersionTool_SetsParentVersion`
- `GetArticleContentTool_ReturnsCurrentContent`

**Subject Extraction Tests:**
- `ExtractSubjects_Phase1_UsesFragmentSummary`
- `ExtractSubjects_HandlesDeduplication`
- `ExtractSubjects_CreatesManyToManyMappings`

**Version Message Tests:**
- `GenerateMessage_UserVersion_IncludesEditSummary`
- `GenerateMessage_AIVersion_IncludesFragmentCount`
- `GenerateMessage_AcceptVersion_IncludesDraftNumber`

### Integration Tests

**Conversation Flow Tests:**
- `UserSendsMessage_AgentResponds_ConversationStateUpdates`
- `MultipleToolCalls_ExecuteInSequence_ResultsProcessed`
- `DraftCreation_VersionCreated_StateTransitionsCorrectly`
- `DraftAcceptance_PromotesToUserVersion_ConversationCompletes`

**Multi-User Tests:**
- `TwoUsersOnSameArticle_SeeSameConversation`
- `UserSendsMessage_OtherUserSeesUpdate_RealTime`
- `EitherUserCanApprove_BothSeeResult`

**Cancellation Tests:**
- `CancelDuringToolExecution_JobStops_ConversationRecoverable`
- `CancelThenResume_ContinuesCorrectly`

### E2E Tests (Playwright)

**Happy Path:**
1. Open article
2. Click Assistant tab
3. Type message "find fragments about X"
4. Wait for results
5. Click Review
6. Uncheck some items
7. Click Create Draft
8. Review draft in tab
9. Click Accept
10. Verify version created
11. Verify conversation complete

**Multi-User:**
1. User A starts conversation
2. User B opens same article
3. Verify B sees A's messages
4. B clicks Review
5. B unchecks items
6. B creates draft
7. A sees draft tab appear
8. A accepts draft
9. B sees completion

**Error Scenarios:**
1. No fragments found
2. Network error during search
3. Draft creation fails
4. Cancellation mid-process

---

## Success Metrics & KPIs

### Adoption Metrics (30 days)
- **Target:** 50% of active users try Assistant at least once
- **Measurement:** % users who send at least one message

### Engagement Metrics (90 days)
- **Target:** 30% of articles include Assistant-enhanced version
- **Measurement:** % articles with VersionType = AI in history

### Efficiency Metrics
- **Target:** 60% reduction in article enhancement time
- **Baseline:** Average 45 minutes manual fragment search + integration
- **Target:** Average 15 minutes with Assistant
- **Measurement:** Time from article open to version save

### Quality Metrics
- **Target:** 80% of AI drafts approved without modification
- **Measurement:** % drafts accepted immediately vs refined further

### User Satisfaction
- **Target:** NPS score 50+ for Assistant feature
- **Measurement:** In-app survey after first 5 uses

### Technical Metrics
- **Agent Response Time:** P95 < 15 seconds
- **Fragment Search Time:** P95 < 2 seconds
- **Draft Creation Time:** P95 < 3 seconds
- **Job Success Rate:** >99%
- **SignalR Connection Stability:** >99.5%

---

## Risks & Mitigation

### Risk 1: LLM Tool Calling Reliability
**Risk:** AI may not call tools correctly or get stuck in loops  
**Impact:** High - Core feature failure  
**Mitigation:**
- Implement max iteration limit (10)
- Extensive tool testing with various inputs
- Fallback error messages when loops detected
- Logging and monitoring of tool call patterns

### Risk 2: Performance with Large Fragment Sets
**Risk:** Searching 100K+ fragments may be slow  
**Impact:** Medium - Poor user experience  
**Mitigation:**
- Pgvector indexing already handles this
- Load testing with production-scale data
- Implement result pagination if needed
- Cache frequently accessed fragments

### Risk 3: Version History Complexity
**Risk:** Parallel version chains may confuse users  
**Impact:** Medium - User frustration  
**Mitigation:**
- Hide AI versions from main version panel
- Clear visual distinction (✨ icon for AI drafts)
- Commit messages explain what changed
- User testing to validate understanding

### Risk 4: Multi-User Race Conditions
**Risk:** Two users approving drafts simultaneously  
**Impact:** Low - Rare but problematic  
**Mitigation:**
- Optimistic concurrency on version creation
- First approval wins, second gets error
- Clear error message: "Draft already accepted"
- Refresh UI to show current state

### Risk 5: Hangfire Job Failures
**Risk:** Background jobs may fail and leave conversations stuck  
**Impact:** Medium - User stuck waiting  
**Mitigation:**
- Automatic retry on transient failures (3 retries)
- Timeout detection (5 minute max)
- Dead letter queue for failed jobs
- UI shows error and allows retry

### Risk 6: Context Window Limits
**Risk:** Long conversations exceed LLM context  
**Impact:** Low in Phase 1, High in Phase 2  
**Mitigation:**
- Phase 1: Accept limitation, rare in practice
- Phase 2: Implement conversation summarization
- Monitor conversation lengths
- Warn users approaching limits

---

## Launch Plan

### Phase 1: MVP (Weeks 1-8)

**Week 1-2: Data Model & Infrastructure**
- ArticleVersion extensions (ParentVersionId, VersionType, ChangeMessage)
- ArticleConversation and ConversationMessage entities
- FragmentSubject and mapping tables
- Database migrations
- Repository extensions

**Week 3-4: Backend Services**
- ArticleAgentService with tool calling
- 4 core agent tools (search, create draft, get content, list versions)
- ConversationService for state management
- SubjectExtractionService (Phase 1 simple version)
- VersionMessageGenerator
- Hangfire job for agent processing
- SignalR hub for real-time updates

**Week 5-6: Frontend Components**
- Assistant panel Vue component
- Review pane Vue component
- AI Draft tab integration
- Tree view icon updates
- SignalR client integration
- API client methods

**Week 7: Integration & Testing**
- End-to-end flow testing
- Multi-user testing
- Performance testing
- Bug fixes
- Documentation

**Week 8: Beta Launch**
- Deploy to beta environment
- Internal user testing
- Collect feedback
- Minor iterations
- Production deployment

### Phase 2: Enhancements (Weeks 9-16)

**Week 9-10: LLM Subject Clustering**
- Implement intelligent subject extraction
- Many-to-many subject assignments
- Context-aware subject naming
- A/B test vs Phase 1 simple version

**Week 11-12: Additional Tools**
- analyze_article_gaps
- rewrite_section
- suggest_outline
- find_related_articles

**Week 13-14: UX Improvements**
- Progressive disclosure tips
- Search/filter in review pane
- Context window management
- Better error messages
- User onboarding (optional)

**Week 15-16: Polish & Scale**
- Performance optimizations
- Load testing
- Monitoring dashboards
- Analytics integration
- Production hardening

---

## Out of Scope

### Explicitly Not Included in Phase 1-2

**Advanced Collaboration:**
- Real-time co-editing of drafts (async review only)
- Approval workflows with multiple reviewers (single user approval)
- Role-based permissions for assistant usage (all users equal access)

**Content Generation:**
- Write entire articles from scratch (enhancement only)
- Generate images or diagrams (text only)
- Create videos or presentations (documentation only)

**External Integrations:**
- Export to Google Docs, Notion, Confluence (internal only)
- Import from external knowledge bases (fragments only)
- Third-party tool plugins (closed system)

**Advanced AI Features:**
- Voice interaction with assistant (text only)
- Image analysis in fragments (text-based)
- Sentiment analysis of conversations (basic only)
- Predictive article suggestions (reactive only)

**Enterprise Features:**
- Custom agent training per organization (shared model)
- White-label assistant branding (Medley brand only)
- API access for external agents (internal use only)

---

## Appendix

### Glossary

- **Agent:** AI system that can use tools to accomplish tasks autonomously
- **Tool:** Specific capability the agent can invoke (search, create draft, etc.)
- **Tool Call:** Agent's invocation of a tool with specific parameters
- **Agentic Loop:** Iterative process of agent calling tools and processing results
- **Draft Version:** AI-generated version that doesn't affect main content until approved
- **Version Chain:** Linked sequence of versions through ParentVersionId relationships
- **Subject:** Thematic grouping of related fragments
- **Fragment-Subject Mapping:** Many-to-many relationship between fragments and subjects
- **Conversation State:** Current status (Active, Waiting, Complete) of assistant conversation
- **Commit Message:** Auto-generated description of what changed in a version

### References

- [Microsoft.Extensions.AI Documentation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.ai)
- [AWS Bedrock Tool Calling](https://docs.aws.amazon.com/bedrock/latest/userguide/tool-use.html)
- [Hangfire Background Jobs](https://www.hangfire.io/)
- [SignalR Real-time Updates](https://learn.microsoft.com/en-us/aspnet/core/signalr/)
- [Pgvector Semantic Search](https://github.com/pgvector/pgvector)

### Related Documents

- [Solution Architecture](./solution-architecture.md) - Overall system architecture
- [Epics](./epics.md) - Detailed epic and story breakdown
- [Technical Decisions](./technical-decisions.md) - Technology choices and rationale
- [UX Specification](./ux-specification.md) - User interface design details

---

**Document Status:** ✅ Design Complete - Ready for Implementation  
**Next Steps:** 
1. Technical design review with engineering team
2. UX review with design team
3. Story breakdown and estimation
4. Sprint planning for Phase 1 MVP

**Questions or Feedback:** Contact product team

