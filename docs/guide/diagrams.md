---
title: Mermaid Diagrams
order: 3
---

# Mermaid Diagrams

MokaDocs has built-in support for [Mermaid](https://mermaid.js.org/) diagrams. Mermaid lets you create diagrams and visualizations using a text-based syntax directly in your Markdown files. Diagrams are rendered client-side using the Mermaid.js library loaded from a CDN.

## Basic Usage

To create a diagram, use a fenced code block with `mermaid` as the language identifier:

````markdown
```mermaid
flowchart LR
    A[Start] --> B{Decision}
    B -->|Yes| C[Action]
    B -->|No| D[End]
```
````

```mermaid
flowchart LR
    A[Start] --> B{Decision}
    B -->|Yes| C[Action]
    B -->|No| D[End]
```

MokaDocs automatically detects `mermaid` code blocks and renders them as interactive SVG diagrams instead of displaying the raw syntax.

## Theme Support

Mermaid diagrams automatically adapt to the current color scheme of your documentation site. When a user switches between light and dark mode, diagrams re-render with appropriate colors and contrast levels. No additional configuration is needed.

## Diagram Types

Mermaid supports a wide variety of diagram types. Below are examples of the most commonly used types in technical documentation.

### Flowchart

Flowcharts describe processes and workflows with nodes and directional edges.

````markdown
```mermaid
flowchart TD
    A[User Request] --> B{Authenticated?}
    B -->|Yes| C[Load Dashboard]
    B -->|No| D[Show Login]
    D --> E[Enter Credentials]
    E --> F{Valid?}
    F -->|Yes| C
    F -->|No| G[Show Error]
    G --> D
    C --> H[Display Data]
```
````

```mermaid
flowchart TD
    A[User Request] --> B{Authenticated?}
    B -->|Yes| C[Load Dashboard]
    B -->|No| D[Show Login]
    D --> E[Enter Credentials]
    E --> F{Valid?}
    F -->|Yes| C
    F -->|No| G[Show Error]
    G --> D
    C --> H[Display Data]
```

Flowchart direction options:
- `TD` or `TB` — Top to bottom
- `BT` — Bottom to top
- `LR` — Left to right
- `RL` — Right to left

Node shapes:
- `[Text]` — Rectangle
- `(Text)` — Rounded rectangle
- `{Text}` — Diamond (decision)
- `([Text])` — Stadium
- `[[Text]]` — Subroutine
- `[(Text)]` — Cylinder (database)
- `((Text))` — Circle

### Sequence Diagram

Sequence diagrams show interactions between participants over time. They are excellent for documenting API flows, service communication, and protocol exchanges.

````markdown
```mermaid
sequenceDiagram
    participant Client
    participant API
    participant Auth
    participant DB

    Client->>API: POST /api/login
    API->>Auth: Validate credentials
    Auth->>DB: Query user record
    DB-->>Auth: User data
    Auth-->>API: JWT token
    API-->>Client: 200 OK + token

    Client->>API: GET /api/data (Bearer token)
    API->>Auth: Verify token
    Auth-->>API: Token valid
    API->>DB: Fetch data
    DB-->>API: Result set
    API-->>Client: 200 OK + data
```
````

```mermaid
sequenceDiagram
    participant Client
    participant API
    participant Auth
    participant DB

    Client->>API: POST /api/login
    API->>Auth: Validate credentials
    Auth->>DB: Query user record
    DB-->>Auth: User data
    Auth-->>API: JWT token
    API-->>Client: 200 OK + token

    Client->>API: GET /api/data (Bearer token)
    API->>Auth: Verify token
    Auth-->>API: Token valid
    API->>DB: Fetch data
    DB-->>API: Result set
    API-->>Client: 200 OK + data
```

Arrow types:
- `->>` — Solid line with arrowhead (synchronous)
- `-->>` — Dashed line with arrowhead (response)
- `--)` — Solid line with open arrow (asynchronous)
- `--x` — Dashed line with cross (lost message)

### Class Diagram

Class diagrams represent the structure of a system by showing classes, their attributes, methods, and relationships. These are particularly useful for .NET API documentation.

````markdown
```mermaid
classDiagram
    class IDocumentProcessor {
        <<interface>>
        +Process(document: Document) Task~Result~
        +Validate(document: Document) bool
    }

    class MarkdownProcessor {
        -ILogger logger
        -MarkdigPipeline pipeline
        +Process(document: Document) Task~Result~
        +Validate(document: Document) bool
        -ParseFrontMatter(content: string) Metadata
    }

    class HtmlRenderer {
        -TemplateEngine engine
        +Render(result: Result) string
    }

    class Document {
        +string Path
        +string Content
        +Metadata Meta
    }

    IDocumentProcessor <|.. MarkdownProcessor : implements
    MarkdownProcessor --> Document : processes
    MarkdownProcessor --> HtmlRenderer : uses
```
````

```mermaid
classDiagram
    class IDocumentProcessor {
        <<interface>>
        +Process(document: Document) Task~Result~
        +Validate(document: Document) bool
    }

    class MarkdownProcessor {
        -ILogger logger
        -MarkdigPipeline pipeline
        +Process(document: Document) Task~Result~
        +Validate(document: Document) bool
        -ParseFrontMatter(content: string) Metadata
    }

    class HtmlRenderer {
        -TemplateEngine engine
        +Render(result: Result) string
    }

    class Document {
        +string Path
        +string Content
        +Metadata Meta
    }

    IDocumentProcessor <|.. MarkdownProcessor : implements
    MarkdownProcessor --> Document : processes
    MarkdownProcessor --> HtmlRenderer : uses
```

Relationship types:
- `<|--` — Inheritance
- `<|..` — Implementation
- `-->` — Association
- `..>` — Dependency
- `--o` — Aggregation
- `--*` — Composition

### State Diagram

State diagrams depict the states of an object and the transitions between them.

````markdown
```mermaid
stateDiagram-v2
    [*] --> Draft
    Draft --> InReview : Submit
    InReview --> Draft : Request Changes
    InReview --> Approved : Approve
    Approved --> Published : Publish
    Published --> Archived : Archive
    Archived --> Draft : Restore
    Published --> Draft : Unpublish
    Approved --> Draft : Revoke
    Published --> [*]
```
````

```mermaid
stateDiagram-v2
    [*] --> Draft
    Draft --> InReview : Submit
    InReview --> Draft : Request Changes
    InReview --> Approved : Approve
    Approved --> Published : Publish
    Published --> Archived : Archive
    Archived --> Draft : Restore
    Published --> Draft : Unpublish
    Approved --> Draft : Revoke
    Published --> [*]
```

### Entity Relationship Diagram

ER diagrams model database schemas and the relationships between entities.

````markdown
```mermaid
erDiagram
    USER ||--o{ DOCUMENT : creates
    USER ||--o{ COMMENT : writes
    DOCUMENT ||--o{ COMMENT : has
    DOCUMENT ||--o{ VERSION : tracks
    DOCUMENT }o--|| CATEGORY : belongs_to

    USER {
        int id PK
        string username
        string email
        datetime created_at
    }
    DOCUMENT {
        int id PK
        string title
        text content
        int author_id FK
        int category_id FK
    }
    COMMENT {
        int id PK
        text body
        int user_id FK
        int document_id FK
    }
```
````

```mermaid
erDiagram
    USER ||--o{ DOCUMENT : creates
    USER ||--o{ COMMENT : writes
    DOCUMENT ||--o{ COMMENT : has
    DOCUMENT ||--o{ VERSION : tracks
    DOCUMENT }o--|| CATEGORY : belongs_to

    USER {
        int id PK
        string username
        string email
        datetime created_at
    }
    DOCUMENT {
        int id PK
        string title
        text content
        int author_id FK
        int category_id FK
    }
    COMMENT {
        int id PK
        text body
        int user_id FK
        int document_id FK
    }
```

### Gantt Chart

Gantt charts are useful for project timelines and scheduling.

````markdown
```mermaid
gantt
    title MokaDocs v2.0 Release Plan
    dateFormat YYYY-MM-DD
    section Core
        Markdown engine upgrade  :done, core1, 2025-01-01, 30d
        API doc generator        :done, core2, after core1, 20d
        Search index builder     :active, core3, after core2, 15d
    section UI
        Theme redesign           :ui1, after core1, 25d
        Component library        :ui2, after ui1, 20d
        Mobile responsive        :ui3, after ui2, 10d
    section Release
        Beta testing             :rel1, after core3, 14d
        Documentation            :rel2, after ui3, 10d
        Public release           :milestone, rel3, after rel2, 0d
```
````

```mermaid
gantt
    title MokaDocs v2.0 Release Plan
    dateFormat YYYY-MM-DD
    section Core
        Markdown engine upgrade  :done, core1, 2025-01-01, 30d
        API doc generator        :done, core2, after core1, 20d
        Search index builder     :active, core3, after core2, 15d
    section UI
        Theme redesign           :ui1, after core1, 25d
        Component library        :ui2, after ui1, 20d
        Mobile responsive        :ui3, after ui2, 10d
    section Release
        Beta testing             :rel1, after core3, 14d
        Documentation            :rel2, after ui3, 10d
        Public release           :milestone, rel3, after rel2, 0d
```

### Pie Chart

Pie charts display proportional data.

````markdown
```mermaid
pie title Documentation Pages by Category
    "Guides" : 42
    "API Reference" : 35
    "Tutorials" : 15
    "FAQ" : 8
```
````

```mermaid
pie title Documentation Pages by Category
    "Guides" : 42
    "API Reference" : 35
    "Tutorials" : 15
    "FAQ" : 8
```

## Tips for Complex Diagrams

### Keep It Readable

Diagrams are most effective when they communicate a concept clearly. If a diagram becomes too complex, consider splitting it into multiple smaller diagrams with explanatory text between them.

### Use Subgraphs for Grouping

In flowcharts, use `subgraph` blocks to group related nodes:

````markdown
```mermaid
flowchart TD
    subgraph Frontend
        A[Browser] --> B[React App]
    end
    subgraph Backend
        C[API Gateway] --> D[Service]
        D --> E[Database]
    end
    B --> C
```
````

```mermaid
flowchart TD
    subgraph Frontend
        A[Browser] --> B[React App]
    end
    subgraph Backend
        C[API Gateway] --> D[Service]
        D --> E[Database]
    end
    B --> C
```

### Add Notes in Sequence Diagrams

Use notes to annotate sequence diagrams with additional context:

````markdown
```mermaid
sequenceDiagram
    Client->>Server: Request
    Note over Client,Server: TLS encrypted
    Server-->>Client: Response
    Note right of Client: Cache for 5 minutes
```
````

```mermaid
sequenceDiagram
    Client->>Server: Request
    Note over Client,Server: TLS encrypted
    Server-->>Client: Response
    Note right of Client: Cache for 5 minutes
```

### Escape Special Characters

If your diagram labels contain special characters, wrap them in quotes:

```
A["Node with (parentheses)"] --> B["Node with {braces}"]
```

### Test Incrementally

When building complex diagrams, add elements one at a time and preview after each addition. A single syntax error can prevent the entire diagram from rendering. The Mermaid Live Editor at [mermaid.live](https://mermaid.live) is a useful tool for testing diagram syntax independently.
