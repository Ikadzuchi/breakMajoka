タカラトミー社による玩具「マジョカアイリス」に搭載されているSDカードに含まれる「.savi」ファイルを扱うプログラムです。

# breakMajoka
saviファイルを1コマごとのjpegとwavに分割します。

Usage:

breakMajoka <saviファイル名>

# composeMajoka
複数のjpegファイルからsaviファイルを生成します。wavは未対応です。

Usage:

composeMajoka <リストファイル名>

リストファイルは1行1ファイルでJPEGファイル名を並べたものです。

# decodeWav
IMA ADPCMのwavファイルをリニアPCMにデコードするプログラムの作りかけです。

マジョカアイリスに含まれていたファイルに対して決め打ちの数値で、(wavヘッダのない)リニアPCMの生データを出力します。

# 更新履歴
2021/01/10: 初版
