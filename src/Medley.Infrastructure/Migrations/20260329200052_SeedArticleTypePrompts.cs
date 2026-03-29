using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedArticleTypePrompts : Migration
    {
        private static readonly (string Name, string PlanPlaceholder, string AgentPlaceholder)[] ArticleTypes =
                [
                    ("How-To",          @"# **Planning Mode Prompt: How-to Articles**

How-to articles are **goal-oriented** documentation that guide competent users through solving specific real-world problems or accomplishing concrete tasks. Focus on **action** and practical work, not learning or explanation.

## **✅ MUST INCLUDE**

**Problem/Goal Focus**

- Specific, real-world problem or task from user''s perspective (not tool-centric)
- Clear title stating exactly what the guide shows: ""How to \[accomplish X\]""
- Assumes user knows what they want to achieve and why

**Action-Oriented Content**

- Executable solution as sequential steps with action verbs
- Conditional imperatives: ""If you want X, do Y. To achieve W, do Z.""
- Adaptable to real-world complexity - allow for user judgement
- Address thinking and decision-making, not just mechanical steps

**Practical Usability**

- Start and end at meaningful points (need not be complete end-to-end)
- Omit unnecessary details - practical usability over completeness
- Alert users to complexity, edge cases, and how to handle them
- Can fork, overlap, have multiple entry/exit points if needed

**Writing Quality**

- Focus exclusively on the task/problem - no digressions
- Brief inline context only - link to reference/explanation docs for depth
- Assumes competent user with basic knowledge

## **❌ MUST NOT INCLUDE**

- **Teaching content**: No conceptual explanations, lessons, or ""what is X"" sections (link to concept docs)
- **Reference material**: No exhaustive lists of options or technical specifications (link to reference docs)
- **Tool-centric guidance**: Not ""how to use feature X"" but ""how to solve problem Y (using feature X)""
- **Trivial step-by-step**: Don''t document obvious operations (turning on devices, clicking Deploy buttons)
- **Multiple unrelated goals**: One problem/task per article
- **Edge cases**: Avoid documenting boundaries of application capability
- **Tutorial content**: Not for learning/teaching - for getting work done

## **🚩 RED FLAGS**

1. Reads like a tutorial (teaching/learning focus) rather than task completion
2. Tool-centric (""use this feature"") not problem-centric (""solve this problem"")
3. Explains concepts instead of linking to explanations
4. Lists all possible options instead of recommending the best approach
5. Trivial steps anyone competent would know
6. Too rigid/linear when real-world requires judgement
7. Vague goal - not addressing specific user need
8. Multiple unrelated tasks in one guide

## **EVALUATION CHECKLIST**

- Does title clearly state specific problem/goal being solved?
- Written from user''s perspective (their problem), not tool''s perspective?
- Assumes competent user? (Not over-explaining basics)
- Pure action focus? (No explanatory digressions)
- Addresses real-world complexity appropriately?
- Links to reference/explanation docs rather than including them inline?

## **RECOMMENDATIONS**

**Add when missing:** Specific goal statement, conditional guidance, links to supporting docs, adaptation notes for real-world variance

**Remove or relocate:** Extended explanations → concept articles, option lists → reference docs, basic tutorials → tutorial section, multiple goals → split articles

---

**Core principle**: How-to guides serve competent users solving real problems. Action-only, goal-focused, adaptable to reality. Users know what they want; the guide shows them how to get it done.",
            @"# **Agent Mode Prompt: Authoring How-to Articles**

Write **goal-oriented** documentation that guides competent users through solving specific real-world problems or accomplishing concrete tasks.

## **Before You Begin**

### **Identify the Real Problem**

- Define one specific problem or task from the user''s perspective (not tool capabilities)
- Ensure it addresses actual user needs, not just system features
- Confirm it''s task-oriented (getting work done), not learning-oriented (tutorial)
- Verify problem is specific enough: ""how to integrate monitoring"" not ""how to monitor""

### **Validate Scope**

- Check it''s not better suited as tutorial (teaching/learning) or troubleshooting (fixing errors)
- Confirm target users have basic competence and know what they want to achieve
- Identify real-world complexity and decision points users will encounter

## **Structure Your How-to**

### **Title and Overview**

- Title format: ""How to \[specific goal\]"" using user language
- State the problem/goal clearly: what user will accomplish
- Brief context on when/why user would need this (1-2 sentences)

### **Prerequisites (if needed)**

- List required knowledge, tools, access, or setup
- Assume basic competence - don''t list obvious requirements
- Link to background information rather than explaining

### **Steps Section**

- Provide logical sequence of actions with imperative verbs
- Use conditional imperatives: ""If you want X, do Y""
- Allow for user judgement and adaptation - real problems aren''t always linear
- Include decision points and branching when necessary for real-world use
- Address thinking/strategy, not just mechanical actions
- Orient users before each step (where to be, what to have ready)

## **Writing Guidelines**

**Stay Focused:**

- Action only - no explanations, teaching, or reference material
- Link to concept docs for ""why"" and reference docs for complete options
- Remove anything not directly serving the goal
- One problem per guide

**Address Reality:**

- Alert to complexity, edge cases, and how to handle them
- Provide guidance for judgement calls, not just procedures
- Acknowledge when multiple approaches exist, recommend the best one

---

**Key:** Guide competent users through real problems with focused action. Problem-centric, not tool-centric. Link extensively to supporting docs."),
            ("Tutorial",        @"# **Tutorial Article - Planning Mode Instructions**

## **Article Purpose**

Tutorials are **learning-oriented experiences** that teach through doing. They build confidence and skills via guided, hands-on practice—not through explanation.

## **Must Include**

- **Clear learning objectives**: What skills/knowledge will users gain? (avoid ""you will learn..."")
- **Defined audience**: Who is this for? What prerequisite knowledge is required?
- **Concrete, actionable steps**: Each step should be imperative, specific, and produce visible results
- **Early and frequent results**: Users should see meaningful output at every step
- **Expected outcomes**: Tell users what they should see/experience at each stage
- **Narrative guidance**: Use first-person plural (""we will...""), maintain confidence throughout
- **Success-oriented path**: Single, reliable path to completion (no branching choices)
- **Minimal context**: Just enough background to start; link to detailed explanations elsewhere

## **Must NOT Include**

- **Explanations and theory**: Ruthlessly minimize. Link to reference docs instead
- **Options and alternatives**: Ignore them. One clear path only
- **Abstract concepts**: Stay concrete and specific to THIS task
- **Choices and decisions**: Don''t ask users to decide; guide them
- **Task-oriented how-to content**: Tutorials teach skills; how-tos complete tasks
- **Prerequisite information in steps**: Requirements belong in ""Before you begin""

## **Review Criteria**

When evaluating tutorial content, check:

- Does each step produce visible, meaningful results?
- Is explanation kept to 1-2 sentences maximum per concept?
- Are prerequisites clearly stated upfront?
- Is there exactly ONE path through (no ""alternatively"" or ""you could also"")?
- Do steps start with imperative verbs and express complete thoughts?
- Does it focus on DOING rather than UNDERSTANDING?
- Will a beginner following exactly achieve success every time?
- Is the scope appropriate (15-60 minutes to complete)?

## **Common Problems to Flag**

- Mixing tutorial with how-to guide (teaching vs. task completion)
- Too much explanation breaking the flow of doing
- Offering choices or optional paths
- Missing expected results or output examples
- Unreliable steps that might fail for some users
- Abstract or theoretical content instead of concrete practice
- Missing learning objectives or unclear audience
- Steps longer than 7 primary actions or 4 substeps",
            @"# **Agent Mode Prompt: Authoring Tutorial Articles**

## **Article Type**

Write **learning-oriented** experiences that teach skills through hands-on practice. Guide learners to success through concrete action, not explanation.

## **Core Principles**

- **Focus on doing, not understanding**: Learning happens through action and visible results. Explanation breaks the flow.
- **Build confidence through success**: Every step must work reliably. Design a single, tested path with no failure modes.
- **Be present as teacher**: Use first-person plural (""we will..."", ""let''s...""). Maintain narrative guidance about what to expect.

## **Structure**

**Title**: Direct and descriptive - ""Build a REST API with Node.js""

**Overview**: State what the learner will accomplish (not ""learn""). Define audience and prerequisites. List learning objectives using ""By the end, you''ll be able to \[verb\] \[specific skill\]""

**Before You Begin**: List all required tools, accounts, installations, configurations.

**Steps Section** - Write steps that produce visible results:

- Start with imperative verbs
- One clear path only - no alternatives, options, or choices
- Show expected results after each step
- Point out what to notice - direct attention to what matters
- Provide reassurance - ""If you see X, you''re on the right path""
- Minimize explanation ruthlessly - 1 sentence maximum, link to concept/reference docs
- Make steps repeatable where possible

**Summary**: List actual skills and knowledge gained. Celebrate accomplishments.

**Next Steps**: Link to related tutorials, how-tos, or concept docs.

## **What NOT to Include**

- Extended explanations - link to concept docs instead
- Options and alternatives - one path only
- Abstract concepts - stay concrete and specific
- Choices and decisions - you decide for learners
- Task-oriented how-to content - tutorials teach, how-tos solve problems

## **Writing Style**

**Use**: First-person plural (""We will...""), imperative for steps, expectations (""You should see...""), encouragement (""Notice that..."") **Avoid**: ""You will learn..."" (presumptuous), explanations over 1 sentence, patronizing words (""simply"", ""just""), second-guessing

---

**Remember**: Tutorials are lessons. Provide an experience that allows learning through doing, not explaining."),
            ("Reference",       @"# **Planning Mode Prompt: Reference Articles**

## **What is a Reference Article?**

An **information-oriented** technical description of product components. Focused on **describing** (information), not teaching or instructing. Provides structured, scannable facts users consult while working. Neutral, authoritative descriptions organized to mirror product structure.

## **✅ MUST INCLUDE**

### **Structure & Content**

- **Concise summary**: Brief description of what''s being referenced
- **Product-aligned structure**: Organization mirrors application''s actual structure
- **Scannable format**: Tables, lists, object schemas for quick lookup
- **Technical specifications**: Parameters, return values, data types, configurations, constraints
- **Examples**: Brief code examples illustrating usage (not teaching)
- **Compatibility**: OS, browser, version support where relevant

### **Style**

- **Active voice**: Clear, direct sentences with key information first
- **Neutral tone**: Objective, austere, factual - no opinions or marketing
- **Precise terminology**: Accurate terms matching product language
- **Brevity**: Essential details only

## **❌ MUST NOT INCLUDE**

- **Instructions/procedures**: No step-by-step or ""how to"" content (link to how-to guides)
- **Explanations**: No ""why it works"" conceptual teaching (link to concept docs)
- **Tutorials**: No learning exercises (link to tutorials)
- **Verbose descriptions**: Keep it austere and concise
- **Ambiguity**: No uncertainty or subjective language

## **Red Flags**

1. Step-by-step instructions or procedural content
2. Conceptual explanations instead of component descriptions
3. Tutorial-style learning content
4. Inconsistent structure or formatting
5. Marketing language or opinions
6. Missing specifications (parameters, types, constraints)
7. Verbose or conversational tone

## **Evaluation Questions**

1. **Pure description**: Describes neutrally without instructing or explaining?
2. **Structured**: Uses tables, lists, schemas for scannability?
3. **Complete**: Parameters, types, values, constraints documented?
4. **Product-aligned**: Structure mirrors product organization?
5. **Consistent**: Standard patterns throughout?

---

**Core principle**: Reference describes **what things are**, not how to use them or why. Neutral, complete, structured, scannable.",
            @"# **Agent Mode Prompt: Authoring Reference Articles**

Write **information-oriented** technical descriptions for user-facing features, commands, and settings. Provide neutral, scannable facts users consult while working.

## **Article Structure**

### **Title**

Match the component name as it appears in the product. Use noun form: ""Configuration Settings"", ""Command Reference"", ""Feature Specifications""

### **Summary (1-2 sentences)**

State what''s being described and its primary purpose. Be concise and factual.

### **Structured Content**

Organize using tables and lists:

- **Tables**: Use for settings, command arguments, compatibility matrices. Example columns: Name, Description, Required/Optional, Valid Values, Default, Example
- **Lists**: Use for command options, setting categories, feature flags
- **Command syntax**: Display CLI command structure and arguments
- **Configuration formats**: Show setting names, valid values, defaults

### **Examples (Optional)**

Provide brief examples showing command usage or configuration values. Keep minimal—illustrate syntax and format only.

### **Constraints & Warnings**

Document limitations, deprecated features, version requirements, compatibility issues. Use clear warning language when relevant.

## **Writing Guidelines**

### **Organize by Product Structure**

Match documentation hierarchy to product organization. Users should navigate docs the same way they navigate the product.

### **Use Standard Patterns Consistently**

Apply identical formats throughout: same table structure, same heading hierarchy, same ordering (alphabetical or by importance).

### **Write Neutral Descriptions**

State facts about features and behavior. No opinions, marketing, or subjective language. Describe what it is and what it does, period.

### **Keep It Austere**

Reference is consulted, not read. Essential information only. Active voice. Key details first, context second. No verbosity.

## **What NOT to Include**

❌ **Step-by-step instructions**: Link to how-to guides instead ❌ **Conceptual explanations**: Link to concept docs instead ❌ **Tutorials or learning content**: Link to tutorials instead ❌ **High-level introductions**: Reference describes specific components

---

**Remember**: Reference is a map of the product. Describe accurately, organize predictably, format scannably."),
            ("Concept",         @"# **Planning Mode Prompt: Concept Articles**

Concept articles are **understanding-oriented** documentation that explain ""why"" and provide context. They deepen comprehension without teaching tasks.

## **✅ MUST INCLUDE**

- **Clear definition**: Glossary-style definition with scope boundaries (what IS/ISN''T covered)
- **Context**: Why concept exists (design rationale, historical background, how it fits bigger picture)
- **Connections**: How concept relates to other concepts, broader system
- **Understanding aids**: Real-world examples/use cases, analogies (if clarifying), visual aids near top
- **Structure**: Inverted pyramid (overview → details), headings for scannability
- **Language**: Conversational tone, minimal jargon, clear and simple

## **❌ MUST NOT INCLUDE**

- **Step-by-step instructions**: No how-to content (link instead)
- **Reference material**: No API specs, parameter lists, technical specifications
- **Implementation details**: Focus on ""what/why"", not ""how to do it""
- **Multiple unrelated concepts**: One concept per document
- **Overwhelming visuals**: Use diagrams strategically, not excessively

## **🚩 RED FLAGS**

1. Contains procedural instructions (belongs in how-to/tutorial)
2. Technical reference material or API docs
3. Multiple unrelated concepts without connections
4. No clear definition or scope
5. Missing ""why"" or background context
6. Heavy unexplained jargon
7. Implementation code examples (unless illustrating concept)
8. Confusing analogies
9. Missing connection to broader system

## **QUICK CHECKS**

- **Definition clear?** Scope boundaries defined?
- **Understanding-oriented?** Explains ""why/what"", not ""how to""? Has background/rationale?
- **Clarity?** Appropriate for audience? Analogies help? Visuals clarify? Jargon minimal?
- **Structure?** Inverted pyramid? Single concept? Separated from how-to/reference?

## **RECOMMENDATIONS**

**Add**: Definition/scope, background, use cases, visual aid, links to how-to/reference docs **Remove**: Instructions → how-to guides, specs → reference, multiple concepts → split, confusing metaphors **Improve**: Add ""why"", connect to known concepts, simplify language, add practical examples

---

**Core principle**: Explain ""why"" things are the way they are, helping readers understand without performing tasks.",
            @"# **Agent Mode Prompt: Authoring Concept Articles**

Write **understanding-oriented** articles explaining ""why"" and providing context. Build mental models, not task completion.

## **Research First**

- Map connections to other concepts and broader system
- Gather background: design decisions, historical context, constraints
- Review support for common ""What is?"" and ""Why?"" questions
- Define clear scope boundaries

## **Structure**

**Title:** ""Overview of \[concept\]"", ""About \[concept\]"", or ""\[Concept\]"" as noun (avoid vague ""Introduction"")

**Intro (1-2 para, optional):** Inverted pyramid—start high-level, establish relevance

**Definition (required):**

- Glossary-style definition
- Explicit scope: what IS/ISN''T covered
- How concept fits bigger picture
- Links to related concepts
- Problem-solution framing
- Analogies if they bridge knowledge gaps

**Visual Aid (near top):** Context diagram, flowchart, or system overview under definition

**Background (optional):** Historical context, design decisions, alternatives, constraints

**Use Cases:** Real-world scenarios showing concept applied; use storytelling sparingly

**Comparison (optional):** Table comparing implementations/versions/types

**Related Resources (optional):** Grouped links (3-5 per group)—How-to''s, Concepts, External

## **Writing Rules**

- Explain ""what/why"", never ""how to""
- One concept per document
- No step-by-step instructions (link to how-to)
- No reference material (link instead)
- Inverted pyramid structure (overview → details)
- Layer explanations: high-level then technical depth
- Conversational tone, minimal jargon
- Universal analogies only (enhance clarity, don''t complicate)

---

**Key:** Explain ""why"" things exist. Illuminate understanding through context, not task instruction."),
            ("FAQ",             @"# **Planning Mode Prompt: FAQ Articles**

FAQ articles are **question-oriented** documentation that provide concise, direct answers to common user questions. They anticipate user needs and offer quick, scannable responses without deep dives into concepts or procedures.

## **✅ MUST INCLUDE**

**Question Quality**

- Real user questions from support tickets, forums, user feedback, or analytics
- Clear, natural question phrasing (how users actually ask, not how we think they should)
- Searchable language matching user vocabulary
- Questions users frequently or commonly ask (not edge cases)

**Answer Quality**

- Concise, direct answers (2-4 paragraphs maximum per question)
- Plain language appropriate for the audience
- Links to detailed documentation for deeper exploration
- Context when needed (brief explanation of ""why"" if clarifying)

**Organization**

- Logical grouping or categorization when multiple FAQs exist
- Scannable format (clear question headings)
- Most common/important questions near the top

## **❌ MUST NOT INCLUDE**

- **Lengthy explanations**: Save conceptual teaching for concept docs (link instead)
- **Step-by-step instructions**: How-to procedures belong in how-to guides (link instead)
- **Technical specifications**: Reference material belongs in reference docs (link instead)
- **Troubleshooting procedures**: Detailed problem-solving belongs in troubleshooting guides (link instead)
- **Hypothetical questions**: Questions users don''t actually ask
- **Marketing content**: Sales language, promotional material, or feature advocacy
- **Redundancy**: Questions better answered by existing docs (link to them)

## **🚩 RED FLAGS**

1. Questions not based on actual user inquiries or data
2. Answers exceeding 4-5 paragraphs (becoming mini-articles)
3. Missing links to related how-to, concept, or reference documentation
4. Step-by-step instructions embedded in answers
5. Poor categorization or no logical grouping
6. Technical jargon without plain-language explanation
7. Questions that duplicate existing documentation without linking to it
8. Answers that assume too much prior knowledge
9. Questions so specific they apply to &lt;5% of users

## **EVALUATION CHECKLIST**

- **Evidence-based?** Questions derived from real user data (support, forums, analytics)?
- **Appropriately scoped?** Answers concise (2-4 paragraphs) without becoming tutorials?
- **Well-linked?** Links to detailed docs for users who need more?
- **Scannable?** Clear question format, logical grouping, easy navigation?
- **Right format?** Content actually suited for FAQ vs. another doc type?
- **User language?** Questions phrased how users ask them, not internal terminology?

## **RECOMMENDATIONS**

**Add**: Links to detailed documentation, question categorization, plain-language explanations for jargon, evidence of user demand

**Remove**: Lengthy explanations → concept docs, procedures → how-to guides, specs → reference docs, hypothetical questions → delete, marketing language → neutralize

**Improve**: Simplify verbose answers, rephrase questions in user language, add missing links, group related questions, prioritize by frequency

---

**Core principle**: Answer the questions users actually ask, concisely and clearly, with links to deeper documentation for those who need it.",
            @"# **Agent Mode Prompt: Authoring FAQ Articles**

Write **question-oriented** documentation that provides concise answers to common user questions.

## **Before You Begin**

- Review support tickets, forums, and analytics for frequently asked questions
- Consult support team about recurring questions
- Validate questions are FAQ-appropriate (not how-to, troubleshooting, or concept deep-dives)
- Check existing docs to avoid duplication

## **Structure Your FAQ**

**Questions:**

- Write as users ask them, using natural language
- Make searchable with common vocabulary
- Keep specific and concrete

**Answers:**

- Start with direct answer in first 1-2 sentences
- Limit to 2-4 short paragraphs maximum
- Link to detailed docs for deeper information
- Provide brief context only when essential

**Organization:**

- Group related questions under category headings
- Order by frequency (most common first)
- Maintain consistent Q&A formatting

## **Writing Guidelines**

**Be Direct:**

- Answer immediately, no preamble
- Use plain language for your audience
- Stay neutral and factual

**Be Concise:**

- If answer exceeds 4 paragraphs, link to how-to instead
- Don''t duplicate existing docs - link to them
- Stop when question is answered

**Link Strategically:**

- How-to guides for procedures
- Concept docs for understanding
- Reference docs for specifications
- Troubleshooting for problem-solving

---

**Key:** Answer real user questions concisely with links to detailed docs."),
            ("Troubleshooting", @"# **Planning Mode Prompt: Troubleshooting Articles**

## **Article Purpose**

Troubleshooting articles help users resolve specific problems they encounter with the product. They are problem-oriented, not learning-oriented.

## **Essential Elements to Include**

### **Problem Identification**

- Clear problem statement that users can recognize within seconds
- Specific symptoms users experience (presented as questions/bullet points)
- Error messages or observable behavior

### **Solutions**

- Step-by-step numbered instructions
- Multiple solutions if applicable, ordered by likelihood of success
- Bold formatting for UI elements and technical terms
- Links to related resources
- Prerequisites or conditions that must be met

### **Evidence-Based Content**

- Solutions based on actual support tickets and user issues
- Tested solutions that work across all relevant platforms
- Input from support team or SMEs

## **What to Exclude**

### **Inappropriate Content**

- Tutorial or learning content (belongs in tutorials, not troubleshooting)
- Feature explanations (belongs in how-to guides or reference)
- Theoretical problems not reported by actual users
- Patronizing language (""easy"", ""simple"", ""just"")

### **Over-Documentation**

- Excessive images when text sufficiently describes steps
- Overly detailed explanations that obscure the solution
- Information about product architecture unless directly relevant to the fix

## **Review Recommendations**

When evaluating troubleshooting articles, recommend:

- Moving tutorial content to appropriate tutorial articles
- Consolidating redundant troubleshooting steps across multiple articles
- Adding links to support tickets or common user issues
- Simplifying complex solutions into clear numbered steps
- Removing outdated solutions for deprecated features
- Adding missing symptoms that users might experience
- Ensuring solutions are in active voice and chronological order",
            @"# **Agent Mode Prompt: Authoring Troubleshooting Articles**

## **Article Type**

Write **problem-solving** guides that help users resolve specific errors or issues. Focus on diagnosis and resolution, not learning or explanation.

## **Core Principles**

- **Evidence-based**: Only document problems from actual user reports (support tickets, forums, reviews)
- **Immediate recognition**: Users identify their problem within seconds
- **Solution-focused**: Get to resolution quickly; link to concepts if deeper explanation needed

## **Structure**

### **Title**

Use specific error or problem statement:

- ""Can''t Upload Photos to Profile""
- ""My Account Settings Won''t Save""

### **Introduction (2-3 sentences)**

State the problem clearly. What''s broken? When does it occur?

### **Symptoms**

List observable signs as bullet points or questions:

- ""The **Save** button is grayed out""
- ""Does your profile photo disappear after uploading?""

### **Solution(s) - Numbered Steps**

1. **Imperative verbs**: ""Click **Save Changes**"", ""Clear your browser cache""
2. **Bold UI elements**: Navigate to **Settings** &gt; **Profile** &gt; **Edit**
3. **One action per step**
4. **Include verification**: ""You should see a success message""
5. **Multiple solutions**: Order by likelihood of success

### **Writing Style**

- Active voice: ""Clear your cache"" not ""Cache should be cleared""
- Present tense: ""This saves..."" not ""This will save...""
- Chronological order
- Specific instructions where possible

## **What NOT to Include**

- Tutorial/learning content
- Feature explanations (link to concept docs)
- All possible causes (focus on common issues)
- Patronizing language (""simply"", ""just"", ""easy"")
- Excessive images (use only when text insufficient)

## **Checklist**

- ✅ Problem from actual user reports
- ✅ Clear, recognizable symptoms
- ✅ Numbered solution steps
- ✅ Active voice, present tense
- ✅ Verification included
- ✅ Tested on all platforms"),
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // article_type_id is resolved by name at migration time since the IDs were
            // generated dynamically when article types were seeded.
            foreach (var (name, planContent, agentContent) in ArticleTypes)
            {
                // ArticleTypePlanMode = 7
                migrationBuilder.Sql($@"
                    INSERT INTO ai_prompts (id, type, content, article_type_id, last_modified_at, created_at, last_synced_with_cursor)
                    SELECT gen_random_uuid(), 7, '{planContent}', at.id, NOW(), NOW(), NULL
                    FROM article_types at
                    WHERE at.name = '{name}'
                    AND NOT EXISTS (
                        SELECT 1 FROM ai_prompts ap WHERE ap.type = 7 AND ap.article_type_id = at.id
                    );
                ");

                // ArticleTypeAgentMode = 8
                migrationBuilder.Sql($@"
                    INSERT INTO ai_prompts (id, type, content, article_type_id, last_modified_at, created_at, last_synced_with_cursor)
                    SELECT gen_random_uuid(), 8, '{agentContent}', at.id, NOW(), NOW(), NULL
                    FROM article_types at
                    WHERE at.name = '{name}'
                    AND NOT EXISTS (
                        SELECT 1 FROM ai_prompts ap WHERE ap.type = 8 AND ap.article_type_id = at.id
                    );
                ");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ai_prompts
                WHERE type IN (7, 8)
                AND article_type_id IN (
                    SELECT id FROM article_types
                    WHERE name = ANY(ARRAY['Index','How-To','Tutorial','Reference','Concept','FAQ','Troubleshooting'])
                );
            ");
        }
    }
}
