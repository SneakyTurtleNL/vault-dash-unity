using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ClanChatPanel — Chat UI that binds to ClanChatManager.
///
/// LAYOUT (setup in Inspector):
///   ScrollView
///     └ Content (VerticalLayoutGroup, ContentSizeFitter)
///         └ [MessageBubble prefab instances]
///   InputField (TMP_InputField)
///   SendButton
///   UnreadBadge (Image + TextMeshProUGUI)
///   ContextMenu (delete / mute popup)
///
/// USAGE:
///   1. Attach to the ClanChat panel root GameObject.
///   2. Assign references in Inspector.
///   3. Enable/disable panel via gameObject.SetActive().
///   4. ClanChatManager auto-drives content via events.
/// </summary>
public class ClanChatPanel : MonoBehaviour
{
    // ─── Inspector References ─────────────────────────────────────────────────
    [Header("Scroll View")]
    public ScrollRect       ScrollView;
    public RectTransform    ContentParent;

    [Header("Input")]
    public TMP_InputField   InputField;
    public Button           SendButton;

    [Header("Bubble Prefabs")]
    [Tooltip("Own message bubble (right-aligned, colored)")]
    public GameObject       OwnBubblePrefab;
    [Tooltip("Other player's bubble (left-aligned, grey)")]
    public GameObject       OtherBubblePrefab;

    [Header("Unread Badge")]
    public GameObject       UnreadBadgeRoot;
    public TextMeshProUGUI  UnreadBadgeText;

    [Header("Context Menu")]
    [Tooltip("Popup shown on long-press / right-click of a message")]
    public GameObject       ContextMenuPanel;
    public Button           ContextDeleteBtn;
    public Button           ContextMuteBtn;

    [Header("Empty State")]
    public GameObject       EmptyStatePlaceholder;  // "No messages yet"

    // ─── State ────────────────────────────────────────────────────────────────
    private readonly List<GameObject> _bubbles = new List<GameObject>();
    private string   _selectedMessageId;
    private string   _selectedAuthorId;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    void OnEnable()
    {
        if (ClanChatManager.Instance == null) return;

        ClanChatManager.Instance.OnMessagesUpdated   += HandleMessagesUpdated;
        ClanChatManager.Instance.OnUnreadCountChanged += HandleUnreadChanged;

        // Immediately render whatever's cached
        HandleMessagesUpdated(ClanChatManager.Instance.Messages);
        HandleUnreadChanged(ClanChatManager.Instance.UnreadCount);
        ClanChatManager.Instance.MarkAllRead();
    }

    void OnDisable()
    {
        if (ClanChatManager.Instance == null) return;

        ClanChatManager.Instance.OnMessagesUpdated    -= HandleMessagesUpdated;
        ClanChatManager.Instance.OnUnreadCountChanged -= HandleUnreadChanged;
        ClanChatManager.Instance.MarkAllRead();
    }

    void Start()
    {
        if (SendButton   != null) SendButton.onClick.AddListener(OnSendClicked);
        if (InputField   != null) InputField.onSubmit.AddListener(_ => OnSendClicked());
        if (ContextDeleteBtn != null) ContextDeleteBtn.onClick.AddListener(OnDeleteClicked);
        if (ContextMuteBtn   != null) ContextMuteBtn.onClick.AddListener(OnMuteClicked);

        HideContextMenu();
    }

    // ─── Message Rendering ────────────────────────────────────────────────────

    private void HandleMessagesUpdated(List<ClanChatManager.ChatMessage> messages)
    {
        // Clear existing bubbles
        foreach (var b in _bubbles) Destroy(b);
        _bubbles.Clear();

        if (EmptyStatePlaceholder != null)
            EmptyStatePlaceholder.SetActive(messages == null || messages.Count == 0);

        if (messages == null || messages.Count == 0) return;

        foreach (var msg in messages)
        {
            SpawnBubble(msg);
        }

        // Scroll to bottom after next frame
        Canvas.ForceUpdateCanvases();
        if (ScrollView != null)
            ScrollView.verticalNormalizedPosition = 0f;
    }

    private void SpawnBubble(ClanChatManager.ChatMessage msg)
    {
        GameObject prefab = msg.IsOwnMessage ? OwnBubblePrefab : OtherBubblePrefab;
        if (prefab == null) prefab = OtherBubblePrefab ?? OwnBubblePrefab;
        if (prefab == null)
        {
            // Minimal fallback: create a raw text object
            SpawnFallbackBubble(msg);
            return;
        }

        var go  = Instantiate(prefab, ContentParent);
        _bubbles.Add(go);

        // Populate text fields (handles various prefab layouts)
        var texts = go.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (var tmp in texts)
        {
            if (tmp.name.ToLower().Contains("author") || tmp.name.ToLower().Contains("name"))
                tmp.text = msg.AuthorName;
            else if (tmp.name.ToLower().Contains("time"))
                tmp.text = msg.Timestamp.ToLocalTime().ToString("HH:mm");
            else
                tmp.text = msg.Text;
        }

        // Long-press handler for context menu
        var btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
        string mid = msg.MessageId, aid = msg.AuthorId;
        btn.onClick.AddListener(() => ShowContextMenu(mid, aid));
    }

    private void SpawnFallbackBubble(ClanChatManager.ChatMessage msg)
    {
        var go = new GameObject("MsgBubble", typeof(RectTransform));
        go.transform.SetParent(ContentParent, false);
        _bubbles.Add(go);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text     = $"<b>{msg.AuthorName}</b>: {msg.Text}";
        tmp.fontSize = 14;
        tmp.color    = msg.IsOwnMessage ? new Color(0.2f, 0.6f, 1f) : Color.white;

        var fitter = go.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    // ─── Input Handling ───────────────────────────────────────────────────────

    private void OnSendClicked()
    {
        if (InputField == null) return;
        string text = InputField.text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        ClanChatManager.Instance?.SendMessage(text);
        InputField.text = string.Empty;
        InputField.ActivateInputField();
    }

    // ─── Unread Badge ─────────────────────────────────────────────────────────

    private void HandleUnreadChanged(int count)
    {
        if (UnreadBadgeRoot == null) return;
        UnreadBadgeRoot.SetActive(count > 0);
        if (UnreadBadgeText != null)
            UnreadBadgeText.text = count > 99 ? "99+" : count.ToString();
    }

    // ─── Context Menu ─────────────────────────────────────────────────────────

    private void ShowContextMenu(string messageId, string authorId)
    {
        _selectedMessageId = messageId;
        _selectedAuthorId  = authorId;

        if (ContextMenuPanel != null) ContextMenuPanel.SetActive(true);

        string myUid = FirebaseManager.Instance?.UserId ?? "";
        bool isOwn   = authorId == myUid;

        // Only show delete for own messages; mute for others
        if (ContextDeleteBtn != null) ContextDeleteBtn.gameObject.SetActive(isOwn);
        if (ContextMuteBtn   != null) ContextMuteBtn.gameObject.SetActive(!isOwn);
    }

    private void HideContextMenu()
    {
        if (ContextMenuPanel != null) ContextMenuPanel.SetActive(false);
        _selectedMessageId = null;
        _selectedAuthorId  = null;
    }

    private void OnDeleteClicked()
    {
        if (!string.IsNullOrEmpty(_selectedMessageId))
            ClanChatManager.Instance?.DeleteMessage(_selectedMessageId);
        HideContextMenu();
    }

    private void OnMuteClicked()
    {
        if (!string.IsNullOrEmpty(_selectedAuthorId))
            ClanChatManager.Instance?.MuteMember(_selectedAuthorId, durationMinutes: 60);
        HideContextMenu();
    }

    // Close context menu when tapping outside
    void Update()
    {
        if (ContextMenuPanel != null && ContextMenuPanel.activeSelf)
        {
            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                // Simple dismiss — a proper implementation would check if touch hit the panel
                HideContextMenu();
            }
        }
    }
}
