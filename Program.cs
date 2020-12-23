using Microsoft.VisualBasic.FileIO;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace FixFile {

    public class Program {

        public static void Main(String[] args) {
            new Program().run(args);
        }

        public void run(String[] args) {
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
                    Console.WriteLine("\n例: -d delete [字],[デ],[終]|最終回！");
                    Console.WriteLine("上記の場合、'[終]' が '最終回！' に変更されます。");
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

                    while (!parser.EndOfData) {
                        String[] row = parser.ReadFields();

                        Console.WriteLine("\n-> " + row[1]);
                        Console.WriteLine("・ファイル名の変更: " + row[0]);

                        File.Move(row[1], row[0]);

                        Console.WriteLine("・作成日変更: " + row[2]);
                        File.SetCreationTime(row[0], Convert.ToDateTime(row[2]));
                        Console.WriteLine("・更新日変更: " + row[3]);
                        File.SetLastWriteTime(row[0], Convert.ToDateTime(row[3]));
                    }
                    return;
                }
            }

            var pattern = "([0-9]{4})年(0[1-9]|1[0-2])月(0[1-9]|[12][0-9]|3[01])日([0-9]{2})時([0-9]{2})分([0-9]{2})秒";
            var csv = "";

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

                if (del != null) {
                    title = title.Replace(".mp4", "");
                    foreach (String value in del) {
                        var item = this.getReplaceItem(value);
                        var newValue = item.newValue;
                        title = title.Replace(item.oldValue, newValue);
                        Console.WriteLine("・" + item.oldValue + " を" + (newValue.Equals("") ? "削除し" : " " + newValue + " に置き換え") + "ました。");
                    }
                    title = title.Trim() + ".mp4";
                }

                title = title.Trim();

                String create = this.dateTimeToString(File.GetCreationTime(file));
                String lastWrite = this.dateTimeToString(File.GetLastWriteTime(file));
                csv += file + "|" + title + "|" + create + "|" + lastWrite + "\n";

                File.Move(file, title);
                Console.WriteLine("・ファイル名の変更: " + title);

                File.SetCreationTime(title, date);
                File.SetLastWriteTime(title, date);
                Console.WriteLine("・作成日更新日変更: " + strDate);
            }

            if (csv.Length > 0) {
                var writer = new StreamWriter("rollback.csv", false, Encoding.UTF8);
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
