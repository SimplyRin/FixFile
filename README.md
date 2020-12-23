# FixFile
EPGStation で録画したデータファイルから日付を削除し作成日、更新日の書き換えと不要なタグを削除する物

・ファイル名変更

・作成日、更新日の変更

・ロールバック対応

# セットアップ
・[Release](https://github.com/SimplyRin/FixName/releases) からファイルをダウンロード

・システム環境変数にパスが通っているフォルダに設定しておくとエクスプローラーから実行できるので便利です。たぶん。

# 実行例
![output.gif](https://github.com/SimplyRin/FixName/blob/main/gif/output.gif?raw=true)

# コマンド
```md
使用方法:

-d delete : 指定したワードをファイル名から削除します。
・ , を使うことで複数のワードを削除することができます。
・ | を使うことでワードを別の文字に変更できます。

例: -d delete [字],[デ],[終]|最終回！
上記の場合、'[終]' が '最終回！' に変更されます。

-rollback : 変換後に生成されたファイル(rollback.csv)を使用して元のファイル名にロールバックします。
```
