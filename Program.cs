using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using static System.Environment;

namespace FixFile {

    public class Program {

        public static void Main(String[] args) {
            new Program().run(args);
        }

        private String APPFOLDER;
        private String TEMPLATE;

        public void run(String[] args) {
            APPFOLDER = Environment.GetFolderPath(SpecialFolder.ApplicationData) + "\\FixFile";
            Directory.CreateDirectory(APPFOLDER);
            TEMPLATE = APPFOLDER + "\\template.txt";

            String[] del = null;
            if (args.Length > 0) {
                var ar0 = args[0].ToLower();
                if (args.Length > 1) {
                    if (ar0.Equals("-d") || ar0.Equals("-delete")) {
                        var ar = args[1];
                        ar = ar.Replace("\\", "￥");
                        ar = ar.Replace("/", "／");
                        ar = ar.Replace(":", "：");
                        ar = ar.Replace("*", "＊");
                        ar = ar.Replace("?", "？");
                        ar = ar.Replace("<", "＜").Replace(">", "＞");
                        ar = ar.Replace("|", "｜");
                        del = ar.Split(',');
                    }
                }

                if (ar0.Equals("-h") || ar0.Equals("-help")) {
                    Console.WriteLine("使用方法:");
                    Console.WriteLine("\n-d delete : 指定したワードをファイル名から削除します。");
                    Console.WriteLine("・ , を使うことで複数のワードを削除することができます。");
                    Console.WriteLine("・ | を使うことでワードを別の文字に変更できます。");
                    Console.WriteLine("\n例: -d delete [字],[デ],[終]|最終回");
                    Console.WriteLine("上記の場合、'[終]' が '最終回' に変更されます。");
                    Console.WriteLine("\n-t <テンプレート> : -d で指定せずに文字列を削除できます。");
                    Console.WriteLine("・ 例: -t [字],[デ],[終]|最終回,[解],[新]");
                    Console.WriteLine("・ clear でテンプレートを削除します。");
                    Console.WriteLine("・ 設定ファイルは %AppData%/FixFile/ に保存されます。");
                    Console.WriteLine("\n-rollback : 変換後に生成されたファイル(rollback.csv)を使用して元のファイル名にロールバックします。\n");
                    this.read(true);
                    return;
                }

                if (ar0.Equals("-rollback")) {
                    if (!File.Exists("rollback.csv")) {
                        Console.WriteLine("ロールバックファイルが見つかりませんでした。");
                        this.read(true);
                        return;
                    }

                    TextFieldParser parser = new TextFieldParser("rollback.csv", Encoding.UTF8);
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters("|");

                    var rows = new List<String[]>();
                    while (!parser.EndOfData) {
                        String[] row = parser.ReadFields();
                        rows.Add(row);
                    }
                    rows.Reverse();
                    foreach (String[] row in rows) {
                        Console.WriteLine("\n-> " + row[1]);
                        Console.WriteLine("・ファイル名の変更: " + row[0]);

                        if (File.Exists(row[0])) {
                            Console.WriteLine("エラー: 宛先名 " + row[1] + " が既に存在するためファイルは処理されずスキップされました。");
                            continue;
                        }

                        if (File.Exists(row[1])) {
                            File.Move(row[1], row[0]);

                            Console.WriteLine("・作成日変更: " + row[2]);
                            File.SetCreationTime(row[0], Convert.ToDateTime(row[2]));
                            Console.WriteLine("・更新日変更: " + row[3]);
                            File.SetLastWriteTime(row[0], Convert.ToDateTime(row[3]));
                        } else {
                            Console.WriteLine("エラー: " + row[1] + " が見つからなかったため、ファイルは処理されずスキップされました。");
                        }
                    }

                    parser.Close();

                    String uniqueId = Guid.NewGuid().ToString();
                    File.Move("rollback.csv", "rollback-" + uniqueId.Split('-')[0] + ".csv");

                    Console.WriteLine("ロールバックファイルの名前を変更しました。");
                    this.read(true);
                    return;
                }

                if (ar0.Equals("-t") || ar0.Equals("-template")) {
                    if (args.Length > 1) {
                        if (args[1].ToLower().Equals("clear")) {
                            if (File.Exists(TEMPLATE)) {
                                File.Delete(TEMPLATE);
                                Console.WriteLine("ファイルを削除しました。");
                                return;
                            }
                            Console.WriteLine("ファイルが見つかりませんでした。");
                            return;
                        }

                        String value = args[1].Replace("\n", "");

                        var writer = new StreamWriter(TEMPLATE, false, Encoding.UTF8);
                        writer.WriteLine(value);
                        writer.Close();

                        Console.WriteLine("テンプレートを以下の値に設定しました。");
                        Console.WriteLine("・" + value);
                        return;
                    }

                    Console.WriteLine("-t <テンプレート> : 詳しいヘルプは -h で確認してください。");

                    if (File.Exists(TEMPLATE)) {
                        Console.WriteLine("現在設定されているテンプレート: ");
                        StreamReader sr = new StreamReader(TEMPLATE, Encoding.UTF8);
                        String value = sr.ReadToEnd();
                        Console.WriteLine("・" + value);
                    } else {
                        Console.WriteLine("現在テンプレートとして何も設定されていません。");
                    }
                    return;
                }
            }

            String[] delTemplate = null;
            if (File.Exists(TEMPLATE)) {
                StreamReader sr = new StreamReader(TEMPLATE, Encoding.UTF8);
                delTemplate = sr.ReadToEnd().Split(',');
            }

            // -d で指定されたワードとテンプレートを結合
            String[] total = null;
            if (del != null && delTemplate != null) {
                total = new String[del.Length + delTemplate.Length];
                del.CopyTo(total, 0);
                delTemplate.CopyTo(total, del.Length);
            } else if (del != null) {
                total = del;
            } else if (delTemplate != null) {
                total = delTemplate;
            }

            String pattern = "([0-9]{4})年(0[1-9]|1[0-2])月(0[1-9]|[12][0-9]|3[01])日([0-9]{2})時([0-9]{2})分([0-9]{2})秒";
            String csv = "";

            foreach (String file in Directory.GetFiles(".", "*")) {
                String name = this.getName(file);
                var time = name.Split('-')[0];

                var match = Regex.Match(time, pattern);
                if (!match.Success) {
                    continue;
                }

                var groups = match.Groups;

                var year = groups[1];
                var month = groups[2];
                var day = groups[3];

                var h = groups[4];
                var m = groups[5];
                var s = groups[6];

                var strDate = year + "/" + month + "/" + day + " " + h + ":" + m + ":" + s;
                var date = Convert.ToDateTime(strDate);
                var title = Regex.Replace(name, pattern + "-", "");

                Console.WriteLine("\n-> " + name);

                if (total != null) {
                    String tag = ".mp4";
                    if (title.EndsWith(".ts")) {
                        tag = ".ts";
                    }
                    title = title.Replace(".mp4", "").Replace(".ts", "");

                    foreach (String value in total) {
                        var item = this.getReplaceItem(value);
                        var newValue = item.newValue;
                        title = title.Replace(item.oldValue, newValue);
                        Console.WriteLine("・" + item.oldValue + " を" + (newValue.Equals("") ? "削除し" : " " + newValue + " に置き換え") + "ました。");
                    }
                    title = title.Trim() + tag;
                }

                title = title.Trim();

                String create = this.dateTimeToString(File.GetCreationTime(file));
                String lastWrite = this.dateTimeToString(File.GetLastWriteTime(file));
                csv += file + "|" + title + "|" + create + "|" + lastWrite + "\n";

                if (File.Exists(title)) {
                    Console.WriteLine("エラー: 宛先名 " + title + " が既に存在するためファイルは処理されずスキップされました。");
                    continue;
                }

                if (File.Exists(file)) {
                    File.Move(file, title);
                    Console.WriteLine("・ファイル名の変更: " + title);

                    File.SetCreationTime(title, date);
                    File.SetLastWriteTime(title, date);
                    Console.WriteLine("・作成日更新日変更: " + strDate);
                } else {
                    Console.WriteLine("エラー: " + file + " が見つからなかったため、ファイルは処理されずスキップされました。");
                }
            }

            if (csv.Length > 0) {
                // rollback ファイルに追記
                var writer = new StreamWriter("rollback.csv", true, Encoding.UTF8);
                writer.WriteLine(csv);
                writer.Close();
                Console.WriteLine("\nロールバックファイルを保存しました。");
            }

            this.read(true);
        }

        public String dateTimeToString(DateTime time) {
            return time.Year + "/" + time.Month + "/" + time.Day + " " + time.Hour + ":" + time.Minute + ":" + time.Second;
        }

        public String getName(String file) {
            String name;
            if (file.StartsWith(".\\")) {
                name = file.Substring(2);
            } else {
                name = file;
            }
            return name;
        }

        public ReplaceItem getReplaceItem(String value) {
            ReplaceItem item = new ReplaceItem();
            if (value.Contains("|")) {
                item.oldValue = value.Split('|')[0];
                item.newValue = value.Split('|')[1];
            } else {
                item.oldValue = value;
            }
            return item;
        }

        public String read(bool enter) {
            if (enter) {
                Console.WriteLine("\n処理が完了しました。Enter キーを押すと終了します。");
            }
            return Console.ReadLine();
        }

        public class ReplaceItem {
            public String oldValue { get; set; } = null;
            public String newValue { get; set; } = "";
        }

    }

}
