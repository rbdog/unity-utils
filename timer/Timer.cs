using System;
using UnityEngine;

/*
# 使い方

## 準備

    このスクリプト Timer.cs をUnityの中のどこかに置く

## タイマーの生成

    var timer = gameObject.AddComponent<Timer>();

## カウントアップ (0, 1, 2 ...)

    timer.CountUp(1.0f, 
        (time) => 
        {
            // 1秒 に 1回 ここに来る. time には、その時の時間が入っている
            Debug.Log(time);
        }
    );

## カウントダウン (30, 29, 28 ...)

    timer.CountDown(30.0f, 1.0f, 
        (time) => 
        {
            // 1秒 に 1回 ここに来る. time には 残り時間 が入っている
            Debug.Log(time);
        }, 
        () => 
        {
            // 30.0秒 のカウントダウンが終わったらここに来る
            Debug.Log("complete");
        }
    );

## 一時停止

    timer.Pause(
        (time)=>
        {
            // time には止まった時の時間が入っている
            Debug.Log(time);
        }
    );

## 一時停止から再開

    timer.Resume();

## タイマーを捨てる

    timer.Dispose();

*/

enum TimerMode
{
    CountUp,
    CountDown
}

/// Unityで時間を測るためのタイマー
public class Timer : MonoBehaviour
{
    /// モード
    private TimerMode mode = TimerMode.CountDown;
    /// 時を刻んでいるかどうか
    private bool isTicking = false;
    /// 今何秒時点か
    private float countTime = 0.0f;
    /// loopActionの実行インターバル
    private float loopActionInterval = 1.0f;
    /// 次のloopAction実行までの残り時間
    private float loopActionTimeLeft = 1.0f;
    /// 一定秒毎に一回実行するアクション
    private Action<float> loopAction = null;
    /// 完了時のアクション
    private Action completeAction = null;

    // -----------------------------------------------------------------------------------------------------------------------------------
    /// 全ての状態をリセットする
    private void resetAllStatus()
    {
        mode = TimerMode.CountDown;
        isTicking = false;
        countTime = 0.0f;
        loopActionInterval = 1.0f;
        loopActionTimeLeft = 1.0f;
        loopAction = null;
        completeAction = null;
    }
    // -----------------------------------------------------------------------------------------------------------------------------------
    /// カウントアップを0から開始
    public void CountUp(float actionInterval, Action<float> loopAction)
    {
        resetAllStatus(); // 全ての状態をリセット
        mode = TimerMode.CountUp;
        isTicking = true;
        countTime = 0.0f;
        loopActionInterval = actionInterval;
        loopActionTimeLeft = actionInterval;
        this.loopAction = loopAction;
        completeAction = null;
    }
    // -----------------------------------------------------------------------------------------------------------------------------------
    /// カウントダウンをtimeから開始
    public void CountDown(float startTime, float actionInterval, Action<float> loopAction, Action completeAction)
    {
        resetAllStatus(); // 全ての状態をリセット
        mode = TimerMode.CountDown;
        isTicking = true;
        countTime = startTime;
        loopActionInterval = actionInterval;
        loopActionTimeLeft = actionInterval;
        this.loopAction = loopAction;
        this.completeAction = completeAction;
    }
    // -----------------------------------------------------------------------------------------------------------------------------------
    /// 一時停止
    public void Pause(Action<float> stopAction)
    {
        isTicking = false;
        stopAction(countTime);
    }
    // -----------------------------------------------------------------------------------------------------------------------------------
    /// 一時停止からの再開
    public void Resume()
    {
        isTicking = true;
    }
    // -----------------------------------------------------------------------------------------------------------------------------------
    /// 使い終わった タイマー を捨てる
    public void Dispose()
    {
        Destroy(this);
    }
    // -----------------------------------------------------------------------------------------------------------------------------------
    // Update is called once per frame
    void Update()
    {
        // タイマー動作中のみ実行
        if (isTicking)
        {
            // カウントアップ中
            if (mode == TimerMode.CountUp)
            {
                countTime += Time.deltaTime;
            }
            // カウントダウン中
            else if (mode == TimerMode.CountDown)
            {
                countTime -= Time.deltaTime;
            }

            loopActionTimeLeft -= Time.deltaTime; // 次のloopActionまでの時間を減少

            // カウントダウン終了かどうか判定
            if (mode == TimerMode.CountDown && countTime < 0)
            {
                isTicking = false;

                // nullチェック
                if (completeAction != null)
                {
                    completeAction(); // 完了時のアクションを実行
                }
            }

            // loopActionまでの時間が0を切った
            else if (loopActionTimeLeft <= 0)
            {
                loopActionTimeLeft = loopActionInterval; // 次のloopTimeまでの時間をリセット

                // nullチェック
                if (loopAction != null)
                {
                    loopAction(countTime); // loopActionを実行
                }
            }
        }
    }
}
