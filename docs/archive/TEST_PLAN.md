## 🧪 Journal AI - Comprehensive Testing Guide

### Application URLs
- **Frontend**: http://localhost:5173/
- **Backend API**: http://localhost:5000/api/v1
- **Browser Console**: Press F12 to open Developer Tools

---

## ✅ Test Cases

### 1. Authentication Tests

#### 1a. Register New User
- [ ] Go to http://localhost:5173/register
- [ ] Enter email: `test-user-$(date).com`
- [ ] Enter password: `Test123!@#`
- [ ] Click Register
- [ ] **Expected**: Redirected to Timeline page, auto-logged in

#### 1b. Login
- [ ] Go to http://localhost:5173/login
- [ ] Enter registered email and password
- [ ] Click Login
- [ ] **Expected**: Redirected to Timeline, see "Welcome" message if applicable

#### 1c. Logout
- [ ] Click Logout button (⛔ icon)
- [ ] **Expected**: Redirected to Login page

---

### 2. Entry Creation with Sentiment Analysis (CRITICAL TEST)

#### 2a. Create Entry with Positive Sentiment
- [ ] Click "Create New Entry" or go to http://localhost:5173/entries/new
- [ ] **Title**: "Great Day Today"
- [ ] **Body**:
  ```
  I had an amazing wonderful time! I love working on this project. 
  It's fantastic and I'm so happy about the progress!
  ```
- [ ] **Confidentiality**: Public
- [ ] **Open Browser Console** (F12) and watch for logs:
  - `[EntryForm] Sentiment calculation triggered: newScore: X.XX`
  - `[EntryForm] Payload being sent: {sentimentScore: X.XX, ...}`
  - `[apiClient] POST /entries body: {sentimentScore: X.XX, ...}`
- [ ] Click Submit
- [ ] **Expected**:
  - Success message shows: `📊 Sentiment: 😄 Very Positive (X.XX)` where X.XX > 0
  - Redirected to Timeline after 1.5 seconds
  - Entry appears in Timeline with positive sentiment badge

#### 2b. Create Entry with Negative Sentiment
- [ ] Go to http://localhost:5173/entries/new
- [ ] **Title**: "Difficult Day"
- [ ] **Body**:
  ```
  This was terrible and awful. I'm frustrated and disappointed.
  This is really bad and I hate how this turned out.
  ```
- [ ] **Confidentiality**: Public
- [ ] **Watch Console Logs** for sentiment calculation
- [ ] Click Submit
- [ ] **Expected**:
  - Success message shows: `📊 Sentiment: 😞 Very Negative (X.XX)` where X.XX < 0
  - Entry in Timeline shows negative sentiment icon

#### 2c. Create Entry with Neutral Sentiment
- [ ] Go to http://localhost:5173/entries/new
- [ ] **Title**: "Regular Day"
- [ ] **Body**:
  ```
  This was a normal day. I went to work and came back home.
  Nothing special happened but that was fine.
  ```
- [ ] Click Submit
- [ ] **Expected**:
  - Success message shows: `📊 Sentiment: 😐 Neutral (0.00)` or close to 0
  - Entry in Timeline shows neutral sentiment icon

#### 2d. Test Fast Submission (Timing Bug Check)
- [ ] Go to http://localhost:5173/entries/new
- [ ] **Title**: "Quick Test"
- [ ] **Body**: Type text with sentiment and IMMEDIATELY click Submit (no delay)
- [ ] Body with sentiment words: "I like and enjoy this amazing thing"
- [ ] **Expected**:
  - **CRITICAL**: Even with fast submission, sentiment should NOT be 0
  - Success message should show positive sentiment
  - Check Browser Console for: `[EntryForm] Recalculated sentiment on submit` (indicates safety net was used)
  - Entry in Timeline should have positive sentiment

---

### 3. Timeline & Entry Viewing Tests

#### 3a. View Timeline List
- [ ] Navigate to http://localhost:5173/
- [ ] **Expected** features:
  - [ ] All created entries are visible
  - [ ] Entries show: Title, Date, Sentiment Score/Icon, Confidentiality
  - [ ] Sentiment icons match: 😄 (positive), 😐 (neutral), 😞 (negative)
  - [ ] Pagination works if > 20 entries

#### 3b. View Entry Detail
- [ ] Click the 👁️ (eye) icon on any entry
- [ ] **Expected**:
  - [ ] Full entry text displayed
  - [ ] Sentiment score and label shown
  - [ ] Date, category, tags visible
  - [ ] Edit/Delete buttons (if not immutable)

#### 3c. Edit Entry (if within 24h of creation)
- [ ] Click ✏️ (pencil) icon on a recent entry
- [ ] Modify the body text
- [ ] **Note**: Sentiment won't update on edit (by design - immutable design)
- [ ] Click Update
- [ ] **Expected**: Entry updated without sentiment change

#### 3d. Delete Entry
- [ ] Click 🗑️ (trash) icon on any entry
- [ ] Confirm deletion
- [ ] **Expected**: Entry removed from Timeline

---

### 4. Visualization Tests

#### 4a. Mindmap View
- [ ] Click 🧠 (Mindmap) button on Timeline
- [ ] **Expected**:
  - [ ] Hierarchical mindmap of entries by category
  - [ ] Root node shows total entries
  - [ ] Expandable/collapsible nodes
  - [ ] Entry nodes colored by sentiment (green/yellow/red)
  - [ ] Statistics cards showing totals

#### 4b. Sentiment Chart View
- [ ] Click 📈 (Chart) button on Timeline
- [ ] **Expected**:
  - [ ] Bar chart showing sentiment distribution
  - [ ] Color coding: 🟢 happy, 🟡 neutral, 🔴 difficult
  - [ ] Statistics: Average sentiment, happy days, neutral days, difficult days
  - [ ] Date range filters (Week, Month, All time)

---

### 5. Data Persistence Tests

#### 5a. Sentiment Persists After Page Refresh
- [ ] Create entry with clear sentiment: "I love this!"
- [ ] Note the sentiment score shown
- [ ] Refresh the page (F5)
- [ ] Navigate back to Timeline
- [ ] **Expected**: Entry still shows the same sentiment score

#### 5b. Entries Persist After Login/Logout
- [ ] Create 2-3 entries with different sentiments
- [ ] Logout
- [ ] Login again
- [ ] **Expected**: All entries still visible with correct sentiment scores

---

### 6. Browser Console Checks

#### Expected Log Patterns (Open F12 Developer Tools)
```
[EntryForm] Sentiment calculation triggered: {
  bodyTextLength: 150,
  newScore: 0.5,
  previousScore: 0
}

[sentimentAnalyzer] {
  text: "I love this amazing wonderful thing!",
  wordCount: 7,
  sentimentWordCount: 3,
  foundWords: [{word: "love", type: "positive", score: 1}, ...],
  score: 3,
  finalScore: 1,
  rounded: 1,
  label: "😄 Very Positive"
}

[EntryForm] Payload being sent: {
  sentimentScore: 1,
  sentimentLabel: "😄 Very Positive",
  sentimentModel: "simple-keyword-analysis"
}

[apiClient] POST /entries body: {
  sentimentScore: 1,
  ...
}
```

#### No Errors Expected
- [ ] Console should be **free of red error messages**
- [ ] No 401/403 Authorization errors
- [ ] No network failures
- [ ] No React warnings about unhandled promises

---

### 7. Edge Cases & Stress Tests

#### 7a. Empty Entry
- [ ] Try to submit entry with no title OR body
- [ ] **Expected**: Validation error message

#### 7b. Very Long Text
- [ ] Create entry with 5000+ character body with mixed sentiment
- [ ] **Expected**: Sentiment calculated correctly, no performance issues

#### 7c. Special Characters & Emojis
- [ ] Create entry with: "I ❤️ this! It's awesome 🎉"
- [ ] **Expected**: Processes without errors, sentiment detected correctly

#### 7d. Multiple Rapid Submissions
- [ ] Open 2 browser tabs with the app
- [ ] Create entries in both tabs rapidly
- [ ] **Expected**: No data corruption, all entries appear correctly

---

## 🐛 Common Issues to Watch For

| Issue | Expected Behavior | How to Check |
|-------|-------------------|------------|
| Sentiment always shows 0 | Should show calculated value | Check success message |
| Entries disappear after page refresh | Should persist in timeline | Refresh and see entries |
| Can't create entry | Should show validation error | Check for error alert |
| Login/Register not working | Should redirect on success | Browser console for 401/403 |
| Sentiment icons misaligned | Should show emoji correctly | Look at timeline sentiment column |

---

## ✨ Success Criteria

- ✅ Authentication (register/login/logout) works
- ✅ Entry creation succeeds
- ✅ **Sentiment score is calculated and displayed in success message**
- ✅ **Sentiment score is NOT 0 when text has sentiment words**
- ✅ **Sentiment score persists in database (same value after refresh)**
- ✅ Timeline displays entries with correct sentiment icons
- ✅ Both visualization modes (Mindmap, Chart) work
- ✅ No console errors
- ✅ Edit/Delete/View entry operations work
- ✅ Logout and login again shows persisted entries with sentiment

---

## 📝 Testing Notes

**Date**: $(date)
**Tester**: You
**Browser**: $(browser.version)
**Backend**: http://localhost:5000
**Frontend**: http://localhost:5173

### Issues Found:
(List any bugs or unexpected behaviors)

### Passed Tests:
(Mark completed test cases)
