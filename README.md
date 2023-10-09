## 概要

TypingSystemはタイピングゲームの開発をサポートするライブラリです。  
平仮名、アルファベット、数字、記号を含んだ文字列をゼロアロケーションでローマ字で入力可能な文字列に変換します。  
ライブラリの挙動を確認したい方は、[WebGLデモ](https://rinqer0203.github.io/TypingSystemWebGL/)を参照してください。

![image](https://github.com/Rinqer0203/TypingSystem/assets/64554381/57554f74-afa6-4e8a-aa72-d8f8836f42ee)

## 特徴

- 入力済みの文字に応じたタイピング情報を生成します。
- インスタンス生成時のバッファ確保によりゼロアロケーションで動作します。
- 変換対象の文字列は平仮名、アルファベット、数字、記号の混合に対応。
- TextMeshProに文字列をセットするときにString生成を回避できる拡張メソッド、バッファを提供。

## セットアップ
### 要件
- C# 9.0以上
- TextMeshPro ([TextMeshPro拡張](https://github.com/Rinqer0203/TypingSystem/tree/main/Assets/TypingSystem/Extensions)を削除すれば導入しなくても動作します)
### インストール
 [最新リリース](https://github.com/Rinqer0203/TypingSystem/releases/latest)からUnityPackageをダウンロード

## 使い方
 ### TypingSystem
 #### 初期化
 ```cs
TypingSystem typingSystem = new TypingSystem();

//平仮名、アルファベット、数字、記号でテキストをセット
typingSystem.SetTypingKanaText("たいぴんぐもじれつ");
 ```

#### 入力チェック
サンプルでは、InputSystemの[onTextInput](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/api/UnityEngine.InputSystem.Keyboard.html#UnityEngine_InputSystem_Keyboard_onTextInput)イベントでキーボードからの入力をchar型で取得しています。
```cs
//入力されたchar型文字をセットされた文字列に対して有効かどうかチェックする
if (typingSystem.CheckInputChar('t'))
{     
}
```

#### タイピング情報
```cs
//セットされたタイピング文字列を現在の入力状況を考慮したローマ字に変換したもの
ReadOnlySpan<char> fullRomajiPatternSpan = typingSystem.FullRomajiPatternSpan;
Debug.Log(fullRomajiPatternSpan.ToString());

//タイピング文字列セット後の有効な入力
ReadOnlySpan<char> validInputSpan = typingSystem.ValidInputSpan;
Debug.Log(validInputSpan.ToString());

//入力可能なローマ字パターン
foreach (ReadOnlySpan<char> pattern in typingSystem.GetPermittedRomajiPatterns())
{
      Debug.Log(pattern.ToString());
}

//セットされたタイピング文字列を入力し終えたか
bool isComplete = typingSystem.IsComplete;

//入力済みのセットされたタイピング文字列の長さ
int inputedKanaLength = typingSystem.InputedKanaLength;
```

### カスタムバッファ

- #### StructBuffer
  Spanでの要素の追加とビューの取得に使う
```cs
//char[64]が内部で確保される。
StructBuffer<char> buffer = new StructBuffer<char>(64);

ReadOnlySpan<char> validInputSpan = typingSystem.ValidInputSpan;

//Spanをそのまま追加できる
buffer.Add(validInputSpan);
//char単体も追加できる
buffer.Add('!');

//追加された要素の配列のビューを取得できる (ArraySegment, ReadOnlySpan, Memory)
ArraySegment<char> segment = buffer.Segment;
ReadOnlySpan<char> span = buffer.Span;
ReadOnlyMemory<char> memory = buffer.Memory;

buffer.Clear();
```

- #### LimitedQueue
  事前に取得したキャパシティを超えたときに古い要素を削除して詰めるキュー  
  サンプルでは入力履歴で使用
```cs
//char[32]が内部で確保される
LimitedQueue<char> buffer = new LimitedQueue<char>(32);

//キューに追加
//キューが上限に達した場合は古い要素を捨てて詰める
buffer.Enqueue('a');

//追加された要素の配列のビューを取得できる (ArraySegment, ReadOnlySpan, Memory)
ArraySegment<char> segment = buffer.Segment;
ReadOnlySpan<char> span = buffer.Span;
ReadOnlyMemory<char> memory = buffer.Memory;

buffer.Clear();
```

### [TextMeshPro拡張](https://github.com/Rinqer0203/TypingSystem/blob/main/Assets/TypingSystem/Extensions/TextMeshProExtensions.cs)
TextMeshProにはchar配列で文字列をセットできる[SetCharArray関数](http://digitalnativestudios.com/textmeshpro/docs/ScriptReference/TextMeshPro-SetCharArray.html)が用意されているので、String生成を避けることができます。
```cs
//SpanをStructBufferに書き込んでArraySegmentで文字列をセットする拡張メソッド
public static void SetCharSpan(this TMP_Text tmp, in ReadOnlySpan<char> text, StructBuffer<char> buffer)

//ArraySegmentで文字列をセットする拡張メソッド
public static void SetCharArraySegment(this TMP_Text tmp, in ArraySegment<char> segment)
```

- RichColorTag
  TextMeshProのリッチテキストカラータグを生成する構造体
```cs
//初期化 (内部でカラータグを生成)
RichTextTag richTextTag = new RichTextTag(Color.Red);
```

```cs
//引数で渡された文字列の先頭から指定された文字数までタグで囲んでバッファに書き込む
public void WriteTaggedTextToBuffer(in ReadOnlySpan<char> text, int charLength, StructBuffer<char> buffer)
```
