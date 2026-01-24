# Mero Dainiki - Entity Relationship Diagram (ERD)

## Complete Database Schema

```mermaid
erDiagram
    USER ||--o{ JOURNAL_ENTRY : creates
    USER ||--o{ USER_PREFERENCE : has
    USER ||--o{ USER_SESSION : has
    JOURNAL_ENTRY ||--o{ TAG : contains
    JOURNAL_ENTRY ||--o{ ENTRY_ATTACHMENT : has
    USER ||--o{ PIN_ATTEMPT : records
    TAG ||--o{ TAG_CATEGORY : belongs_to
    
    USER {
        int Id PK
        string Username UK
        string Email UK
        string PasswordHash
        string Pin
        datetime CreatedAt
        datetime LastLoginAt
        boolean IsActive
    }
    
    JOURNAL_ENTRY {
        int Id PK
        int UserId FK
        string Title
        string Content
        date Date UK "unique per user per day"
        datetime CreatedAt
        datetime UpdatedAt
        enum PrimaryMood
        enum SecondaryMood1
        enum SecondaryMood2
        enum Category
        boolean IsFavorite
        string ImagePath
        int WordCount "calculated"
    }
    
    TAG {
        int Id PK
        string Name UK
        string Color
        datetime CreatedAt
        int CategoryId FK
    }
    
    TAG_CATEGORY {
        int Id PK
        string Name
        string Description
    }
    
    ENTRY_ATTACHMENT {
        int Id PK
        int EntryId FK
        string FileName
        string FilePath
        string FileType
        long FileSize
        datetime UploadedAt
    }
    
    USER_PREFERENCE {
        int Id PK
        int UserId FK UK
        enum ThemeMode
        boolean EnableNotifications
        boolean EnableStreakReminder
        string ReminderTime
        int ReminderDaysOfWeek
        string Language
        boolean PrivateMode
        datetime UpdatedAt
    }
    
    USER_SESSION {
        int Id PK
        int UserId FK
        string SessionToken UK
        string DeviceName
        string IPAddress
        enum DeviceType
        datetime CreatedAt
        datetime LastActiveAt
        datetime ExpiresAt
        boolean IsActive
    }
    
    PIN_ATTEMPT {
        int Id PK
        int UserId FK
        string EnteredPin
        boolean IsCorrect
        datetime AttemptedAt
        string DeviceInfo
    }
```

## Entity Descriptions

### 1. **USER** (Core Authentication Entity)
- **Purpose**: Store user account information and authentication data
- **Primary Keys**: Id
- **Unique Keys**: Username, Email
- **Key Fields**:
  - `PasswordHash`: Hashed password (bcrypt/PBKDF2)
  - `Pin`: Optional PIN for quick unlock
  - `IsActive`: Soft delete flag
  - `LastLoginAt`: Track user activity

### 2. **JOURNAL_ENTRY** (Core Business Entity)
- **Purpose**: Store daily journal entries
- **Primary Keys**: Id
- **Unique Keys**: (UserId, Date) - One entry per user per day
- **Key Fields**:
  - `PrimaryMood`: Required mood selection (5-point scale)
  - `SecondaryMood1/2`: Optional additional moods
  - `Category`: Entry category (Work, Personal, Travel, etc.)
  - `IsFavorite`: Mark as favorite entry
  - `WordCount`: Calculated field for analytics
  - `ImagePath`: Support for attaching images

### 3. **TAG** (Categorization Entity)
- **Purpose**: Allow users to tag and organize entries
- **Primary Keys**: Id
- **Unique Keys**: Name
- **Key Fields**:
  - `Color`: Custom color coding for visual organization
  - `CategoryId`: Group related tags
  - Many-to-Many relationship with Journal Entries

### 4. **TAG_CATEGORY** (Tag Organization)
- **Purpose**: Organize tags into categories
- **Examples**: #work, #personal, #health, #family

### 5. **ENTRY_ATTACHMENT** (File Management)
- **Purpose**: Support for images/attachments in entries
- **Key Fields**:
  - `FileType`: Image, PDF, Document
  - `FileSize`: For quota management
  - `FilePath`: Cloud storage reference

### 6. **USER_PREFERENCE** (Settings Entity)
- **Purpose**: Store user-specific settings and preferences
- **Key Fields**:
  - `ThemeMode`: Light/Dark/System
  - `EnableNotifications`: Push notification toggle
  - `ReminderTime`: Scheduled reminder time
  - `PrivateMode`: Enhanced privacy options

### 7. **USER_SESSION** (Session Management)
- **Purpose**: Track active user sessions across devices
- **Key Fields**:
  - `SessionToken`: JWT or similar token
  - `DeviceName`: Device identifier
  - `IPAddress`: Track login locations
  - `ExpiresAt`: Session expiration time

### 8. **PIN_ATTEMPT** (Security Audit)
- **Purpose**: Log PIN entry attempts for security
- **Key Fields**:
  - `IsCorrect`: Success/failure tracking
  - `DeviceInfo`: Device fingerprint

## Enums Reference

### Mood Enum
```
VeryHappy = 5   😊
Happy = 4       😊
Neutral = 3     😐
Sad = 2         😢
VerySad = 1     😢
```

### EntryCategory Enum
- Personal
- Work
- Travel
- Health
- Family
- Friends
- Hobbies
- Goals
- Gratitude
- Other

### ThemeMode Enum
- Light
- Dark
- System

### DeviceType Enum
- Mobile
- Tablet
- Desktop
- Web

## Relationships Summary

| From | To | Type | Cardinality | Description |
|------|----|----|------|---|
| USER | JOURNAL_ENTRY | One-to-Many | 1:N | A user has many entries |
| USER | USER_PREFERENCE | One-to-One | 1:1 | Each user has one preference set |
| USER | USER_SESSION | One-to-Many | 1:N | A user can have multiple active sessions |
| USER | PIN_ATTEMPT | One-to-Many | 1:N | Track PIN attempts per user |
| JOURNAL_ENTRY | TAG | Many-to-Many | M:N | Entry can have multiple tags, tag can be in multiple entries |
| JOURNAL_ENTRY | ENTRY_ATTACHMENT | One-to-Many | 1:N | Entry can have multiple attachments |
| TAG | TAG_CATEGORY | Many-to-One | N:1 | Multiple tags belong to one category |

## Indexes Recommended

```sql
-- Performance indexes
CREATE INDEX idx_journal_entry_user_date ON JournalEntry(UserId, Date);
CREATE INDEX idx_journal_entry_user_created ON JournalEntry(UserId, CreatedAt);
CREATE INDEX idx_journal_entry_favorite ON JournalEntry(UserId, IsFavorite);
CREATE INDEX idx_journal_entry_category ON JournalEntry(Category);
CREATE INDEX idx_tag_name ON Tag(Name);
CREATE INDEX idx_user_session_active ON UserSession(UserId, IsActive);
CREATE INDEX idx_pin_attempt_user_date ON PINAttempt(UserId, AttemptedAt);
```

## Future Enhancements

### Planned Entities
1. **MOOD_ANALYTICS** - Aggregate mood data for charts
2. **ENTRY_SEARCH** - Full-text search index
3. **BACKUP_LOG** - Track data export/backup history
4. **NOTIFICATION_LOG** - Store notification history
5. **ACTIVITY_LOG** - Comprehensive audit trail
6. **SHARING** - Share entries with others
7. **COLLABORATION** - Multi-user journal support
8. **SUBSCRIPTION** - Premium features tracking

### Planned Relationships
- USER → SHARING → JOURNAL_ENTRY (share entries)
- USER → COLLABORATION (invite other users)
- USER → SUBSCRIPTION (feature access)

## Data Validation Rules

### USER
- Email must be unique and valid format
- Username must be 3-20 characters
- Password minimum 8 characters
- Pin must be 4-6 digits

### JOURNAL_ENTRY
- Title: 1-200 characters
- Content: 1-50000 characters
- Only one entry per user per day
- Date cannot be in future
- WordCount auto-calculated

### TAG
- Name: 1-50 characters
- Color: Valid hex code
- No duplicate tag names per user

### USER_PREFERENCE
- Valid time format for reminders
- Valid color codes
- Language codes from ISO 639-1

## Backup & Recovery

- Regular backups of JOURNAL_ENTRY table
- Archive old entries to separate schema
- Point-in-time recovery capability
- User data export functionality

