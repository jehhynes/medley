# Article Assistant - Feature Specification

## Overview

The Article Assistant is an intelligent, conversational interface that helps users improve their articles by finding and incorporating relevant knowledge from organizational fragments. Rather than manually searching through sources and fragments, users can simply describe what they need, and the Assistant will find relevant information, propose improvements, and create enhanced draft versions for review.

Think of it as having a knowledgeable colleague who remembers everything your organization has discussed and documented, and can help you weave that knowledge into coherent articles.

## Core Philosophy

The Assistant operates on three key principles:

**1. Conversation Over Commands**  
Users interact through natural conversation rather than clicking through menus and forms. Simply describe what you want to achieve, and the Assistant figures out how to help.

**2. Human Control with AI Assistance**  
The Assistant proposes and suggests, but never makes final decisions. Users review and approve all changes before they're applied to articles. Every modification creates a version snapshot that can be reviewed or reverted.

**3. Transparency and Trust**  
The Assistant shows its work - what fragments it found, why it selected them, and what changes it proposes. Users can see the reasoning behind every suggestion and make informed decisions.

## How It Works

### Starting a Conversation

When working on an article, users see an "Assistant" tab in the right sidebar (alongside the existing Versions tab). Opening this tab reveals a simple conversation interface - just an input field where users can describe what they need.

For example, a user might type:
- "Find information about OAuth implementation and add it to the security section"
- "Add more technical details about authentication"
- "Enhance this article with examples from our meeting notes"

The Assistant understands these natural requests and takes appropriate action.

### Finding Relevant Knowledge

When asked to improve an article, the Assistant searches through the organization's fragment library using semantic similarity - it understands meaning, not just keywords. If you ask about "user authentication," it will find fragments about OAuth, security tokens, login flows, and related topics even if they don't use those exact words.

The search happens in the background while the Assistant keeps users informed:
- "Searching fragments..."
- "Found 8 relevant fragments"
- "Analyzing subjects..."

### Review and Approval

This is where the Assistant's approach becomes powerful. Rather than immediately adding content to the article, the Assistant presents what it found for review.

A new "Review Selections" tab opens in the center of the screen, showing two lists side by side:

**Fragments** (left side):  
Individual pieces of knowledge found across sources - meeting notes, documents, specifications, etc. Each fragment shows:
- Its title and summary
- Where it came from
- How relevant it is (similarity percentage)
- Which subjects it relates to

**Subjects** (right side):  
Topics or themes identified in the fragments. Initially, these are simply the fragment summaries. In future releases, the Assistant will intelligently group fragments by theme, identify what's missing from the current article, and suggest specific improvements like "Add OAuth implementation steps" or "Enhance security section with JWT handling."

Everything starts selected by default, but users can uncheck any fragments or subjects they don't want included. The lists are connected - if you deselect all fragments related to a subject, that subject is automatically deselected, and vice versa. This makes it easy to exclude entire themes or cherry-pick specific pieces of knowledge.

Users can preview any fragment in detail by clicking on it, seeing the full content in a modal window just like when browsing sources.

### Creating the Draft

Once satisfied with the selection, users click "Create Draft." The Assistant:
- Composes the selected fragments into coherent additions
- Creates a new draft version of the article
- Generates a descriptive message explaining what changed (like "Added OAuth implementation details from 3 fragments (+450 words)")
- Shows the draft in a new tab for review

### Reviewing Changes

The center pane now shows two tabs:

**Current Version** - The existing article content (now locked to prevent conflicting edits)

**AI Draft** - The proposed new version with changes highlighted:
- Additions shown in green
- Deletions shown with strikethrough
- Modified sections highlighted in yellow

This uses the same diff visualization as the existing version comparison feature, making it familiar and easy to understand what's changing.

### Refining or Accepting

At this point, users have several options:

**Continue the conversation** to refine the draft:
- "Make it less technical"
- "Add more examples"
- "Remove the section about token expiration"

Each refinement creates a new version in the draft chain (v3.1, v3.2, v3.3) without affecting the main article. Users can iterate until the draft is exactly right.

**Accept the draft** when satisfied:
- The draft becomes the new current version
- A version snapshot is created with the AI-generated change message
- The article content updates
- The interface returns to normal editing mode
- The conversation is marked as complete

**Discard the draft** if it's not helpful:
- The draft is abandoned
- The original article remains unchanged
- Users can start a new conversation

## Conversation Management

### Per-Article Conversations

Each article has its own conversation thread with the Assistant. Switching between articles switches conversations - they're completely independent. This means you can have the Assistant working on improving multiple articles simultaneously, each with its own context and history.

### Conversation States

Articles show their conversation status in the tree view with small icons:

**🔄 AI Working** - The Assistant is actively searching for fragments or creating drafts  
**👤 Waiting for Response** - The Assistant has asked a question or is waiting for approval  
**✅ Complete** - A draft has been accepted and the conversation has concluded  
**(No icon)** - No active conversation

This makes it easy to see at a glance which articles have Assistant work in progress.

### Conversation History

All conversations are preserved forever. When you open an article with a completed conversation, the Assistant panel shows a clean slate ready for a new conversation. A "History" button provides access to past conversations, which can be reviewed but not edited - they're a permanent record of how the article evolved.

Users can only start a new conversation after the previous one is complete (draft accepted or discarded). This prevents confusion from having multiple parallel improvement efforts on the same article.

### Starting Fresh

After a conversation is complete, users see a "New Conversation" button. Clicking this begins a fresh conversation with clean context. The Assistant remembers the article content but doesn't carry over the previous conversation's context, allowing for new directions and approaches.

## Multi-User Collaboration

The Assistant enables real-time collaboration. Multiple people can work together on improving an article:

### Shared Conversations

All users working on the same article see the same conversation. When someone sends a message, everyone sees it. When the Assistant responds, everyone sees the response. Messages show who sent them:

```
John Smith
Add OAuth implementation details

Assistant
Searching fragments...
Found 5 relevant fragments

Sarah Chen
Make it less technical
```

This creates a collaborative workspace where team members can discuss improvements with each other and the Assistant simultaneously.

### Coordinated Workflows

When an AI draft exists for an article, the system prevents confusion through warnings. If someone tries to edit the article content directly while reviewing a draft, they see a friendly warning:

"An AI assistant draft is currently in progress. Editing now may cause confusion when the draft is applied. We recommend reviewing and approving or rejecting the AI draft first."

Users can still edit if they choose, but the warning helps coordinate work and prevent conflicts. There's no hard lock - the system trusts users to make good decisions.

## Version Management

### Automatic Version History

Every change to an article creates a version snapshot with an automatically generated description of what changed. This happens whether users edit directly or accept Assistant-created drafts.

Version descriptions read like commit messages in version control:
- "Added OAuth implementation details from 3 fragments (+450 words)"
- "Enhanced security section with JWT best practices (+180 words)"  
- "Edited introduction and added code examples"
- "Initial article creation"

This makes version history meaningful and searchable - users can quickly understand what changed in each version without reading through detailed diffs.

### Two Types of Versions

**User Versions** appear in the Versions panel and represent the official history of the article. These are the versions that matter for tracking article evolution over time.

**AI Draft Versions** exist temporarily during the improvement process but aren't shown in the version history. They live within the conversation as artifacts of the collaborative process. Once a draft is accepted, it becomes a new user version. If discarded, the draft versions disappear.

This separation keeps version history clean and focused on meaningful milestones while still providing full traceability during the improvement process.

### Version Chains

Behind the scenes, versions form a tree structure. When the Assistant creates a draft based on version 3 and then refines it twice, you get a chain: v3 → v3.1 → v3.2 → v3.3. The display automatically shows this relationship, making it clear these are iterations on the same base version.

When the draft is accepted, it becomes v4 - a new user version that continues the main timeline.

## User Experience Details

### Simple, Focused Interface

The Assistant panel is deliberately minimal. When there's no active conversation, you just see an empty input field. No clutter, no overwhelming options, no complex menus. Type what you need and press send.

The interface reveals capabilities progressively as users interact with it. After your first successful fragment search, you might see a tip: "I can refine searches by asking for specific topics or categories." After creating your first draft: "You can ask me to make changes before approving the draft."

This learn-by-doing approach avoids overwhelming new users while helping them discover capabilities naturally through use.

### Helpful Error Messages

When things don't work as expected, the Assistant provides friendly, actionable guidance:

"I couldn't find any fragments matching 'quantum encryption.' Try different keywords or broader search terms, or check if fragments exist for this topic."

"I encountered an issue while searching. Please try again. If the problem persists, contact support."

No technical jargon, no confusing error codes - just clear explanation and next steps.

### Canceling Work in Progress

If the Assistant is taking too long or heading in the wrong direction, users can cancel the current operation. A "Cancel" button stops the background processing immediately. The conversation remains intact at its current state, so users can send a new message to try a different approach or simply abandon that line of work.

### Mobile Experience

On smaller screens, the review interface adapts to a vertical stacked layout rather than side-by-side columns. Fragments and subjects appear one after another, making it easy to scroll through and make selections even on tablets or phones.

## What the Assistant Can Do

### Phase 1 Capabilities (Initial Release)

The first release focuses on the core improvement workflow:

**Search for Fragments**  
Find relevant knowledge based on semantic similarity to user requests. Understands context and meaning, not just keywords.

**Create Draft Versions**  
Compose selected fragments into a coherent draft article version with proper formatting and attribution.

**Review Article Content**  
Read and understand the current article to provide context-aware suggestions.

**Show Version History**  
Display the article's version timeline with descriptive change messages, making it easy to understand how the article evolved.

### Phase 2 Enhancements (Future)

Future releases will dramatically expand the Assistant's intelligence and capabilities:

**Intelligent Subject Clustering**  
Rather than simple one-to-one fragment-to-subject mapping, the Assistant will use AI to identify meaningful themes across fragments, recognize what's already in the article versus what's missing, and make specific recommendations like "Add implementation steps for OAuth flow" or "Enhance token security section with best practices from three meeting discussions."

**Content Analysis**  
- Identify gaps and weak areas in articles
- Find contradictions or inconsistent information
- Assess reading level and complexity
- Suggest structural improvements
- Detect redundancy that should be consolidated

**Advanced Content Generation**  
- Rewrite sections with style guidance
- Generate or update summaries automatically
- Suggest improved titles
- Expand sparse sections with detail
- Create relevant examples for abstract concepts
- Build article outlines from collections of fragments

**Cross-Article Intelligence**  
- Find related articles worth linking to
- Identify overlap between articles
- Suggest where articles fit in the hierarchy
- Extract content worth splitting into separate articles

**Organization and Metadata**  
- Recommend appropriate tags
- Suggest article categorization
- Identify relevant sources to review
- Determine stakeholders who should be notified

**Advanced Version Management**  
- Compare any two versions with detailed analysis
- Restore previous versions as new drafts
- Suggest what to work on next based on article state

**Collaboration Features**  
- Recommend reviewers for changes
- Generate release notes for published versions
- Assess publication readiness

## Design Principles in Practice

### Safety Through Versioning

Nothing the Assistant does is permanent until users explicitly approve it. Every change creates a version snapshot, and users can always revert to any previous version. This safety net encourages experimentation - try new approaches knowing you can always go back.

### Transparency Builds Trust

In the initial release, the Assistant shows every step of its work: searching, finding fragments, analyzing subjects, creating drafts. Users can see exactly what it's doing and why. Future releases may simplify this display for efficiency, but the foundation is full transparency that builds user trust and understanding.

### Progressive Complexity

The feature starts simple - just a text input - and reveals capabilities through use. First-time users aren't overwhelmed with possibilities. Experienced users discover advanced capabilities naturally as their needs evolve.

### Collaboration-First

The Assistant isn't just a personal tool - it's designed for team collaboration from the ground up. Shared conversations, visible status indicators, and coordination mechanisms make it natural for multiple people to work together on improving articles.

## Success Scenarios

### Scenario 1: Quick Enhancement

Maria is writing a technical architecture document and realizes she needs to add details about the authentication system. She opens the Assistant and types: "Add information about our OAuth implementation."

The Assistant searches and finds 5 relevant fragments from specification documents and meeting notes. After reviewing and deselecting one that seems outdated, Maria clicks "Create Draft." Within seconds, she's reviewing a new version of her document with 400 words of OAuth implementation details properly integrated.

She notices it's too technical for her audience, so she tells the Assistant: "Make this less technical and more focused on the business benefits." The Assistant revises the draft. Satisfied, Maria clicks "Accept" and continues working on other sections.

Total time: 3 minutes. Previous approach of searching sources, reading fragments, and manually copying content: 30+ minutes.

### Scenario 2: Collaborative Improvement

The product team is collaborating on their quarterly strategy document. John starts a conversation with the Assistant: "Find our recent discussions about market positioning."

The Assistant presents 8 fragments from various strategy meetings. While John is reviewing these, Sarah joins and sees the conversation in progress. She adds: "Also include the competitive analysis from last month."

The Assistant searches again and adds 3 more fragments to the selection. Together, John and Sarah review the 11 fragments, discuss which are most relevant, deselect a few, and approve the rest.

The Assistant creates a draft. John reviews it and asks for refinements: "Emphasize the enterprise market more." The Assistant revises. Sarah reviews the second draft and accepts it.

Both team members contributed to improving the document through natural collaboration, with the Assistant serving as a shared tool that enhanced their teamwork rather than replacing human judgment.

### Scenario 3: Iterative Refinement

Alex is updating the company's security handbook. He starts broadly: "Improve the authentication section with our latest security practices."

The Assistant finds 12 fragments covering various security topics. Alex reviews them and realizes it's too broad. He deselects most and focuses on just the OAuth-related fragments. The Assistant creates a draft.

Looking at the draft, Alex realizes he wants more: "Add the two-factor authentication fragments too." The Assistant searches specifically for 2FA content, presents it, and creates an updated draft building on the previous one.

After two more rounds of refinement - first making it more concise, then adding specific examples - Alex has exactly the section he envisioned, assembled from organizational knowledge he didn't have to manually hunt down.

The conversation history provides a complete record of how this section evolved and why certain content was included.

## Summary

The Article Assistant transforms how users create and improve documentation by:

- **Eliminating tedious manual search** through sources and fragments
- **Providing AI assistance with human control** - suggestions, not dictates
- **Enabling natural collaboration** through shared conversations
- **Maintaining complete safety** through version history and approval workflows
- **Building trust through transparency** - showing all work and reasoning
- **Growing with users** through progressive disclosure of capabilities

It's designed to feel like working with a knowledgeable colleague who has perfect memory of everything your organization has discussed and documented, can find relevant information instantly, and can help compose that information into coherent content - but who always defers to human judgment for final decisions.

The initial release provides the essential workflow: find, review, approve, and iterate. Future enhancements will add increasingly sophisticated analysis and generation capabilities while maintaining the same simple, conversational interface.
