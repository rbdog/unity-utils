using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static TalkPlayer;

public class Sample02 : TalkObject
{
    // プレイヤーと会話できる距離
    public override float MaxDistance { get => 5.0f; }

    // 会話を進めるためのキー Enter
    public override Key PushActionKey { get => Key.Enter; }

    // 会話のセリフ
    public override List<Line> Words
    {
        get => new List<Line>()
        {
            new SkipOn("おはようございます、これはSample2"),
            new SkipOn("こんにちは、これは2番目のメッセージです"),
            new Clear(),
            new SkipOff("こんばんは、これは3番目のメッセージです"),
            new SkipOn("おやすみなさい、これは4番目のメッセージです"),
            new SkipOn("これでメッセージの表示を終わります"),
        };
    }

    // 1文字にずつにかかる秒数 0.02秒
    public override float CharIntervalSec { get => 0.02f; }

    // プレイヤーが近づいてきたとき
    public override void OnPlayerEnter()
    {
        Debug.Log("Enterで話しかける");
    }

    // メッセージが始まるとき
    public override void WillTalk()
    {
        controller.ReloadObject(this);
        controller.ShowTextArea(); // テキストエリアを表示する
        controller.PushAction(); // メッセージを進める
    }

    // メッセージが終わったとき
    public override void DidTalk()
    {
        controller.HideTextArea(); // テキストエリアを非表示にする
    }

    // プレイヤーが離れていったとき
    public override void OnPlayerExit()
    {
        this.DidTalk(); // 会話が終わったことにする
    }
}
