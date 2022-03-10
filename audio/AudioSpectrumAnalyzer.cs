using UnityEngine;
using System.Collections;
using System.Linq;

public class AudioSpectrumAnalyzer
{
    /// バンド分類
    public enum BandClassification
    {
        Bands4,
        Bands10,
        Bands31
    };

    /// バンド (周波数帯)
    class Band
    {
        // スペクトラム中の最小インデックス
        public readonly int minIndex;
        // スペクトラム中の最大インデックス
        public readonly int maxIndex;
        public Band(int minIndex, int maxIndex)
        {
            this.minIndex = minIndex;
            this.maxIndex = maxIndex;
        }
    }

    /* コンパイル時間に決定 */
    /// サンプル数 大きいほど精度と負荷が上がる 2の累乗数 64-8192
    static public readonly int numSamples = 1024;
    /// サンプルする音源のチャンネル
    static public readonly int audioChannel = 0;
    /// 高速フーリエ変換に使う窓関数の種類
    static public readonly FFTWindow fftWindow = FFTWindow.BlackmanHarris;
    /// 1秒あたりの最大衰弱スピード (急激にボリュームが下がるとチカチカするので 表示用の加工に使う)
    static public float maxFallSpeed = 0.50f;

    /* インスタンス生成時に決定 */
    /// バンド分類
    public readonly BandClassification classification;
    /// バンド一覧
    Band[] bands;   

    /// バンド分類ごとのノード周波数たち
    static public float[] NodeFreqs(BandClassification classification)
    {
        switch (classification)
        {
            case BandClassification.Bands4:
                return new float[] { 125.0f, 500, 1000, 2000 };
            case BandClassification.Bands10:
                return new float[] { 31.5f, 63, 125, 250, 500, 1000, 2000, 4000, 8000, 16000 };
            case BandClassification.Bands31:
                return new float[] { 20.0f, 25, 31.5f, 40, 50, 63, 80, 100, 125, 160, 200, 250, 315, 400, 500, 630, 800, 1000, 1250, 1600, 2000, 2500, 3150, 4000, 5000, 6300, 8000, 10000, 12500, 16000, 20000 };
            default:
                return new float[] { };
        }
    }

    /// ノードごとの範囲を決める係数
    static float RangeCoef(BandClassification classification)
    {
        switch (classification)
        {
            case BandClassification.Bands4:
                return 1.414f; // 2^(1/2)
            case BandClassification.Bands10:
                return 1.414f; // 2^(1/2)
            case BandClassification.Bands31:
                return 1.122f; // 2^(1/6)
            default:
                return 0f;
        }
    }

    /// 周波数からスペクトラム中のインデックスを計算する
    static int IndexOfFreq(float freq)
    {
        var index = Mathf.FloorToInt(freq / AudioSettings.outputSampleRate * 2.0f * numSamples);
        return Mathf.Clamp(index, 0, numSamples - 1);
    }

    /// コンストラクタ
    public AudioSpectrumAnalyzer(BandClassification classification)
    {
        // バンド分類を保存
        this.classification = classification;
        // バンド分類から ノード周波数たち, ノードごとの範囲を決める係数 を求める
        var nodeFreqs = AudioSpectrumAnalyzer.NodeFreqs(classification);
        var rangeCoef = AudioSpectrumAnalyzer.RangeCoef(classification);
        // バンドの初期化
        this.bands = new Band[nodeFreqs.Length];
        var bandIndex = 0;
        foreach (var nodeFreq in nodeFreqs)
        {
            var minFreq = nodeFreq / rangeCoef;
            var minIndex = IndexOfFreq(minFreq);
            var maxFreq = nodeFreq * rangeCoef;
            var maxIndex = IndexOfFreq(maxFreq);
            // バンドを作成して保存
            var band = new Band(minIndex, maxIndex);
            this.bands[bandIndex] = band;
            // インクリメント
            bandIndex++;
        }
    }

    /// バンドごとの最大ボリュームを取得する
    public float[] GetMaxVolumes(float[] rawSpectrum)
    {
        var volumes = new float[bands.Length];
        // 全てのバンドに対して
        var bandIndex = 0;
        foreach (var band in bands)
        {
            // 担当範囲のスペクトラムのうち最大ボリューム
            var skipCount = band.minIndex;
            var takeCount = band.maxIndex - band.minIndex + 1;
            var maxVolume = rawSpectrum.Skip(skipCount).Take(takeCount).Max();
            // 値を保存
            volumes[bandIndex] = maxVolume;
            // インクリメント
            bandIndex++;
        }
        return volumes;
    }

    /// バンドごとの最大ボリュームから 表示用に加工されたボリューム を計算する
    public float[] GetVisualVolumes(float[] maxVolumes, float[] oldVisualVolumes)
    {
        var volumes = new float[maxVolumes.Length];
        /// 急激なボリュームダウンを防ぐための最大変化量
        var maxFallVolume = maxFallSpeed * Time.deltaTime;
        // 全ての最大ボリュームに対して
        var bandIndex = 0;
        foreach (var maxVolume in maxVolumes)
        {
            // 表示用に加工したボリュームを保存
            var enhanced = maxVolume * 5 * (bandIndex * bandIndex + 1); // 周波数(インデックス)が大きいものは強調されたボリューム
            var manualEstimated = oldVisualVolumes[bandIndex] - maxFallVolume; // 最大変化量を使って人工的に見積もったボリューム
            var natualVolume = Mathf.Max(manualEstimated, enhanced); // 強調されたボリュームが小さすぎる場合は見積もったボリュームの方を採用する
            // 値を保存
            volumes[bandIndex] = enhanced;
            // インクリメント
            bandIndex++;
        }
        return volumes;
    }
}

/// <summary>
/// MusicPlayer
/// </summary>
public class MusicPlayer : MonoBehaviour
{
    /// 音のコンポーネント
    AudioSource audioSource;
    /// 解析されたボリュームが流れ込んでくる配列 低い音=周波数が小さい順
    float[] rawSpectrum = new float[AudioSpectrumAnalyzer.numSamples];
    /// バンドごとの最大ボリューム
    float[] rawMaxVolumes;
    /// 表示用の補正を補正をかけられたボリューム
    float[] visualVolumes;
    /// 解析装置
    AudioSpectrumAnalyzer analyzer;

    /// アクセス用 バンドごとの最大ボリューム
    public float[] Volumes
    {
        get { return rawMaxVolumes; }
    }

    /// アクセス用 表示用の補正を補正をかけられたボリューム
    public float[] VisualVolumes
    {
        get { return visualVolumes; }
    }

    public virtual string FilePath()
    {
        return "no music file";
    }

    public virtual bool AutoPlay()
    {
        return false;
    }

    public virtual AudioSpectrumAnalyzer.BandClassification Classification()
    {
        return AudioSpectrumAnalyzer.BandClassification.Bands10;
    }

    public virtual void OnUpdateRawMaxVolumes(float[] volumes)
    {
        /* DO NOTHING */
    }

    public virtual void OnUpdateVisualVolumes(float[] volumes)
    {
        /* DO NOTHING */
    }

    /// 音楽再生 & 再生測定開始
    public void Play()
    {
        this.audioSource.Play();
    }

    /// 音楽停止 & 測定停止
    public void Stop()
    {
        this.audioSource.Stop();
    }

    void Start()
    {
        // 解析装置を初期化
        var classification = this.Classification();
        this.analyzer = new AudioSpectrumAnalyzer(classification);
        // バンドごとのボリュームを初期化
        var bandCount = AudioSpectrumAnalyzer.NodeFreqs(classification).Length;
        this.rawMaxVolumes = new float[bandCount];
        this.visualVolumes = new float[bandCount];

        // 必要なコンポーネントを付与
        this.audioSource = this.gameObject.AddComponent<AudioSource>();
        // 音源ファイルを読み込む
        var path = FilePath();
        var clip = Resources.Load<AudioClip>(path);
        if (clip == null)
        {
            Debug.Log($"音源ファイルが見つかりません Resources/{path}");
        }
        this.audioSource.clip = clip;

        if (this.AutoPlay())
        {
            this.Play();
        }   
    }

    void Update()
    {
        /// 音楽再生中のみ処理する
        if (this.audioSource.isPlaying)
        {
            // スペクトラムを更新する
            this.audioSource.GetSpectrumData(rawSpectrum, AudioSpectrumAnalyzer.audioChannel, AudioSpectrumAnalyzer.fftWindow);
            // バンドごとの最大ボリュームを更新
            this.rawMaxVolumes = analyzer.GetMaxVolumes(this.rawSpectrum);
            this.OnUpdateRawMaxVolumes(this.rawMaxVolumes);
            // バンドごとの表示用に加工されたボリュームを更新
            this.visualVolumes = analyzer.GetVisualVolumes(this.rawMaxVolumes, this.visualVolumes);
            this.OnUpdateVisualVolumes(this.visualVolumes);
        }
    }
}
