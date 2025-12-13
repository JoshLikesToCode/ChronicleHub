# Prompt and Reason

Execute a task requested by the user and log the reasoning and changes to Reasoning.md.

## Instructions

1. Ask the user: "What would you like me to do?"

2. After receiving their response, complete the requested task fully.

3. Once the task is complete, ask the user: "Task complete. Are you ready to append this to Reasoning.md?"

4. After user confirms, append an entry to `Reasoning.md` in the project root with the following format:

```markdown
## [2-3 Word Title Summarizing the Task]

[Concise 2-4 sentence summary of what changes were made and why]

**Files Changed:**
- path/to/changed/file1.ext
- path/to/changed/file2.ext

**Files Added:**
- path/to/new/file1.ext
- path/to/new/file2.ext

**Timestamp:** YYYY-MM-DD HH:MM:SS UTC

---
```

5. Create Reasoning.md if it doesn't exist yet, with a header:
```markdown
# ChronicleHub Development Reasoning Log

This file tracks all prompts and code changes made during development.

---
```

6. When appending entries:
   - Use UTC timestamp
   - Keep the title concise (2-3 words max)
   - List only files actually changed or added during this specific task
   - Omit "Files Changed" or "Files Added" sections if none apply
   - Append new entries at the END of the file (newest entries at bottom)
