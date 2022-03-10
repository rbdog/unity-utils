using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static TalkPlayer;

public partial class TalkPlayer : MonoBehaviour
{
    public Text talkText;
    public Button talkButton;
    public AudioClip talkSound;
    public static TalkPlayer controller;
    TalkObject _talkObject;
    AudioSource _audioSource;
    bool _isWriting = false;
    int _lineIndex = -1;
    List<Line> _allLines;
    float _charIntervalSec = 0.0f;
    bool _canReloadThisFrame = false;
    List<char> _muteChars = new List<char>()
    {
        ' ', '　', ',' , '、', '。', '「', '」', '!', '?',
    };
    public void Start()
    {
        _audioSource = talkText.gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.clip = talkSound;
        controller = this;
    }
    public void Update()
    {
        _canReloadThisFrame = true;
    }
}

public partial class TalkPlayer
{
    public void ReloadObject(TalkObject talkObject)
    {
        talkObject.SetIsTalking(true);
        _canReloadThisFrame = false;
        _talkObject = talkObject;
        _allLines = talkObject.Words;
        _lineIndex = -1;
    }
    public bool CanReloadThisFrame { get => _canReloadThisFrame; }
    public void ShowTextArea()
    {
        controller.talkText.gameObject.SetActive(true);
        controller.talkButton.gameObject.SetActive(true);
        controller.ClearLines();
    }
    public void HideTextArea()
    {
        controller.ClearLines();
        controller.talkText.gameObject.SetActive(false);
        controller.talkButton.gameObject.SetActive(false);
    }
    public void WriteLine()
    {
        _charIntervalSec = _talkObject.CharIntervalSec;
        StartCoroutine(IEWrite(_allLines[_lineIndex].text));
    }
    public void ClearLines()
    {
        talkText.text = "";
    }
    void StartNewLineIfNotEmpty()
    {
        if (talkText.text != "")
        {
            talkText.text += "\n";
        }
    }
    void ReadNext()
    {
        _lineIndex += 1;
        var line = _allLines[_lineIndex];
        switch (line.lineType)
        {
            case LineType.SkipOn:
                StartNewLineIfNotEmpty();
                WriteLine();
                break;
            case LineType.SkipOff:
                StartNewLineIfNotEmpty();
                WriteLine();
                break;
            case LineType.Clear:
                ClearLines();
                // 再帰呼び出し
                ReadNext();
                break;
        }
    }
    public void PushAction()
    {
        _canReloadThisFrame = false;
        if (_talkObject == null) return;
        if (_isWriting)
        {
            if (_lineIndex < 0)
            {
                _isWriting = false;
                return;
            }
            if (_allLines[_lineIndex].lineType == LineType.SkipOn)
            {
                _charIntervalSec = 0f;
            }
        }
        else if (_allLines.Count - 1 <= _lineIndex)
        {
            _isWriting = false;
            _talkObject.DidTalk();
            _talkObject.SetIsTalking(false);
            return;
        }
        else
        {
            ReadNext();
        }
    }
    System.Collections.IEnumerator IEWrite(string lineText)
    {
        _isWriting = true;
        foreach (char c in lineText)
        {
            talkText.text += c;
            if (!_muteChars.Contains(c))
            {
                _audioSource.Play();
            }
            yield return new WaitForSeconds(_charIntervalSec);
        }
        _isWriting = false;
    }
}

public partial class TalkPlayer
{
    public enum LineType
    {
        SkipOn,
        SkipOff,
        Clear,
    }
    public interface Line
    {
        LineType lineType { get; }
        String text { get; }
    }
    public class SkipOn : Line
    {
        public LineType lineType { get => LineType.SkipOn; }
        public String text { get; }
        public SkipOn(String text)
        {
            this.text = text;
        }
    }
    public class SkipOff : Line
    {
        public LineType lineType { get => LineType.SkipOff; }
        public String text { get; }
        public SkipOff(String text)
        {
            this.text = text;
        }
    }
    public class Clear : Line
    {
        public LineType lineType { get => LineType.Clear; }
        public String text { get => null; }
    }
}

public class TalkObject : MonoBehaviour
{
    bool _isTalking = false;
    bool _canAction = true;
    public void Start()
    {
        var talkArea = gameObject.AddComponent<SphereCollider>();
        talkArea.isTrigger = true;
        talkArea.radius = MaxDistance;
    }
    public void Update()
    {
        _canAction = true;
    }
    public virtual float MaxDistance { get => 5.0f; }
    public virtual Key PushActionKey { get => Key.Enter; }
    public virtual List<Line> Words
    {
        get => new List<Line>()
        {
            new SkipOn("これはデフォルトのメッセージです"),
        };
    }
    public virtual float CharIntervalSec { get => 0.1f; }
    public virtual void OnPlayerEnter() { }
    public virtual void WillTalk()
    {
        controller.ReloadObject(this);
        controller.ShowTextArea();
        controller.PushAction();
    }
    public virtual void DidTalk()
    {
        controller.HideTextArea();
    }
    public virtual void OnPlayerExit()
    {
        DidTalk();
    }
    public void SetIsTalking(bool isTalking)
    {
        _isTalking = isTalking;
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<TalkPlayer>() != null) OnPlayerEnter();
    }
    void OnTriggerStay(Collider other)
    {
        if (other.gameObject.GetComponent<TalkPlayer>() != null)
        {
            if (Keyboard.current?[PushActionKey].wasPressedThisFrame ?? false)
            {
                if (!_canAction) return;
                _canAction = false;
                if (_isTalking)
                {
                    controller.PushAction();
                }
                else if (controller.CanReloadThisFrame)
                {
                    WillTalk();
                }
            }
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.GetComponent<TalkPlayer>() != null) OnPlayerExit();
    }
}
