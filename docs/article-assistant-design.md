# Article Assistant - Comprehensive Design Specification

**Version:** 1.0  
**Date:** December 30, 2024  
**Status:** Design Complete - Ready for Implementation

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Core Concept](#core-concept)
3. [Design Decisions Summary](#design-decisions-summary)
4. [End-to-End User Flows](#end-to-end-user-flows)
5. [Architecture & Data Model](#architecture--data-model)
6. [User Interface Design](#user-interface-design)
7. [Conversation Management](#conversation-management)
8. [Multi-User Collaboration](#multi-user-collaboration)
9. [Agent Tools & Capabilities](#agent-tools--capabilities)
10. [Phase 1 vs Phase 2 Features](#phase-1-vs-phase-2-features)
11. [Error Handling](#error-handling)
12. [Implementation Notes](#implementation-notes)

---

## Executive Summary

The **Article Assistant** is an agentic AI feature that helps users improve articles by finding related fragments and intelligently incorporating them. Unlike traditional chat interfaces, the Assistant can take actions through tool calling, creating draft versions that users can review and approve before applying.

### Key Innovation: Draft Version Chains

The system maintains parallel version chains:
- **User versions**: The official version history
- **AI draft versions**: Temporary improvement suggestions

This architecture provides safety, auditability, and explicit user control while enabling powerful AI-assisted content enhancement.

### Design Philosophy

1. **Human-in-the-loop**: AI suggests, human decides
2. **Safety first**: All changes go through draft review
3. **Transparent**: Users see what the AI is doing
4. **Collaborative**: Multi-user conversations on shared articles
5. **Progressive**: Start simple (Phase 1), enhance intelligently (Phase 2)

---

## Core Concept

### The Assistant Tab

Replaces the existing "Chat" tab with a more capable "Assistant" that:
- Engages in natural language conversation
- Uses tools to search fragments, analyze content, create drafts
- Maintains conversation history per article
- Supports multi-user collaboration
- Shows progress transparently during work

### The Approval Workflow

When users request improvements:
1. Assistant searches for relevant fragments
2. Extracts subjects from fragments
3. Presents fragments and subjects for user approval
4. User reviews, selects/deselects items
5. Assistant creates AI draft version with approved content
6. User reviews diff in dedicated tab
7. User accepts (creates new user version) or refines further

### Version Chain Architecture

```
User Versions (official history):
v1 → v2 → v3 [current]

AI Draft Chain (temporary):
v3 → v3.1 → v3.2 [AI working on improvements]

After approval:
v1 → v2 → v3 → v4 [AI draft accepted, becomes new current]
```

---

## Design Decisions Summary

### 1. Naming & Branding

**Decision:** Tab name = **"Assistant"** (💬 icon)

**Rationale:**
- Friendly and approachable
- Doesn't limit to chat or actions
- Conveys help/collaboration
- Room to grow capabilities

**Rejected alternatives:** Chat (too limited), Agent (too technical), AI (too generic), Studio (unclear), Copilot (derivative)

---

### 2. Version Architecture

#### Core Decisions

| Aspect | Decision | Rationale |
|--------|----------|-----------|
| **Version Chains** | Parallel User + AI chains | Separates official history from draft suggestions |
| **ParentVersionId** | Add to ArticleVersion entity | Links versions in chains |
| **VersionType** | Enum: User \| AI | Distinguishes version types |
| **Version Numbering** | Sequential integers, display with prefix | Internal: 1,2,3,4,5... Display: v3, v3.1, v3.2, v4 |
| **Current Version Lock** | Read-only while AI draft exists | Prevents conflicts |
| **Active Draft Limit** | Single active AI chain per article | Simplifies UX |
| **Version Messages** | Auto-generated for all versions | Like Git commit messages |
| **Versions Panel** | Shows User versions only | AI versions live in Assistant conversation |

#### Version Message Generation

**System Feature (not an agent tool):**
- Automatically generated when any version is saved
- User versions: "Edited introduction and added examples"
- AI versions: "Added OAuth details from 3 fragments (+450 words)"
- Accept action: "Accepted AI draft v3.2: Enhanced OAuth section"

**Phase 1:** Simple template-based generation from diff  
**Phase 2:** LLM-generated semantic messages

---

### 3. Approval Workflow

#### Fragment-Subject Relationship

**Architecture:** Many-to-many relationship (fragments ↔ subjects)

**Phase 1 Implementation:**
- Fragment.Summary = Subject name
- Effectively 1:1 in practice
- Architecture supports many-to-many from day one

**Phase 2 Enhancement:**
- LLM analyzes fragments and article context
- Intelligent subject extraction
- One fragment can belong to multiple subjects
- Subjects can span multiple fragments
- Context-aware recommendations (what to add vs enhance)

#### Selection Behavior

| Rule | Behavior |
|------|----------|
| **Default state** | All fragments and subjects selected (opt-out) |
| **Fragment deselection** | If all subjects for fragment deselected → auto-deselect fragment |
| **Subject deselection** | If all fragments for subject deselected → auto-deselect subject |
| **Independence** | Can deselect either fragments or subjects independently |

#### Review Pane

**Location:** New tab in center pane (opens when user clicks "Review")

**Layout:**
```
┌────────────────────────────────────────┐
│  Fragments (8)     │  Subjects (4)     │
│  ─────────────────────────────────────  │
│  ☑ Fragment #247   │  ☑ OAuth Impl.   │
│    Meeting Notes   │     (3 fragments) │
│    95% match       │     #247,#183,#291│
│    🏷️ OAuth, Security                  │
│    [Preview]       │  ☑ Security      │
│                    │     (2 fragments) │
│  ☑ Fragment #183   │     #247, #412   │
│    ...             │                   │
│                    │  ☑ API Design    │
│                    │     (1 fragment)  │
│                    │     #291          │
└────────────────────────────────────────┘
Selection: 8 fragments, 4 subjects, ~520 words
[✓ Create Draft] [✗ Cancel]
```

**Features:**
- Two-column layout (Fragments | Subjects)
- Fragment preview in modal (reuse Sources pattern)
- Live selection updates
- No search/filter (Phase 1)
- Mobile: Stacked vertical layout

---

### 4. Agent Tools

#### Phase 1 MVP Tools (4 tools)

1. **search_fragments(query, limit, threshold)**
   - Semantic search for related fragments
   - Returns fragments with similarity scores
   - User approves before use

2. **create_draft_version(fragment_ids, subject_ids)**
   - Creates new AI draft version
   - Incorporates approved fragments
   - Generates version message
   - Returns version info

3. **get_article_content(article_id)**
   - Retrieves current article content
   - Provides context for AI decisions

4. **list_versions(article_id)**
   - Shows version history
   - Includes commit messages
   - Helps user navigate history

#### Why These Four?

- **Focused:** Core improvement workflow only
- **Testable:** Each tool has clear input/output
- **Safe:** All require user approval before changes
- **Complete:** Covers end-to-end improvement flow

---

### 5. Tool Transparency

**Phase 1 Decision:** Show everything (full transparency)

**Display format:**
```
🤖 Assistant:

🔍 Searching fragments...
   Query: "authentication"
   ✓ Found 8 fragments (0.4s)

🧠 Analyzing subjects...
   ✓ Identified 4 subjects (0.3s)

I found 8 fragments covering 4 subjects...
[Review button]
```

**Real-time updates via SignalR:**
- Show each tool execution
- Display results with checkmarks
- Progress visible as it happens

**Future option:** Summarize/collapse for production (can add later)

---

### 6. Conversation Management

#### Persistence & Scope

| Aspect | Decision |
|--------|----------|
| **Persistence** | Forever (no expiry in Phase 1) |
| **Scope** | Per-article (each article has own conversation) |
| **Past conversations** | Read-only, accessible via [📜 History] button |
| **New conversation** | Available after draft approval |
| **Default view** | Start new conversation if previous complete |
| **Context** | Full conversation history sent to AI (Phase 1) |

#### Conversation States

```
[No Conversation]
    ↓ User sends message
[AI Working] 🔄
    ↓ AI presents approval
[Waiting for User] 👤
    ↓ User approves/refines
[AI Working] 🔄 OR [Continue conversing]
    ↓ User accepts draft
[Approved] ✅
    ↓ User can start new OR continue
[New Conversation] OR [Continue Refining]
```

#### Tree View Status Icons

**Priority order (highest to lowest):**
1. 🔄 **AI Working** - Agent actively processing
2. 👤 **Waiting for User** - Needs user input/approval
3. ✅ **Approved** - Draft accepted, conversation complete
4. (No icon) - No active conversation

#### Conversation UI

**Layout:** Single-pane with history access

```
┌─────────────────────────────────────────┐
│ 💬 Assistant          [📜 History (3)]  │
├─────────────────────────────────────────┤
│                                          │
│ [Current conversation messages]         │
│                                          │
│ ─────────────────────────────────────── │
│ Type a message...          [Send]       │
└─────────────────────────────────────────┘
```

**Empty state:** Minimal - just input field (learn by doing)

**History button:** Opens list of past conversations (read-only)

---

### 7. Multi-User Collaboration

#### Shared Conversations

**Decision:** All users see all messages in real-time

**Implementation:**
- SignalR broadcasts messages to all users on same article
- Real-time synchronization of conversation state
- All users see tree view status icons update
- Collaborative like Slack/Teams

#### Message Attribution

**Minimal display:**
```
John Smith
Add OAuth implementation details

Assistant
🔍 Searching fragments...
✓ Found 5 fragments

Sarah Chen
Make it less technical
```

Simple names, clean layout, no avatars or timestamps in primary view.

#### Edit Warning

**When user attempts to edit while AI draft exists:**

```
⚠️ Active AI Draft Exists

An AI assistant draft is currently in progress.
Editing now may cause confusion when the draft is applied.

Recommendation: Review and approve/reject the AI draft first.

[View AI Draft] [Edit Anyway] [Cancel]
```

**No locking:** Soft warning only (Phase 1)

---

### 8. Interaction Controls

#### User Actions During AI Work

**Phase 1 Decision:** Cannot send messages while AI is working

**Rationale:**
- Simpler implementation
- Prevents complex interrupt handling
- Most operations complete quickly (<10 seconds)
- Can add queuing in Phase 2 if needed

#### Cancellation

**Behavior:** Hard stop (non-destructive)

**What happens:**
- Kills Hangfire background job immediately
- Conversation remains in incomplete state
- User can resume by sending another message
- Like pausing rather than deleting

**Use case:** User realizes they asked wrong question or wants to change direction

#### Post-Approval Behavior

**User can:**
- Continue refining in same conversation
- Start new conversation (clears state)

**New conversation button:**
- Always available after approval
- Disabled before approval (one conversation at a time)

---

### 9. Center Pane Tabbed Interface

#### Normal Mode (No AI Draft)

```
┌─────────────────────────────────────────┐
│  📄 Content                              │
├─────────────────────────────────────────┤
│  [Tiptap editor - editable]             │
└─────────────────────────────────────────┘
```

Single tab, standard editing experience.

#### AI Draft Exists

```
┌─────────────────────────────────────────┐
│  📄 Current Version  |  ✨ AI Draft (v3.2) │
├─────────────────────────────────────────┤
│  [Content based on active tab]          │
│                                          │
│  Current: Read-only (locked)            │
│  AI Draft: Diff view with highlights    │
└─────────────────────────────────────────┘
```

**Tab 1 (Current Version):**
- Shows current user version
- Read-only (locked) while draft exists
- User cannot edit

**Tab 2 (AI Draft):**
- Shows AI draft content
- Rendered with highlights (additions/deletions)
- Reuses existing version comparison rendering
- Action buttons: [Accept Draft] [Discard]

#### Review Mode

```
┌─────────────────────────────────────────┐
│  📄 Current  |  📋 Review Selections     │
├─────────────────────────────────────────┤
│  [Fragment & Subject approval interface]│
└─────────────────────────────────────────┘
```

Temporarily adds Review tab when user needs to approve fragments.

---

### 10. Performance & Responsiveness

#### Background Processing

**Decision:** Use Hangfire for AI work

**Benefits:**
- Non-blocking - user can switch articles
- Resilient - retries on failure
- Observable - can track job status
- Cancellable - user can stop job

**Implementation:**
- User sends message → creates Hangfire job
- Job runs agent loop (tool calling)
- Progress updates via SignalR
- Result delivered via SignalR when complete

#### Progress Indication

**During AI work:**
- Tree view shows 🔄 icon
- Assistant panel shows spinner and status
- Real-time tool execution updates
- "AI Working..." or "Searching fragments..." messages

**Long operations (>10 seconds):**
- Show progress messages
- Keep UI responsive
- User can cancel if needed
- No hard timeout (Phase 1)

---

### 11. Discovery & Onboarding

#### Empty State

**Decision:** Minimal (just input field)

**Rationale:**
- Learn by doing
- No hand-holding or example prompts
- Users naturally ask questions
- Progressive disclosure of capabilities

#### Capability Discovery

**Progressive disclosure approach:**
- Users learn by using
- After first search: "💡 Tip: I can refine searches..."
- After first draft: "💡 Tip: You can ask me to make changes..."
- Contextual hints appear after relevant actions

#### First-Time Experience

**Decision:** No onboarding (Phase 2+ feature)

**Phase 1:** Ship fast, learn from usage  
**Phase 2:** Add tours/walkthroughs based on user feedback

---

### 12. Error Handling

#### User-Friendly Messages

**No Results:**
```
I couldn't find any fragments matching "quantum encryption."

Try:
• Different keywords
• Broader search terms
• Checking if fragments exist for this topic
```

**Search Failed:**
```
I encountered an issue while searching.
Please try again. If the problem persists, contact support.
```

**Draft Creation Failed:**
```
I wasn't able to create the draft version.
Please try again or cancel this conversation.
```

**Timeout:**
```
The request timed out.
Would you like me to try again?
```

**Principles:**
- User-friendly language
- Actionable suggestions
- No technical jargon
- Log technical details server-side

---

## End-to-End User Flows

### Flow 1: Simple Improvement Request

**Scenario:** User wants to add authentication details to article

```
1. User opens article in Articles view
   └─ No active conversation

2. User clicks Assistant tab
   └─ Empty state with input field

3. User types: "Add information about OAuth implementation"
   └─ Sends message

4. Article tree icon changes to 🔄 (AI Working)

5. Assistant panel shows progress:
   🔍 Searching fragments...
      Query: "OAuth implementation"
      ✓ Found 5 fragments (0.3s)
   
   🧠 Analyzing subjects...
      ✓ Identified 2 subjects (0.2s)

6. Assistant responds:
   "I found 5 fragments covering 2 subjects that could
   enhance your article.
   
   [8 Fragments | 2 Subjects]
   Estimated: +450 words
   
   [✓ Approve All] [📋 Review]"

7. User clicks [Review]
   └─ Center pane adds "Review Selections" tab

8. Review pane opens with two columns:
   Left: Fragments (5 items, all checked)
   Right: Subjects (2 items, all checked)

9. User unchecks one low-relevance fragment
   └─ Selection updates: "4 fragments, 2 subjects, ~380 words"

10. User clicks [Create Draft]
    └─ Review tab closes
    └─ AI Working 🔄

11. Assistant creates draft:
    ✏️ Creating draft version v3.1...
       Using 4 fragments, 2 subjects
       ✓ Draft v3.1 created (1.1s)
    
    "Draft v3.1 is ready. +380 words added covering
    OAuth implementation and security practices.
    
    [📊 View Draft]"

12. Center pane adds "AI Draft (v3.1)" tab
    └─ Shows diff with highlighted additions

13. User reviews changes, looks good

14. User clicks [Accept Draft] in draft tab
    └─ Creates user version v4
    └─ Updates article content
    └─ Returns to single-pane editor
    └─ Conversation state: ✅ Approved

15. Tree icon changes to ✅
    Assistant offers: "[Start New Conversation]"
```

**Duration:** ~2-3 minutes  
**Result:** Article enhanced with vetted fragment content

---

### Flow 2: Iterative Refinement

**Scenario:** User refines AI suggestions before accepting

```
1-11. [Same as Flow 1 through draft creation]

12. User views AI draft v3.1
    └─ Too technical, too long

13. User returns to Assistant tab and types:
    "Make it less technical and more concise"

14. AI Working 🔄

15. Assistant:
    ✏️ Refining draft version v3.2...
       Simplifying language and reducing length
       ✓ Draft v3.2 created (1.5s)
    
    "I've simplified the content and reduced it to
    ~280 words. [View Draft]"

16. User reviews v3.2 - much better

17. User: "Add a note about diagram references"

18. AI creates v3.3 with diagram note

19. User reviews v3.3 - perfect!

20. User accepts draft
    └─ v3.3 becomes user version v4
    └─ Conversation complete ✅
```

**Duration:** ~5-7 minutes  
**Result:** Iteratively refined content before committing

---

### Flow 3: Multi-User Collaboration

**Scenario:** Two users collaborating on same article

```
1. John opens article, starts conversation
   └─ "Add OAuth details"

2. AI searches, presents approval interface

3. Sarah opens same article
   └─ Sees John's message
   └─ Sees AI working 🔄
   └─ Sees same conversation in real-time

4. AI responds with fragments/subjects

5. John clicks [Review]
   
6. Sarah's view updates - sees Review tab appear
   └─ Both users see same selection state

7. John unchecks some fragments

8. Sarah sees selections update in real-time
   └─ Adds comment: "Don't forget the security section"

9. John adjusts selections based on Sarah's input

10. John clicks [Create Draft]

11. Both users see:
    └─ Draft v3.1 created
    └─ New AI Draft tab appears
    └─ Can both review simultaneously

12. Sarah reviews first: "Looks good!"

13. John accepts draft
    └─ Both see article updated
    └─ Both see conversation complete ✅
```

**Duration:** ~4-6 minutes  
**Result:** Collaborative article improvement

---

### Flow 4: Cancellation & Resume

**Scenario:** User cancels mid-process and resumes later

```
1. User: "Add comprehensive authentication guide"

2. AI starts working (large search, many fragments)
   └─ Taking longer than expected

3. User realizes wrong article, clicks [Cancel]
   └─ Hangfire job killed
   └─ Conversation stays in incomplete state
   └─ Icon returns to 👤 Waiting for User

4. [Later] User returns to correct article

5. User: "Find fragments about basic auth only"

6. AI resumes from where it stopped
   └─ Previous incomplete conversation archived
   └─ New search with refined query

7. Continues normally...
```

**Result:** Graceful handling of interruptions

---

### Flow 5: No Results Handling

**Scenario:** Search finds no relevant fragments

```
1. User: "Add information about quantum encryption"

2. AI Working 🔄

3. Assistant:
   🔍 Searching fragments...
      Query: "quantum encryption"
      ✓ Search completed (0.3s)
   
   ⚠️ No relevant fragments found above 0.7
      similarity threshold.
   
   Try:
   • Different keywords
   • Broader search terms
   • Checking if fragments exist for this topic

4. User: "Try 'encryption' instead"

5. AI searches with new term
   └─ Finds 4 fragments
   └─ Continues normally

6. OR User: "Never mind, thanks"
   └─ Conversation ends
   └─ Can start new conversation later
```

**Result:** Helpful recovery from empty results

---

## Architecture & Data Model

### Database Schema Changes

#### ArticleVersion Entity (Enhanced)

```
ArticleVersion:
  Id                 (Guid, existing)
  ArticleId          (Guid, existing)
  ContentSnapshot    (string, existing)
  ContentDiff        (string?, existing)
  VersionNumber      (int, existing)
  CreatedById        (Guid?, existing)
  CreatedAt          (DateTimeOffset, existing)
  
  ParentVersionId    (Guid?, NEW) - Links to parent in chain
  VersionType        (enum, NEW)  - User | AI
  ChangeMessage      (string, NEW) - Commit-style message
  IsActive           (bool, NEW)   - Current in chain?
```

#### New Entity: ArticleConversation

```
ArticleConversation:
  Id                (Guid)
  ArticleId         (Guid, FK to Article)
  Status            (enum: Active | Complete | Cancelled)
  CreatedAt         (DateTimeOffset)
  CompletedAt       (DateTimeOffset?)
  CompletedByUserId (Guid?, FK to User)
  
  Messages          (ICollection<ConversationMessage>)
```

#### New Entity: ConversationMessage

```
ConversationMessage:
  Id                  (Guid)
  ConversationId      (Guid, FK to ArticleConversation)
  MessageType         (enum: User | Assistant | System | ToolExecution)
  Content             (string)
  UserId              (Guid?, FK to User) - null for Assistant
  CreatedAt           (DateTimeOffset)
  Metadata            (string?, JSON) - Tool results, etc.
```

#### New Entity: FragmentSubject (Join Table)

```
FragmentSubject:
  FragmentId        (Guid, FK to Fragment)
  SubjectId         (Guid, FK to Subject)
  ConversationId    (Guid, FK to ArticleConversation)
  Confidence        (double?)
  CreatedAt         (DateTimeOffset)
```

#### New Entity: Subject

```
Subject:
  Id                (Guid)
  Name              (string)
  Description       (string?)
  CreatedByType     (enum: System | LLM)
  CreatedAt         (DateTimeOffset)
```

### Service Layer

#### New: IArticleAssistantService

**Responsibilities:**
- Orchestrate agentic loop
- Manage tool calling
- Handle conversation state
- Coordinate with SignalR for real-time updates

**Key methods:**
- `ProcessUserMessageAsync(conversationId, message, userId)`
- `CancelConversationAsync(conversationId)`
- `GetConversationHistoryAsync(articleId)`
- `ApproveFragmentsAndSubjectsAsync(conversationId, fragmentIds, subjectIds)`

#### Enhanced: IArticleVersionService

**New methods:**
- `CreateAIDraftVersionAsync(articleId, content, parentVersionId, metadata)`
- `AcceptDraftVersionAsync(draftVersionId, userId)` - Promotes to user version
- `GetVersionChainAsync(versionId)` - Returns full chain
- `GenerateVersionMessageAsync(fromContent, toContent, context)` - Auto-message

#### New: IFragmentSubjectService

**Responsibilities:**
- Extract subjects from fragments (Phase 1: simple, Phase 2: LLM)
- Manage many-to-many relationships
- Validate selections

**Key methods:**
- `ExtractSubjectsAsync(fragments)` - Phase 1: uses Summary
- `ExtractSubjectsWithLLMAsync(fragments, articleContext)` - Phase 2: intelligent
- `ValidateSelectionsAsync(fragmentIds, subjectIds)` - Check consistency

### Background Jobs (Hangfire)

#### New: AgentConversationJob

**Purpose:** Run agentic loop in background

**Process:**
```
1. Receive user message
2. Load conversation context
3. Build AI prompt with:
   - Conversation history
   - Article content
   - Available tools
4. Call AI with tool definitions
5. If AI wants to use tool:
   a. Execute tool
   b. Send progress via SignalR
   c. Add result to conversation
   d. Continue loop (back to step 4)
6. If AI has final response:
   a. Save message to conversation
   b. Update conversation state
   c. Notify via SignalR
7. Handle errors gracefully
```

**Cancellation:** Job checks CancellationToken, stops cleanly

---

## User Interface Design

### Right Sidebar - Assistant Tab

#### Active Conversation View

```
┌──────────────────────────────────────────┐
│ 💬 Assistant          [📜 History (2)]   │
├──────────────────────────────────────────┤
│                                           │
│ John Smith                                │
│ Add OAuth implementation details          │
│                                           │
│ Assistant                                 │
│ 🔍 Searching fragments...                │
│    ✓ Found 5 fragments (0.3s)            │
│                                           │
│ I found 5 fragments covering 2 subjects. │
│                                           │
│ ┌────────────────────────────────────┐   │
│ │ 5 Fragments | 2 Subjects           │   │
│ │ Estimated: +450 words              │   │
│ │                                     │   │
│ │ [✓ Approve All] [📋 Review]        │   │
│ └────────────────────────────────────┘   │
│                                           │
│ ──────────────────────────────────────── │
│ Type a message...             [Send]     │
│                                           │
│ [+ New Conversation] [🗑️ Cancel]        │
└──────────────────────────────────────────┘
```

#### Empty State (No Active Conversation)

```
┌──────────────────────────────────────────┐
│ 💬 Assistant          [📜 History (2)]   │
├──────────────────────────────────────────┤
│                                           │
│                                           │
│           [Empty conversation area]       │
│                                           │
│                                           │
│ ──────────────────────────────────────── │
│ Type a message...             [Send]     │
└──────────────────────────────────────────┘
```

#### History Modal/Drawer

```
┌──────────────────────────────────────────┐
│ Conversation History               [×]   │
├──────────────────────────────────────────┤
│                                           │
│ ☑ Dec 30, 2024 - 2:30 PM (Complete)     │
│   Added OAuth implementation              │
│   5 messages | Draft v3.2 accepted       │
│   [View]                                  │
│                                           │
│ ☑ Dec 28, 2024 - 10:15 AM (Complete)    │
│   Enhanced security section               │
│   8 messages | Draft v2.1 accepted       │
│   [View]                                  │
│                                           │
│ ──────────────────────────────────────── │
│ [Close]                                   │
└──────────────────────────────────────────┘
```

### Left Sidebar - Tree View with Status Icons

```
Articles:
├─ 📄 Security Guide 🔄          (AI working)
├─ 📄 API Documentation 👤       (Waiting for user)
├─ 📄 User Manual ✅             (Conversation complete)
├─ 📄 Architecture Overview      (No conversation)
└─ 📄 Deployment Guide
```

**Icon appears next to article title in tree view**

### Center Pane - Dynamic Tabs

#### Normal Mode

```
┌──────────────────────────────────────────┐
│ 📄 Content                                │
├──────────────────────────────────────────┤
│                                           │
│ [Tiptap Editor - Editable]               │
│                                           │
└──────────────────────────────────────────┘
```

#### With AI Draft

```
┌──────────────────────────────────────────┐
│ 📄 Current Version | ✨ AI Draft (v3.2)  │
├──────────────────────────────────────────┤
│                                           │
│ [Content - Current: locked/readonly]     │
│ [Content - AI Draft: diff with highlights]│
│                                           │
│ [✓ Accept Draft] [✗ Discard Draft]      │
└──────────────────────────────────────────┘
```

#### With Review Selections

```
┌──────────────────────────────────────────┐
│ 📄 Current | 📋 Review Selections         │
├──────────────────────────────────────────┤
│                                           │
│ [Two-column fragment/subject selector]   │
│                                           │
└──────────────────────────────────────────┘
```

---

## Phase 1 vs Phase 2 Features

### Phase 1: MVP (Essential)

**Goal:** Validate core workflow, ship fast, learn from users

#### Features

✅ **Assistant Tab**
- Conversation UI with minimal empty state
- Real-time message updates via SignalR
- History access via button
- New conversation button

✅ **Agent Tools (4)**
- search_fragments
- create_draft_version
- get_article_content
- list_versions

✅ **Version Architecture**
- Parallel User/AI chains
- ParentVersionId linking
- VersionType enum
- Auto-generated commit messages
- Version numbering with chain prefix display

✅ **Approval Workflow**
- Fragment/subject selection (many-to-many architecture)
- Review pane with two columns
- Independent selection with auto-deselection
- Fragment preview in modal
- Phase 1 subjects: Fragment.Summary

✅ **Draft Management**
- Create AI draft versions
- Diff view in dedicated tab
- Accept/discard actions
- Lock current version during draft

✅ **Conversation Management**
- Per-article conversations
- Persistent (no expiry)
- Read-only history
- Conversation states with tree icons

✅ **Multi-User Support**
- Shared conversations
- Real-time collaboration
- Edit warning (soft, no lock)

✅ **Background Processing**
- Hangfire jobs for AI work
- Cancellable operations
- Progress via SignalR

✅ **Tool Transparency**
- Show all tool executions
- Real-time progress updates
- Checkmarks for completed steps

✅ **Error Handling**
- User-friendly messages
- Actionable suggestions
- No technical jargon

---

### Phase 2: Enhancements

**Goal:** Add intelligence, optimize UX, expand capabilities

#### Enhanced Subject Extraction

**LLM-Based Intelligent Clustering:**
- Analyze article context + fragments
- Multi-subject fragments (1 fragment → N subjects)
- Subject merging (reduce redundancy)
- Action-oriented subjects:
  - "Add OAuth implementation steps"
  - "Enhance security section with JWT details"
  - "Add diagram references"
- Context-aware (knows what's already in article)
- Rationale for each subject

#### Additional Agent Tools (20+)

**Content Analysis:**
- `analyze_article_gaps` - Find missing topics, weak sections
- `check_consistency` - Detect contradictions
- `estimate_reading_level` - Assess complexity
- `suggest_outline` - Recommend structure
- `identify_redundancy` - Find repetitive content

**Content Generation:**
- `rewrite_section` - AI rewrite with style guidance
- `generate_summary` - Create/update article summary
- `suggest_title` - Better title recommendations
- `expand_section` - Add depth to sparse sections
- `add_examples` - Generate relevant examples
- `create_outline_from_fragments` - Build structure

**Cross-Article Intelligence:**
- `find_related_articles` - Suggest articles to link
- `compare_articles` - Identify overlap
- `suggest_parent_article` - Recommend hierarchy
- `extract_reusable_content` - Find splittable content

**Metadata & Organization:**
- `suggest_tags` - Recommend tags
- `categorize_article` - Suggest article type
- `suggest_related_sources` - Find sources to review
- `identify_stakeholders` - Who to notify

**Advanced Version Management:**
- `compare_versions` - Detailed diff any two versions
- `revert_to_version` - Restore as new draft
- `merge_drafts` - Combine draft chains
- `suggest_next_steps` - Recommend what to work on

**Collaboration:**
- `notify_reviewers` - Suggest reviewers
- `generate_changelog` - Release notes
- `suggest_publication_readiness` - Ready to publish?

#### UX Enhancements

**Tool Transparency:**
- Collapsible detail view
- Summary mode by default
- Expandable for power users

**Conversation:**
- Message interruption/queuing
- Context window summarization
- Smart pruning for long conversations

**Review Pane:**
- Search/filter fragments
- Sort by relevance, date, confidence
- Category filtering
- Bulk operations

**Discovery:**
- Contextual tips and suggestions
- Example prompts
- Capability hints

**Onboarding:**
- First-time user tour
- Interactive walkthrough
- Sample article with guided flow

#### Intelligent Features

**Version Messages:**
- LLM-generated semantic messages
- User-editable commit messages
- Rich descriptions of changes

**Proactive Suggestions:**
- "I noticed your article mentions X but lacks Y..."
- Auto-suggest improvements
- Gap analysis on article open

**Smart Defaults:**
- Learn user preferences
- Personalized subject recommendations
- Adaptive thresholds

---

## Implementation Notes

### Technology Stack

**Backend:**
- ASP.NET Core (existing)
- Hangfire for background jobs
- SignalR for real-time communication
- AWS Bedrock for LLM (existing integration)
- PostgreSQL with pgvector (existing)

**Frontend:**
- Vue 3 (existing)
- Tiptap editor (existing)
- Bootstrap 5 (existing)
- SignalR client

### Key Integration Points

#### Existing Systems to Leverage

1. **Fragment System**
   - Fragment repository with vector search
   - Embedding generation (existing)
   - Similarity search (existing)

2. **Version System**
   - ArticleVersion entity (enhance)
   - ArticleVersionService (extend)
   - Version comparison rendering (reuse)

3. **SignalR Infrastructure**
   - ArticleHub (extend)
   - Real-time updates (existing pattern)

4. **AI Processing**
   - BedrockAiService (existing)
   - Tool calling with Microsoft.Extensions.AI
   - Structured output parsing

#### New Components to Build

1. **ArticleAssistantService**
   - Agentic loop orchestration
   - Tool calling framework
   - Conversation state management

2. **ConversationRepository**
   - CRUD for conversations
   - Message persistence
   - History retrieval

3. **FragmentSubjectService**
   - Subject extraction (Phase 1: simple)
   - Many-to-many relationship management
   - Selection validation

4. **AgentConversationJob**
   - Hangfire background job
   - Cancellation support
   - Progress reporting

5. **UI Components**
   - AssistantPanel.vue (enhanced from ChatPanel)
   - ReviewPane.vue (new)
   - ConversationHistory.vue (new)

### Development Approach

**Iteration 1: Core Infrastructure**
- Database schema changes
- Version architecture
- Basic conversation storage

**Iteration 2: Agent Tools**
- Tool framework
- search_fragments implementation
- create_draft_version implementation
- get_article_content, list_versions

**Iteration 3: Approval Workflow**
- Review pane UI
- Fragment/subject selection
- Many-to-many logic

**Iteration 4: UI Integration**
- Assistant panel enhancement
- Center pane tabs
- Tree view icons

**Iteration 5: Background Processing**
- Hangfire job implementation
- SignalR progress updates
- Cancellation support

**Iteration 6: Multi-User**
- Shared conversation sync
- Edit warnings
- Real-time collaboration

**Iteration 7: Polish**
- Error handling
- User messages
- Version commit messages
- Testing and refinement

---

## Success Metrics

### Phase 1 Success Criteria

**Adoption:**
- % of articles with at least one conversation
- Average conversations per article
- User retention (return to use Assistant)

**Effectiveness:**
- % of conversations resulting in accepted drafts
- Average fragments used per improvement
- Time from request to accepted draft

**Quality:**
- User satisfaction with suggested fragments
- Approval rate (fragments selected / fragments shown)
- Version message usefulness ratings

**Collaboration:**
- % of conversations with multiple participants
- Time to resolution in collaborative sessions

### Phase 2 Success Criteria

**Intelligence:**
- LLM subject extraction accuracy
- Reduction in user selection changes
- Proactive suggestion acceptance rate

**Productivity:**
- Time saved vs manual fragment search
- Articles improved per user per week
- Reduction in article editing time

**Advanced Tool Usage:**
- Adoption of new tools
- Most valuable tools (by usage)
- Tool combination patterns

---

## Future Vision

### Phase 3+: Advanced Intelligence

**Cross-Article Understanding:**
- Knowledge graph navigation
- Automatic linking of related content
- Duplication detection and merging
- Consistency checking across articles

**Autonomous Improvements:**
- Scheduled article review
- Automatic gap detection
- Proactive improvement suggestions
- Email digests of opportunities

**Team Workflows:**
- Review assignments
- Approval workflows
- Publishing pipelines
- Changelog generation

**Learning & Adaptation:**
- User preference learning
- Organization-specific prompts
- Custom tool creation
- Workflow templates

---

## Appendix: Design Rationale

### Why Parallel Version Chains?

**Alternative considered:** Single linear version history with draft flags

**Chosen approach:** Parallel chains (User + AI)

**Rationale:**
- Clear separation of official vs proposed changes
- Users always know what's "real" vs "suggested"
- Easy to discard entire AI chain without polluting history
- Supports iterative refinement (v3.1 → v3.2 → v3.3)
- Version panel stays clean (user versions only)
- Natural Git-like mental model (branches)

### Why Human-in-the-Loop Approval?

**Alternative considered:** AI directly modifies article, user reverts if needed

**Chosen approach:** User approves before changes applied

**Rationale:**
- Articles are high-stakes content (not like code completion)
- Users need confidence in AI suggestions
- Trust built through transparency
- Prevents surprising/unwanted changes
- Explicit control reduces anxiety
- Review workflow catches errors before commit

### Why Many-to-Many Fragments/Subjects?

**Alternative considered:** Hierarchical (subjects contain fragments)

**Chosen approach:** Many-to-many independent relationship

**Rationale:**
- Fragments can cover multiple topics (OAuth + Security)
- Subjects can span multiple fragments (comprehensive coverage)
- Flexible deselection (remove subject, keep some fragments)
- Future-proof for LLM clustering
- More accurate representation of knowledge
- Users can fine-tune at either level

### Why Single Active Conversation?

**Alternative considered:** Multiple parallel AI draft chains

**Chosen approach:** One active conversation per article at a time

**Rationale:**
- Simpler mental model
- Prevents confusion (which draft is which?)
- Easier state management
- Most users work on one improvement at a time
- Can always start new conversation after completing
- Reduces UI complexity

### Why Minimal Empty State?

**Alternative considered:** Example prompts, instructions, capabilities list

**Chosen approach:** Just input field

**Rationale:**
- Learn by doing is fastest
- Natural language input needs no tutorial
- Progressive disclosure teaches as you go
- Reduces cognitive load
- Cleaner UI
- Power users aren't slowed down
- Can add onboarding in Phase 2 if needed

---

## Document History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | Dec 30, 2024 | Initial design specification from comprehensive design session |

---

**End of Document**

