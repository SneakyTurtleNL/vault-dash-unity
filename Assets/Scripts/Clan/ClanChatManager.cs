using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if FIREBASE_FIRESTORE
using Firebase.Firestore;
using Firebase.Extensions;
#endif

/// <summary>
/// ClanChatManager — Real-time clan chat via Firestore.
///
/// FIRESTORE SCHEMA:
///   clans/{clanId}/messages/{messageId}/
///     authorId     : string
///     authorName   : string
///     text         : string
///     timestamp    : timestamp
///     deleted      : bool
///     mutedUntil   : timestamp (null if not muted)
///
/// FEATURES:
///   • Real-time message listener (Firestore snapshot)
///   • Send, delete (soft), mute members
///   • Unread badge counter (persisted in PlayerPrefs)
///   • Events for UI binding
///
/// INTEGRATION:
///   1. Attach ClanChatManager to a persistent GameObject.
///   2. Call JoinClanChat(clanId) after clan is loaded.
///   3. Subscribe to OnMessagesUpdated to drive ClanChatPanel UI.
///   4. Call SendMessage(text) from the input field.
///
/// REQUIREMENTS:
///   • FIREBASE_FIRESTORE scripting define.
///   • FirebaseManager.Instance.UserId must be set.
///   • Player must be in a clan (clanId != null).
/// </summary>
public class ClanChatManager : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static ClanChatManager Instance { get; private set; }

    // ─── Constants ────────────────────────────────────────────────────────────
    private const int  MAX_HISTORY      = 100;   // messages to load from Firestore
    private const int  MAX_TEXT_LENGTH  = 280;   // character limit per message
    private const string PREF_LAST_READ = "VaultDash_ClanChatLastRead";

    // ─── Data ─────────────────────────────────────────────────────────────────
    [Serializable]
    public class ChatMessage
    {
        public string   MessageId;
        public string   AuthorId;
        public string   AuthorName;
        public string   Text;
        public DateTime Timestamp;
        public bool     Deleted;
        public bool     IsOwnMessage;   // convenience flag
    }

    // ─── Events ───────────────────────────────────────────────────────────────
    public event Action<List<ChatMessage>> OnMessagesUpdated;  // full list
    public event Action<ChatMessage>       OnNewMessage;       // single new message
    public event Action<int>               OnUnreadCountChanged;

    // ─── State ────────────────────────────────────────────────────────────────
    public string             CurrentClanId   { get; private set; }
    public List<ChatMessage>  Messages        { get; private set; } = new List<ChatMessage>();
    public int                UnreadCount     { get; private set; } = 0;

    private DateTime _lastReadTimestamp = DateTime.MinValue;

#if FIREBASE_FIRESTORE
    private ListenerRegistration _listener;
#endif

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadLastReadTimestamp();
    }

    void OnDestroy()
    {
        LeaveClanChat();
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>Attaches a real-time listener to the given clan's chat.</summary>
    public void JoinClanChat(string clanId)
    {
        if (string.IsNullOrEmpty(clanId)) { Debug.LogWarning("[ClanChat] clanId is null."); return; }

        LeaveClanChat();  // detach any previous listener
        CurrentClanId = clanId;

#if FIREBASE_FIRESTORE
        var db = FirebaseFirestore.DefaultInstance;
        _listener = db.Collection("clans")
                      .Document(clanId)
                      .Collection("messages")
                      .OrderBy("timestamp")
                      .Limit(MAX_HISTORY)
                      .Listen(OnSnapshotReceived);

        Debug.Log($"[ClanChat] Joined clan chat: {clanId}");
#else
        Debug.LogWarning("[ClanChat] FIREBASE_FIRESTORE not defined — chat unavailable.");
#endif
    }

    /// <summary>Detaches the Firestore listener.</summary>
    public void LeaveClanChat()
    {
#if FIREBASE_FIRESTORE
        _listener?.Stop();
        _listener = null;
#endif
        CurrentClanId = null;
    }

    /// <summary>Sends a message to the current clan chat.</summary>
    public void SendMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (text.Length > MAX_TEXT_LENGTH) text = text.Substring(0, MAX_TEXT_LENGTH);
        if (string.IsNullOrEmpty(CurrentClanId))
        {
            Debug.LogWarning("[ClanChat] Not in a clan chat session.");
            return;
        }

#if FIREBASE_FIRESTORE
        string uid  = FirebaseManager.Instance?.UserId ?? "unknown";
        string name = FirebaseManager.Instance?.DisplayName ?? "Player";

        var db  = FirebaseFirestore.DefaultInstance;
        var col = db.Collection("clans").Document(CurrentClanId).Collection("messages");

        var data = new Dictionary<string, object>
        {
            { "authorId",   uid },
            { "authorName", name },
            { "text",       text },
            { "timestamp",  Timestamp.GetCurrentTimestamp() },
            { "deleted",    false }
        };

        col.AddAsync(data).ContinueWithOnMainThread(t =>
        {
            if (t.IsFaulted)
                Debug.LogWarning($"[ClanChat] Send failed: {t.Exception?.Message}");
        });
#else
        // Offline preview: add locally
        var msg = new ChatMessage
        {
            MessageId  = Guid.NewGuid().ToString(),
            AuthorId   = "local",
            AuthorName = "You",
            Text       = text,
            Timestamp  = DateTime.UtcNow,
            Deleted    = false,
            IsOwnMessage = true
        };
        Messages.Add(msg);
        OnMessagesUpdated?.Invoke(Messages);
        OnNewMessage?.Invoke(msg);
#endif
    }

    /// <summary>Soft-deletes a message (sets deleted:true). Author or clan officer only.</summary>
    public void DeleteMessage(string messageId)
    {
        if (string.IsNullOrEmpty(CurrentClanId)) return;

#if FIREBASE_FIRESTORE
        var db = FirebaseFirestore.DefaultInstance;
        db.Collection("clans")
          .Document(CurrentClanId)
          .Collection("messages")
          .Document(messageId)
          .UpdateAsync("deleted", true)
          .ContinueWithOnMainThread(t =>
          {
              if (t.IsFaulted)
                  Debug.LogWarning($"[ClanChat] Delete failed: {t.Exception?.Message}");
          });
#endif
    }

    /// <summary>
    /// Mutes a clan member for the given duration (minutes).
    /// Stores mutedUntil timestamp on the member's message document pattern.
    /// Enforcement is done server-side via Firestore Security Rules.
    /// </summary>
    public void MuteMember(string memberId, int durationMinutes)
    {
#if FIREBASE_FIRESTORE
        var muteUntil = DateTime.UtcNow.AddMinutes(durationMinutes);
        var db = FirebaseFirestore.DefaultInstance;
        db.Collection("clans")
          .Document(CurrentClanId)
          .Collection("mutedMembers")
          .Document(memberId)
          .SetAsync(new Dictionary<string, object>
          {
              { "mutedUntil", Timestamp.FromDateTime(muteUntil) },
              { "mutedBy",    FirebaseManager.Instance?.UserId ?? "unknown" }
          })
          .ContinueWithOnMainThread(t =>
          {
              if (t.IsFaulted)
                  Debug.LogWarning($"[ClanChat] Mute failed: {t.Exception?.Message}");
              else
                  Debug.Log($"[ClanChat] Member {memberId} muted for {durationMinutes}min.");
          });
#endif
    }

    /// <summary>Marks all current messages as read, resets unread counter.</summary>
    public void MarkAllRead()
    {
        _lastReadTimestamp = DateTime.UtcNow;
        UnreadCount = 0;
        SaveLastReadTimestamp();
        OnUnreadCountChanged?.Invoke(0);
    }

    // ─── Snapshot Handler ─────────────────────────────────────────────────────

#if FIREBASE_FIRESTORE
    private void OnSnapshotReceived(QuerySnapshot snapshot, FirestoreException error)
    {
        if (error != null)
        {
            Debug.LogWarning($"[ClanChat] Listener error: {error.Message}");
            return;
        }

        string myUid = FirebaseManager.Instance?.UserId ?? "";
        var updated  = new List<ChatMessage>();

        foreach (var doc in snapshot.Documents)
        {
            bool deleted = doc.ContainsField("deleted") && doc.GetValue<bool>("deleted");
            if (deleted) continue;

            var msg = new ChatMessage
            {
                MessageId    = doc.Id,
                AuthorId     = doc.ContainsField("authorId")   ? doc.GetValue<string>("authorId")   : "",
                AuthorName   = doc.ContainsField("authorName") ? doc.GetValue<string>("authorName") : "?",
                Text         = doc.ContainsField("text")       ? doc.GetValue<string>("text")       : "",
                Timestamp    = doc.ContainsField("timestamp")  ? doc.GetValue<Timestamp>("timestamp").ToDateTime() : DateTime.UtcNow,
                Deleted      = false
            };
            msg.IsOwnMessage = msg.AuthorId == myUid;
            updated.Add(msg);
        }

        Messages = updated;
        OnMessagesUpdated?.Invoke(Messages);

        // Count unread
        int unread = 0;
        foreach (var m in Messages)
            if (m.Timestamp > _lastReadTimestamp && !m.IsOwnMessage) unread++;

        if (unread != UnreadCount)
        {
            UnreadCount = unread;
            OnUnreadCountChanged?.Invoke(UnreadCount);

            if (unread > 0)
                OnNewMessage?.Invoke(Messages[Messages.Count - 1]);
        }
    }
#endif

    // ─── Persistence ──────────────────────────────────────────────────────────

    private void SaveLastReadTimestamp()
    {
        PlayerPrefs.SetString(PREF_LAST_READ, _lastReadTimestamp.ToString("O"));
        PlayerPrefs.Save();
    }

    private void LoadLastReadTimestamp()
    {
        string raw = PlayerPrefs.GetString(PREF_LAST_READ, "");
        if (!string.IsNullOrEmpty(raw) && DateTime.TryParse(raw, null,
            System.Globalization.DateTimeStyles.RoundtripKind, out DateTime dt))
            _lastReadTimestamp = dt;
    }
}
