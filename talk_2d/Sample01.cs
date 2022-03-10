using System.Collections.Generic;
using static TalkPlayer;

public class Sample01 : TalkObject
{
    //
    // SkipOn: スキップできるテキスト
    // SkipOff: スキップできないテキスト
    // Clear: ここで文字を全部消す
    //
    public override List<Line> Words
    {
        get => new List<Line>()
        {
            new SkipOn("おはようございます、これは1番目のメッセージです"),
            new SkipOn("こんにちは、これは2番目のメッセージです"),
            new Clear(),
            new SkipOff("こんばんは、これは3番目のメッセージです"),
            new SkipOn("おやすみなさい、これは4番目のメッセージです"),
            new SkipOn("これでメッセージの表示を終わります"),
        };
    }
}
