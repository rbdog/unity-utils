using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static AudioSpectrumAnalyzer;

// README.md
/*------------------------------------------------------

  # 1. AudioSpectrumAnalyzer.cs を好きな場所に置いておく

  # 2. 再生したい音楽ファイルを Assets/Resources/ 配下に置いておく

  # 3. おいた場所を FilePath() メソッドの中に書く

  # 4. このスクリプト AudioVisualizer.cs を好きなオブジェクトに貼り付ける

  # 5. Unity の画面で メーターを表すオブジェクトたち(meters) を登録する



  # 別のスクリプトから再生する場合
    ```
    public AudioVisualizer av;

    av.Play(); // 音楽を再生する
    av.Stop(); // 音楽を停止する
    ```
------------------------------------------------------*/

/// 音楽をメーターで表すコンポーネント
public class AudioVisualizer : MusicPlayer
{

    /// メーターを表すオブジェクトたち
    /// Unityの画面から設定する
    /// 
    public GameObject[] meters;


    /// 音源ファイルの場所
    /// 例えば xxx.wav ファイルを Assets/Resources/aaa/bbb/ においたとする
    /// 指定するパスは "aaa/bbb/xxx"
    /// 
    public override string FilePath()
    {
        return "aaa/bbb/xxx";
    }

    
    /// ゲームスタート時に自動的に再生するかどうか
    /// 
    public override bool AutoPlay()
    {
        return true;
    }

    
    /// 取得するボリュームの数 4, 10, 31 から選べる
    /// 
    public override BandClassification Classification()
    {
        return BandClassification.Bands10;
    }


    /// 更新 加工されていない生のボリューム
    /// 
    public override void OnUpdateRawMaxVolumes(float[] volumes)
    {
        // 加工前のボリュームはここで使用できます 自分で加工しない場合は無視して構いません
    }


    /// 更新 表示用に加工されたボリューム
    /// 
    public override void OnUpdateVisualVolumes(float[] volumes)
    {
        // 全てのメーターに対して処理をする
        int n = 0;
        foreach (var meter in meters)
        {
            // n番目のボリュームを取得
            var volume = volumes[n];
            // メーターの高さ(Y座標)をボリュームにする
            var x = meter.transform.localScale.x;
            var y = volume;
            var z = meter.transform.localScale.z;
            meter.transform.localScale = new Vector3(x, y, z);
            // インクリメント
            n++;
        }
    }
}
