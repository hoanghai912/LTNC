using MVNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ConsoleApplication2
{
    class IniFile
    {
        string Path;
        string EXE = Assembly.GetExecutingAssembly().GetName().Name;

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

        public IniFile(string IniPath = null)
        {
            Path = new FileInfo(IniPath ?? EXE + ".ini").FullName;
        }

        public string Read(string Key, string Section = null)
        {
            var RetVal = new StringBuilder(255);
            GetPrivateProfileString(Section ?? EXE, Key, "", RetVal, 255, Path);
            return RetVal.ToString();
        }

        public void Write(string Key, string Value, string Section = null)
        {
            WritePrivateProfileString(Section ?? EXE, Key, Value, Path);
        }

        public void DeleteKey(string Key, string Section = null)
        {
            Write(Key, null, Section ?? EXE);
        }

        public void DeleteSection(string Section = null)
        {
            Write(null, null, Section ?? EXE);
        }

        public bool KeyExists(string Key, string Section = null)
        {
            return Read(Key, Section).Length > 0;
        }
    }
    class Program
    {
        const string api = "5209198819:AAGndggSzo4SPzofLOrq3wuoZiEewVGx96w";
        static string chat_id = "2095713085";
        const string url_api = "https://api.telegram.org/bot" + api;
        static string message_id_bot = "";
        static bool toggle_warning = false;
        static double lower_bound = 0;
        static double upper_bound = 0;
        const string path_ini = "setting.ini";
        static bool error = false;
        static string noti = "";

        static Stopwatch time_line = new Stopwatch();

        static void Main(string[] args)
        {

            //var myIni = new IniFile(path_ini);
            //string lowerBound = myIni.Read("upperBound");
            //double lower = 0;
            //double.TryParse(lowerBound, out lower);
            //Console.WriteLine(lower);

            test();

            Console.ReadKey();
        }

        static void test()
        {
            //
            getNewUpdatesMess(getLatesUpdateID(getUpdates()));
            ////string optional = "\"reply_markup\": { \"inline_keyboard\": [ [{\"text\":\"First Button\", \"callback_data\":\"price\"}], [{\"text\":\"Second Button\", \"callback_data\":\"profit\"}] ] }";
            ////sendMessage("Caption Message", optional);
            ////getLatesMessage();

            var myIni = new IniFile(path_ini);
            if (myIni.KeyExists("ToggleWarning", chat_id))
            {
                toggle_warning = Convert.ToBoolean(myIni.Read("ToggleWarning", chat_id));
            }
            if (myIni.KeyExists("LowerBound", chat_id))
            {
                lower_bound = Convert.ToDouble(myIni.Read("LowerBound", chat_id));
            }
            if (myIni.KeyExists("UpperBound", chat_id))
            {
                upper_bound = Convert.ToDouble(myIni.Read("UpperBound", chat_id));
            }

            while (true)
            {
                
                string source = getUpdates();
                Console.WriteLine("Get Updates");
                string command = getCommand(source);
                if (command == "/start")
                {
                    startCommand();
                    Console.WriteLine("Command message");
                    getNewUpdatesMess(getLatesUpdateID(getUpdates()));
                }

                string message = getLatesMessage(source);
                if (message == "Edit lower bound")
                {
                    editLowerSingle();
                    getNewUpdatesMess(getLatesUpdateID(getUpdates()));
                }
                else if (message == "Edit upper bound")
                {
                    editUpperSingle();
                    getNewUpdatesMess(getLatesUpdateID(getUpdates()));
                }
                else if (message == "Menu")
                {
                    startCommand();
                    getNewUpdatesMess(getLatesUpdateID(getUpdates()));
                }
                else if (message == "Toggle warning: ON")
                {
                    toggle_warning = false;
                    myIni.Write("ToggleWarning", toggle_warning.ToString(), chat_id);
                    string optional = "\"reply_markup\": { \"keyboard\": [ [ { \"text\": \"Toggle warning: " + getState() + "\" } ], [ { \"text\": \"Edit lower bound\" } ], [ { \"text\": \"Edit upper bound\" } ], [ { \"text\": \"Menu\" } ] ] }";
                    sendMessage("Toggle Warning: OFF", optional);
                    getNewUpdatesMess(getLatesUpdateID(getUpdates()));
                }
                else if (message == "Toggle warning: OFF")
                {
                    toggle_warning = true;
                    myIni.Write("ToggleWarning", toggle_warning.ToString(), chat_id);
                    string optional = "\"reply_markup\": { \"keyboard\": [ [ { \"text\": \"Toggle warning: " + getState() + "\" } ], [ { \"text\": \"Edit lower bound\" } ], [ { \"text\": \"Edit upper bound\" } ], [ { \"text\": \"Menu\" } ] ] }";
                    sendMessage("Toggle Warning: ON", optional);
                    getNewUpdatesMess(getLatesUpdateID(getUpdates()));
                }

                callBack_Func(source);
                if (toggle_warning == true)
                {
                    time_line.Start();
                    getWarning();
                }
                
                Thread.Sleep(800);
            }

            //sendMessage("test");

        }

        static string getData(string url)
        {
            HttpRequest http = new HttpRequest();
            http.Cookies = new CookieDictionary();

            string html = http.Get(url).ToString();

            return html;

        }

        static void TestRun(string path, string html)
        {
            File.WriteAllText(path, html);
            Process.Start(path);
        }

        static string getUpdates(string offset = "")
        {
            string url_update = url_api + "/getUpdates?offset=" + offset;
            HttpRequest http = new HttpRequest();
            http.Cookies = new CookieDictionary();
            string result = http.Get(url_update).ToString();

            return result;
        }

        static void getNewUpdatesMess(string update_id)
        {
            if (update_id != "") getUpdates((Convert.ToUInt64(update_id) + 1).ToString());
        }

        static string getMessage_Id_bot(string source)
        {
            string message_id;
            Regex reg = new Regex("\"message_id\":(?<message_id>\\d+)");
            Match res = reg.Match(source);

            message_id = res.Groups["message_id"].ToString();

            return message_id;

        }

        static void sendMessage(string message, string optional = "")
        {
            HttpRequest http = new HttpRequest();
            http.Cookies = new CookieDictionary();

            string data = "{\"chat_id\":\"" + chat_id + "\", \"text\":\"" + message + "\"," + optional + "}";

            string url_post = url_api + "/sendMessage";

            string res = http.Post(url_post, data, "application/json").ToString();

            message_id_bot = getMessage_Id_bot(res);
        }

        static string getLatesUpdateID(string source)
        {

            Regex reg = new Regex("\"update_id\":(?<update_id>\\d+)");

            MatchCollection res = reg.Matches(source);

            int length = res.Count;

            if (length == 0) return "";

            return res[length - 1].Groups["update_id"].ToString();

        }

        static string getLatesMessage(string source)
        {

            Regex reg = new Regex("\"text\":\"(?<text>.+)\"");

            MatchCollection res = reg.Matches(source);

            int length = res.Count;

            if (length == 0) return "";

            return res[length - 1].Groups["text"].ToString();

        }

        static void getFullPrice(ref double price, ref double high, ref double low, ref double volume, ref double hr24, ref double per24hr)
        {
            string url = "https://api.binance.com/api/v3/ticker/24hr?symbol=BTCUSDT";
            string source = getData(url);

            Regex reg = new Regex("\"priceChange\":\"(?<hr24>.+?)\",\"priceChangePercent\":\"(?<per24hr>.+?)\".*?\"askPrice\":\"(?<price>.+?)\".*?\"highPrice\":\"(?<high>.+?)\",\"lowPrice\":\"(?<low>.+?)\",\"volume\":\"(?<volume>.+?)\"");
            Match res = reg.Match(source);

            string price_str = res.Groups["price"].ToString();
            if (price_str != "") price = Convert.ToDouble(price_str);

            //reg = new Regex("\"high\":\"(?<high>\\d+\\.\\d+)\"");
            //res = reg.Match(source);

            string high_str = res.Groups["high"].ToString();
            if (high_str != "") high = Convert.ToDouble(high_str);

            //reg = new Regex("\"low\":\"(?<low>\\d+\\.\\d+)\"");
            //res = reg.Match(source);

            string low_str = res.Groups["low"].ToString();
            if (low_str != "") low = Convert.ToDouble(low_str);

            //reg = new Regex("\"volume\":\"(?<volume>\\d+\\.\\d+)\"");
            //res = reg.Match(source);

            string volume_str = res.Groups["volume"].ToString();
            if (volume_str != "") volume = Convert.ToDouble(volume_str);

            string hr24_str = res.Groups["hr24"].ToString();
            if (hr24_str != "") hr24 = Convert.ToDouble(hr24_str);

            string per24hr_str = res.Groups["per24hr"].ToString();
            if (per24hr_str != "") per24hr = Convert.ToDouble(per24hr_str);

        }

        static double getPrice()
        {
            string url = "https://api.binance.com/api/v3/ticker/24hr?symbol=BTCUSDT";
            string source = getData(url);

            Regex reg = new Regex("\"askPrice\":\"(?<price>\\d+\\.\\d+)\"");
            double price = 0;
            Match res = reg.Match(source);

            string price_str = res.Groups["price"].ToString();
            if (price_str != "") price = Convert.ToDouble(price_str);

            return price;
        }

        static string getCallBackData(string source)
        {

            Regex reg = new Regex("\"data\":\"(?<callback_data>\\w+)\"");
            MatchCollection res = reg.Matches(source);

            int count = res.Count;
            if (count == 0)
            {
                return "";
            }
            string callback_data = res[res.Count - 1].Groups["callback_data"].ToString();
            //Console.WriteLine(callback_data);

            return callback_data;

        }

        static string getCommand(string source)
        {
            if (source.Contains("\"id\":")) chat_id = getChatID(source);
            string command = "";
            if (source.Contains("\"type\":\"bot_command\""))
            {
                Regex reg = new Regex("\"text\":\"(?<command>.+?)\"");
                Match res = reg.Match(source);

                command = res.Groups["command"].ToString();
            }

            return command;
        }

        static void startCommand()
        {
            string optional = "\"reply_markup\": { \"inline_keyboard\": [ [{\"text\":\"Get price of BTC\", \"callback_data\":\"price\"}], [{\"text\":\"Calculate Profit\", \"callback_data\":\"profit\"}], [{\"text\":\"Receive new alert notifications\", \"callback_data\":\"get_warning\"}] ], \"remove_keyboard\":true }";
            sendMessage("This is start message\nPress any buttons bottom to use this bot", optional);
            //getNewUpdatesMess(getLatesUpdateID(getUpdates()));
        }

        static void editMessageInline(string text = "", string optional = "")
        {
            string url_edit_inline = url_api + "/editMessageText";
            string data = "{\"chat_id\":\"" + chat_id + "\",\"message_id\":\"" + message_id_bot + "\",\"text\":\"" + text + "\"," + optional + "}";
            HttpRequest http = new HttpRequest();
            http.Cookies = new CookieDictionary();
            http.Post(url_edit_inline, data, "application/json");
        }

        static void getLowerBound_UpperBound()
        {
            sendMessage("Price of BTC now is " + getPrice() + "$");
            //getNewUpdatesMess(getLatesUpdateID(getUpdates()));

            var myIni = new IniFile(path_ini);
            editBound("LOWER");
            if (!error)
            {
                editBound("UPPER");

            }
            myIni.Write("Upper", upper_bound.ToString(), chat_id);
            myIni.Write("Lower", lower_bound.ToString(), chat_id);
        }

        static void editBound(string bound)
        {
            sendMessage("Please send me " + bound + " bound of price BTC you want to watch out (use . to divide number)");
            getNewUpdatesMess(getLatesUpdateID(getUpdates()));

            string bound_str = "";

            while (bound_str == "")
            {
                bound_str = getLatesMessage(getUpdates());
                if (bound_str != "")
                {
                    if (bound == "LOWER")
                    {
                        if (double.TryParse(bound_str, out lower_bound) == true) break;
                        else
                        {
                            
                            error = true;
                            break;
                        }
                    }
                    else
                    {
                        if (double.TryParse(bound_str, out upper_bound) == true) break;
                        else
                        {
                            
                            error = true;
                            break;
                        }
                    }

                }
            }
        }

        static void editLowerSingle()
        {
            var myIni = new IniFile(path_ini);
            if (myIni.KeyExists("Lower", chat_id))
            {
                sendMessage("Current lower bound is " + myIni.Read("Lower", chat_id));
                //getNewUpdatesMess(getLatesUpdateID(getUpdates()));
            }

            editBound("LOWER");
            if (error)
            {
                string optional = "\"reply_markup\": { \"keyboard\": [ [ { \"text\": \"Toggle warning: " + getState() + "\" } ], [ { \"text\": \"Edit lower bound\" } ], [ { \"text\": \"Edit upper bound\" } ], [ { \"text\": \"Menu\" } ] ] }";
                sendMessage("Try again and select any options bellow ⌨️", optional);
                //getNewUpdatesMess(getLatesUpdateID(getUpdates()));
                error = false;
                return;
            }
            else myIni.Write("Lower", lower_bound.ToString(), chat_id);

        }

        static void editUpperSingle()
        {
            var myIni = new IniFile(path_ini);
            if (myIni.KeyExists("Upper", chat_id))
            {
                sendMessage("Current upper bound is " + myIni.Read("Upper", chat_id));
                //getNewUpdatesMess(getLatesUpdateID(getUpdates()));
            }

            editBound("UPPER");
            if (error)
            {
                string optional = "\"reply_markup\": { \"keyboard\": [ [ { \"text\": \"Toggle warning: " + getState() + "\" } ], [ { \"text\": \"Edit lower bound\" } ], [ { \"text\": \"Edit upper bound\" } ], [ { \"text\": \"Menu\" } ] ] }";
                sendMessage("Try again and select any options bellow ⌨️", optional);
                //getNewUpdatesMess(getLatesUpdateID(getUpdates()));
                error = false;
                return;
            }
            else myIni.Write("Upper", upper_bound.ToString(), chat_id);

        }

        static void callBack_Func(string source)
        {
            string callback_data = getCallBackData(source);
            if (callback_data == "") return;

            if (callback_data == "price")
            {
                price_callBack(source);

            }
            else if (callback_data == "profit")
            {
                profit_callBack(source);
            }

            else if (callback_data == "back_menu")
            {
                backMenu_callBack();
            }

            else if (callback_data == "get_warning")
            {
                getWarning_callBack(source);
            }
        }

        static void getWarning()
        {
            if (lower_bound == 0 || upper_bound == 0) return;
            double price = getPrice();
            if (price < lower_bound)
            {
                time_line.Stop();
                if (noti == "" || noti == "UPPER" || time_line.ElapsedMilliseconds >= (60 * 1000))
                {
                    sendMessage(" ↗️ Price of BTC is LOWER than " + lower_bound + "$");
                    noti = "LOWER";
                    time_line = new Stopwatch();
                }

                //getNewUpdatesMess(getLatesUpdateID(getUpdates()));

            }
            else if (price > upper_bound)
            {
                if (noti == "" || noti == "LOWER" || time_line.ElapsedMilliseconds >= (60 * 1000))
                {
                    sendMessage(" ↘️ Price of BTC is UPPER than " + upper_bound + "$");
                    noti = "UPPER";
                    time_line = new Stopwatch();
                }
                
                //getNewUpdatesMess(getLatesUpdateID(getUpdates()));
            }
            else
            {
                if (noti == "UPPER" || noti == "LOWER")
                {
                    sendMessage("Price of BTC is stable now");
                    noti = "";
                    time_line = new Stopwatch();
                }
            }
        }

        static void price_callBack(string source)
        {
            double price = 0, high = 0, low = 0, volume = 0, hr24 = 0, per24hr = 0;
            getFullPrice(ref price, ref high, ref low, ref volume, ref hr24, ref per24hr);
            string optional = "\"reply_markup\": { \"inline_keyboard\": [ [{\"text\":\"Back to menu\", \"callback_data\":\"back_menu\"}] ] }";
            editMessageInline("**Trading on Binance** :\n 💲 Price of BTC: " + price + "$\n 🚀 24hr: "+hr24+ "$  "+per24hr+"%\n 📈 Highest 24h: " + high + "$\n 📉 Lowest 24h: " + low + "$\n 📊 Volume BTC in 24h: " + volume + " BTC", optional);
            getNewUpdatesMess(getLatesUpdateID(source));
            Console.WriteLine("Price callback");
        }

        static void profit_callBack(string source)
        {
            //

            string optional = "\"reply_markup\": { \"inline_keyboard\": [ [{\"text\":\"Back to menu\", \"callback_data\":\"back_menu\"}] ] }";
            editMessageInline("Profit up to now is 5000$", optional);
            Console.WriteLine("Profit callback");
            getNewUpdatesMess(getLatesUpdateID(source));

        }

        static void backMenu_callBack()
        {
            //

            string optional = "\"reply_markup\": { \"inline_keyboard\": [ [{\"text\":\"Get price of BTC\", \"callback_data\":\"price\"}], [{\"text\":\"Calculate Profit\", \"callback_data\":\"profit\"}], [{\"text\":\"Receive new alert notifications\", \"callback_data\":\"get_warning\"}] ] }, \"remove_keyboard\":\"true\"";

            editMessageInline("This is start message\nPress any buttons bottom to use this bot", optional);
            Console.WriteLine("Back to menu callback");
            getNewUpdatesMess(getLatesUpdateID(getUpdates()));
        }

        static void getWarning_callBack(string source)
        {
            //
            var myIni = new IniFile(path_ini);

            string chat_id = getChatID(source);
            //if (myIni.KeyExists("ToggleWarning", chat_id))
            //{
            //    toggle_warning = Convert.ToBoolean(myIni.Read("ToggleWarning", chat_id));
            //}

            //else
            //{
            //    if (toggle_warning == false)
            //    {
            //        toggle_warning = true;
            //        myIni.Write("ToggleWarning", "true", chat_id);
            //    }
            //    else
            //    {
            //        toggle_warning = false;
            //        myIni.Write("ToggleWarning", "false", chat_id);
            //    }
            //}

            //toggle_warning = true;
            //myIni.Write("ToggleWarning", "true", chat_id);

            //if (toggle_warning == true)
            //{
                //
                getLowerBound_UpperBound();
                string optional = "\"reply_markup\": { \"keyboard\": [ [ { \"text\": \"Toggle warning: "+getState()+"\" } ], [ { \"text\": \"Edit lower bound\" } ], [ { \"text\": \"Edit upper bound\" } ], [ { \"text\": \"Menu\" } ] ] }";
                if (!error) sendMessage("Success. Select any options bellow ⌨️", optional);
                else
                {
                    toggle_warning = false;
                    myIni.Write("ToggleWarning", "false", chat_id);
                    optional = "\"reply_markup\": { \"keyboard\": [ [ { \"text\": \"Toggle warning: OFF\" } ], [ { \"text\": \"Edit lower bound\" } ], [ { \"text\": \"Edit upper bound\" } ], [ { \"text\": \"Menu\" } ] ] }";
                    sendMessage("Try again and Select any options bellow ⌨️", optional);
                }
                
                //getNewUpdatesMess(getLatesUpdateID(getUpdates()));
                error = false;
            //}
        }

        static string getChatID(string source)
        {
            Regex reg = new Regex("\"id\":(?<id>\\d+)");
            Match res = reg.Match(source);
            string id = res.Groups["id"].ToString();

            return id;
            

        }

        static string getState()
        {
            string state = "OFF";
            var myIni = new IniFile(path_ini);
            if (myIni.KeyExists("ToggleWarning", chat_id))
            {
                if (myIni.Read("ToggleWarning", chat_id) == "True")
                {
                    state = "ON";
                }
                else state = "OFF";
            }
            

            return state;
        }

    }


}
