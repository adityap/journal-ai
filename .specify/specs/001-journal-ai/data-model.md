# Journal AI — Data Model (detailed)

This file contains table definitions and notes for the primary domain model. Use EF Core models and migrations to implement these schemas.

## Tables

### users

- id: uuid (PK)
- email: varchar(320) UNIQUE NOT NULL
- password_hash: varchar NOT NULL
- name: varchar
- timezone: varchar(64) NOT NULL DEFAULT 'UTC'
- settings: jsonb NULL
- no_training_use: boolean NOT NULL DEFAULT true
- created_at: timestamptz NOT NULL DEFAULT now()
- updated_at: timestamptz

Indexes: email (unique)

### categories

- id: uuid (PK)
- user_id: uuid REFERENCES users(id) ON DELETE CASCADE
- name: varchar NOT NULL
- color: varchar(7) NULL
- parent_id: uuid NULL REFERENCES categories(id)
- created_at: timestamptz NOT NULL DEFAULT now()
- updated_at: timestamptz

Indexes: (user_id, name)

### media

- id: uuid (PK)
- user_id: uuid REFERENCES users(id) ON DELETE CASCADE
- entry_id: uuid NULL REFERENCES entries(id) ON DELETE SET NULL
- url: text NOT NULL
- mime: varchar(128)
- size: bigint
- thumbnail_url: text NULL
- storage_key: varchar NOT NULL
- created_at: timestamptz NOT NULL DEFAULT now()

Indexes: (user_id)

### entries

- id: uuid (PK)
- user_id: uuid REFERENCES users(id) ON DELETE CASCADE
- title: varchar NULL
- body_text: text NULL
- type: varchar(16) NOT NULL DEFAULT 'text' -- text|photo|video|mixed
- confidentiality: varchar(16) NOT NULL DEFAULT 'public' -- public|private
- confidentiality_method: varchar(32) NULL -- account_password|entry_password|none
- confidentiality_hint: varchar NULL
- confidentiality_hash: varchar NULL -- salted hash if entry password used
- category_id: uuid NULL REFERENCES categories(id)
- tags: text[] NULL
- sentiment_score: numeric(5,4) NULL -- range -1..1
- sentiment_label: varchar(16) NULL -- positive|neutral|negative
- sentiment_model: varchar NULL
- created_at: timestamptz NOT NULL DEFAULT now()
- updated_at: timestamptz NULL
- read_only_after: timestamptz NOT NULL -- computed at creation time based on user timezone
- immutable: boolean NOT NULL DEFAULT false -- set true via trigger/job when read_only_after passes
- metadata: jsonb NULL
- source: varchar(16) -- ui|import|api
- original_hash: varchar NULL

Indexes: (user_id, created_at), GIN index on (body_text gin_trgm_ops) or full-text index, GIN on tags

### audit_logs

- id: uuid (PK)
- entry_id: uuid NULL REFERENCES entries(id)
- user_id: uuid NULL REFERENCES users(id)
- action: varchar(32) NOT NULL -- create|update|delete|unlock
- actor_ip: varchar(45) NULL
- timestamp: timestamptz NOT NULL DEFAULT now()
- diff: bytea NULL -- encrypted diff blob

Indexes: (entry_id), (user_id)

### export_jobs

- id: uuid (PK)
- user_id: uuid REFERENCES users(id)
- scope: jsonb NOT NULL -- description of included entries
- format: varchar(16) NOT NULL -- json|md|zip
- status: varchar(16) NOT NULL -- queued|running|completed|failed
- download_url: text NULL -- signed URL when completed
- created_at: timestamptz NOT NULL DEFAULT now()
- completed_at: timestamptz NULL

Indexes: (user_id, status)

### unlock_sessions

- id: uuid (PK)
- user_id: uuid REFERENCES users(id)
- entry_id: uuid NULL REFERENCES entries(id)
- session_token: varchar NOT NULL
- expires_at: timestamptz NOT NULL
- created_at: timestamptz NOT NULL DEFAULT now()

Indexes: (session_token)

## Implementation notes

- read_only_after should be computed server-side at entry creation using the user's timezone. Consider storing it as part of the entry record for easy enforcement.
- immutable can be updated via a scheduled job (e.g., once-per-minute) or computed in queries (`now() > read_only_after`) — storing it avoids repeated computation but requires background job.
- All sensitive fields (confidentiality_hash, audit diff) should be encrypted at rest using a KMS-managed key.
- Use JSONB for flexible metadata; index frequently queried JSON fields if necessary.
- For full-text search, use PostgreSQL `tsvector` columns or trigram indexes for fuzzy search. Keep indexes limited to avoid write amplification.
- EF Core: map arrays (tags) as `string[]` using `Npgsql` array mapping.

*Data model created: 2025-11-19*
