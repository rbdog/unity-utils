using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
# 使い方

## 準備

    このスクリプトをUnityの中のどこかに置く

## タイマーの生成

    // 新しいタイマーを作って gameObject に持たせる
    var timer = gameObject.AddComponent<KitchenTimer>();


## カウントアップ (0, 2, 4, 6 ...)

    timer.StartCountUp(2.0f, 
        (time) => 
        {
            // 2.0秒に一回ここに来る
            // time には、その時の時間が入っている
            Debug.Log(time);
        }
    );

## カウントダウン (30, 29, 28, 27 ...)

    timer.StartCountDown(30.0f, 2.0f, 
        (time) => 
        {
            // 2.0秒 に 1回 ここに来る
            // time には 残り時間 が入っている
            Debug.Log(time);
        }, 
        () => 
        {
            // 30.0秒 のカウントダウンが終わったらここに来る
            Debug.Log("complete");
        }
    );

## ストップ

    timer.Stop(
        (time)=>
        {
            // time にはタイマーが止まった時の時間が入っている
            Debug.Log(time);
        }
    );

## ストップ時点から再開

    timer.StartFromStopPoint();

## 最初から測り直し

    timer.ResetAndReStart();

## タイマーを捨てる

    timer.Dispose();

*/


/// Unityで時間を測るためのタイマー
public class KitchenTimer: MonoBehaviour
{
    // 計測できる限界時間
    private const float LIMIT_TIME_VALUE = 10f * 60f; // 10分 * 60秒/分
    /// カウントアップ中かカウントダウン中か
    private bool isCountingUp = true;
    /// タイマーは動いてるかどうか
    private bool isRunning = false;
    /// スタートした時の秒を記録
    private float startTimeValue = 0f;
    /// 今何秒時点か
    private float currentTimeValue = 0f;
    /// 次のloopAction実行まで後何秒か
    private float loopActionRestTime = 1;
    /// 何秒毎に一回、loopActionソッドを実行するか
    private float loopActionIntervalTime = 1f;
    /// 一定秒毎に一回実行するアクション
    private Action<float> loopAction = null;
    /// 完了時のアクション
    private Action completeAction = null;

    // -----------------------------------------------------------------------------------------------------------------------------------
    /// 全ての状態をリセットする
    private void resetAllStatus()
    {
        this.isCountingUp = true;
        this.isRunning = false;
        this.startTimeValue = 0f;
        this.currentTimeValue = 0f;
        this.loopActionRestTime = 1f;
        this.loopActionIntervalTime = 1f;
        this.loopAction = null;
        this.completeAction = null;
    }
    // -----------------------------------------------------------------------------------------------------------------------------------
    /// カウントアップを0から開始
    public void StartCountUp(float actionInterval, Action<float> loopAction)
    {
        this.resetAllStatus(); // 全ての状態をリセット
        this.isCountingUp = true;
        this.isRunning = true;
        this.startTimeValue = 0f;
        this.currentTimeValue = 0f;
        this.loopActionRestTime = actionInterval;
        this.loopActionIntervalTime = actionInterval;
        this.loopAction = loopAction;
        this.completeAction = null;
    }
    // -----------------------------------------------------------------------------------------------------------------------------------
    /// カウントダウンをtimeから開始
    public void StartCountDown(float startTime, float actionInterval, Action<float> loopAction, Action completeAction)
    {
        this.resetAllStatus(); // 全ての状態をリセット
        this.isCountingUp = false;
        this.isRunning = true;
        this.startTimeValue = startTime;
        this.currentTimeValue = startTime;
        this.loopActionRestTime = actionInterval;
        this.loopActionIntervalTime = actionInterval;
        this.loopAction = loopAction;
        this.completeAction = completeAction;
    }
    // -----------------------------------------------------------------------------------------------------------------------------------
    /// タイマーストップ
    public void Stop(Action<float> stopAction)
    {
        this.isRunning = false;
        stopAction(this.currentTimeValue);
    }
    // -----------------------------------------------------------------------------------------------------------------------------------
    /// タイマー再開(ストップ時点から)
    public void StartFromStopPoint()
    {
        // スタート時点と、現在の時点がずれている場合のみリスタート扱い
        if (currentTimeValue != this.startTimeValue)
        {
            this.isRunning = true;
        }
    }
    // -----------------------------------------------------------------------------------------------------------------------------------
    /// リセットしてもう一度スタートやり直し
    public void ResetAndReStart()
    {
        this.isRunning = true;
        this.currentTimeValue = this.startTimeValue;
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
        // limit分を超えたら強制エラー終了
        if (LIMIT_TIME_VALUE < this.currentTimeValue || this.currentTimeValue < -LIMIT_TIME_VALUE)
        {
            Debug.LogError("計測時間の限界をオーバーしました");
        }

        // 他のプログラムによってTimeScaleが0以下になっていた場合は、このタイマーを停止状態にする
        if (Time.timeScale <= 0)
        {
            this.isRunning = false; // 停止状態
        }
        
        // タイマー動作中のみ実行
        if (this.isRunning) 
        {
            // カウントアップ中
            if (this.isCountingUp) 
            {
                this.currentTimeValue += Time.deltaTime; // 現在の時点を増加
            }
            // カウントダウン中
            else if (!this.isCountingUp)
            {
                this.currentTimeValue -= Time.deltaTime; // 現在の時点を減少
            }

            this.loopActionRestTime -= Time.deltaTime; // 次のloopActionまでの時間を減少

            // カウントダウン終了かどうか判定
            if (this.currentTimeValue < 0)
            {
                // nullチェック
                if (this.completeAction != null)
                {
                    this.completeAction(); // 完了時のアクションを実行
                }

                this.isRunning = false;
                this.loopAction = null;
                this.completeAction = null;
            } 
            // loopActionまでの時間が0を切った
            else if (this.loopActionRestTime <= 0)
            {
                this.loopActionRestTime = this.loopActionIntervalTime; // 次のloopTimeまでの時間をリセット
                // nullチェック
                if (this.loopAction != null)
                {
                    this.loopAction(this.currentTimeValue); // loopActionを実行
                }
            }
        }   
    }
}
