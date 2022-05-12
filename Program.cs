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
    class IniFile // Class quản lý các file cài đặt Bot
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
    class botObj // Class quản lý các thuộc tính và phương thức của Bot
    {
        const string key = "5209198819:AAGndggSzo4SPzofLOrq3wuoZiEewVGx96w";
        const string url_api = "https://api.telegram.org/bot" + key;
        string message_id_bot = "";
        bool error = false;
        int time_wait = 60;

        static userObj user = new userObj();

        DateTime start = DateTime.Now;

        /// Trả về dữ liệu lấy được từ đường link url theo phương thức get
        private string getData(string url)
        {
            HttpRequest http = new HttpRequest();
            http.Cookies = new CookieDictionary();

            string html = http.Get(url).ToString();

            return html;
        }

        /// Trả về dữ liệu được cập nhật từ Bot qua đường link url_update 
        /// theo phương thức get với tham số là chuỗi offset
        private string getUpdates(string offset = "")
        {
            string url_update = url_api + "/getUpdates?offset=" + offset;
            HttpRequest http = new HttpRequest();
            http.Cookies = new CookieDictionary();
            string result = http.Get(url_update).ToString();
    
            return result;
        }

        /// Cập nhật dữ liệu mới nhất theo tham số update_id
        void getNewUpdatesMess(string update_id)
        {
            if (update_id != "") getUpdates((Convert.ToUInt64(update_id) + 1).ToString());
        }

        /// Trả về message_id được xử lý Regex từ chuỗi source
        string getMessage_Id_bot(string source)
        {
            string message_id;
            Regex reg = new Regex("\"message_id\":(?<message_id>\\d+)");
            Match res = reg.Match(source);

            message_id = res.Groups["message_id"].ToString();
            
            return message_id;
        }

        /// Gửi tin nhắn thông qua Bot 
        /// Tham số message: Nội dung tin nhắn; Tham số optional: Tùy chọn kèm theo 
        void sendMessage(string message, string optional = "")
        {
            HttpRequest http = new HttpRequest();
            http.Cookies = new CookieDictionary();

            string data = "{\"chat_id\":\"" + user.Chat_id + "\", \"text\":\"" + message + "\"," + optional + "}";

            string url_post = url_api + "/sendMessage";

            string res = http.Post(url_post, data, "application/json").ToString();

            message_id_bot = getMessage_Id_bot(res);
        }

        /// Trả về update_id mới nhất lấy từ tham số source bằng Regex
        string getLatesUpdateID(string source)
        {
            Regex reg = new Regex("\"update_id\":(?<update_id>\\d+)");

            MatchCollection res = reg.Matches(source);

            int length = res.Count;

            if (length == 0) return "";

            return res[length - 1].Groups["update_id"].ToString();
        }

        /// Trả về tin nhắn được gửi gần nhất lấy từ tham số source bằng Regex
        string getLatesMessage(string source)
        {
            Regex reg = new Regex("\"text\":\"(?<text>.+)\"");

            MatchCollection res = reg.Matches(source);

            int length = res.Count;

            if (length == 0) return "";

            return res[length - 1].Groups["text"].ToString();
        }

        /// Xử lý dữ liệu source lấy từ hàm getData() nhằm lấy ra các thông tin về Bitcoin:
        /// Giá hiện tại, giá nhỏ và lớn nhất trong vòng 24h, biên độ giá trong vòng 24h và khối lượng BTC giao dịch trong 24h.
        /// Dữ liệu được lấy từ API của Binance
        void getFullPrice(ref double price, ref double high, ref double low, ref double volume, ref double hr24, ref double per24hr)
        {
            string url = "https://api.binance.com/api/v3/ticker/24hr?symbol=BTCUSDT";
            string source = getData(url);

            Regex reg = new Regex("\"priceChange\":\"(?<hr24>.+?)\",\"priceChangePercent\":\"(?<per24hr>.+?)\".*?\"askPrice\":\"(?<price>.+?)\".*?\"highPrice\":\"(?<high>.+?)\",\"lowPrice\":\"(?<low>.+?)\",\"volume\":\"(?<volume>.+?)\"");
            Match res = reg.Match(source);

            string price_str = res.Groups["price"].ToString();
            if (price_str != "") price = Convert.ToDouble(price_str);

            string high_str = res.Groups["high"].ToString();
            if (high_str != "") high = Convert.ToDouble(high_str);

            string low_str = res.Groups["low"].ToString();
            if (low_str != "") low = Convert.ToDouble(low_str);

            string volume_str = res.Groups["volume"].ToString();
            if (volume_str != "") volume = Convert.ToDouble(volume_str);

            string hr24_str = res.Groups["hr24"].ToString();
            if (hr24_str != "") hr24 = Convert.ToDouble(hr24_str);

            string per24hr_str = res.Groups["per24hr"].ToString();
            if (per24hr_str != "") per24hr = Convert.ToDouble(per24hr_str);
        }

        /// Trả về giá hiện tại của Bitcoin 
        /// Dữ liệu được lấy từ API của Binance
        double getPrice()
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

        /// Trả về chuỗi callback_data lấy từ tham số source
        /// để kiểm tra Bot thực hiện chức năng nào
        string getCallBackData(string source)
        {
            Regex reg = new Regex("\"data\":\"(?<callback_data>\\w+)\"");
            MatchCollection res = reg.Matches(source);

            int count = res.Count;
            if (count == 0)
            {
                return "";
            }
            string callback_data = res[res.Count - 1].Groups["callback_data"].ToString();

            return callback_data;
        }

        /// Trả về lệnh được gửi cho Bot qua Telegram
        string getCommand(string source)
        {
            if (source.Contains("\"id\":")) user.Chat_id = getChatID(source);
            string command = "";
            if (source.Contains("\"type\":\"bot_command\""))
            {
                Regex reg = new Regex("\"text\":\"(?<command>.+?)\"");
                Match res = reg.Match(source);

                command = res.Groups["command"].ToString();
            }
            
            return command;
        }

        /// Gửi tin nhắn mặc định cùng với Inline Keyboard
        void startCommand()
        {
            string optional = "\"reply_markup\": { \"inline_keyboard\": [ [{\"text\":\"Get price of BTC\", \"callback_data\":\"price\"}], [{\"text\":\"Calculate Profit\", \"callback_data\":\"profit\"}], [{\"text\":\"Receive new alert notifications\", \"callback_data\":\"get_warning\"}] ], \"remove_keyboard\":true }";
            sendMessage("This is start message\nPress any buttons bottom to use this bot", optional);
            getNewUpdatesMess(getLatesUpdateID(getUpdates()));
        }

        /// Chỉnh sửa thông tin Inline Keyboard
        void editMessageInline(string text = "", string optional = "")
        {
            string url_edit_inline = url_api + "/editMessageText";
            string data = "{\"chat_id\":\"" + user.Chat_id + "\",\"message_id\":\"" + message_id_bot + "\",\"text\":\"" + text + "\"," + optional + "}";
            HttpRequest http = new HttpRequest();
            http.Cookies = new CookieDictionary();
            http.Post(url_edit_inline, data, "application/json");
        }

        /// Lấy thông tin về cận trên và dưới của giá Bitcoin
        void getLowerBound_UpperBound()
        {
            sendMessage("Price of BTC now is " + getPrice() + "$");

            var myIni = new IniFile(user.Path_ini);

            editBound("LOWER");
            if (!error)
            {
                editBound("UPPER");

            }
            myIni.Write("Upper", user.Upper_bound.ToString(), user.Chat_id);
            myIni.Write("Lower", user.Lower_bound.ToString(), user.Chat_id);
        }

        /// Lấy giá trị cận trên và dưới của giá BTC do người dùng nhập
        /// Tham số bound xác định là cận trên hay dưới
        void editBound(string bound)
        {
            sendMessage("Please send me " + bound + " bound of price BTC you want to watch out (use . to divide number)");
            getNewUpdatesMess(getLatesUpdateID(getUpdates()));
            string bound_str = "";

            while (bound_str == "")
            {
                bound_str = getLatesMessage(getUpdates());
                if (bound_str != "")
                {
                    double tmp;
                    if (bound == "LOWER")
                    {
                        if (double.TryParse(bound_str, out tmp) == true)
                        {
                            user.Lower_bound = tmp;
                            break;
                        }
                        else
                        {
                            user.Lower_bound = tmp;
                            error = true;
                            break;
                        }
                    }
                    else
                    {
                        if (double.TryParse(bound_str, out tmp) == true)
                        {
                            user.Upper_bound = tmp;
                            break;
                        }
                        else
                        {
                            user.Upper_bound = tmp;
                            error = true;
                            break;
                        }
                    }
                }
            }
        }

        /// Chỉnh sửa cận dưới của giá Bitcoin
        void editLowerSingle()
        {
            var myIni = new IniFile(user.Path_ini);
            if (myIni.KeyExists("Lower", user.Chat_id))
            {
                sendMessage("Current lower bound is " + myIni.Read("Lower", user.Chat_id));
            }

            editBound("LOWER");
            if (error)
            {
                string optional = "\"reply_markup\": { \"keyboard\": [ [ { \"text\": \"Toggle warning: " + getState() + "\" } ], [ { \"text\": \"Edit lower bound\" } ], [ { \"text\": \"Edit upper bound\" } ], [ { \"text\": \"Menu\" } ] ] }";
                sendMessage("Try again and select any options bellow ⌨️", optional);
                error = false;
                return;
            }
            else myIni.Write("Lower", user.Lower_bound.ToString(), user.Chat_id);
        }

        /// Chỉnh sửa cận trên của giá Bitcoin
        void editUpperSingle()
        {
            var myIni = new IniFile(user.Path_ini);
            if (myIni.KeyExists("Upper", user.Chat_id))
            {
                sendMessage("Current upper bound is " + myIni.Read("Upper", user.Chat_id));
            }

            editBound("UPPER");
            if (error)
            {
                string optional = "\"reply_markup\": { \"keyboard\": [ [ { \"text\": \"Toggle warning: " + getState() + "\" } ], [ { \"text\": \"Edit lower bound\" } ], [ { \"text\": \"Edit upper bound\" } ], [ { \"text\": \"Menu\" } ] ] }";
                sendMessage("Try again and select any options bellow ⌨️", optional);
                error = false;
                return;
            }
            else myIni.Write("Upper", user.Upper_bound.ToString(), user.Chat_id);
        }

        /// Kiểm tra callback_data ứng với chức năng nào của Bot
        void callBack_Func(string source)
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

        /// Nhận thông báo về giá của Bitcoin sau mỗi 60s
        /// Gửi thông báo nếu giá nhỏ hơn cận dưới, lớn hơn cận trên hoặc ổn định
        void getWarning()
        {
            if (user.Lower_bound == 0 || user.Upper_bound == 0) return;
            double price = getPrice();
            double time = Convert.ToDouble(DateTime.Now.Subtract(start).TotalMilliseconds);
            if (price < user.Lower_bound)
            {
                if (user.Noti == "" || user.Noti == "UPPER" || (user.Noti == "LOWER" && time >= (time_wait * 1000)))
                {
                    sendMessage(" ↘️ Price of BTC is LOWER than " + user.Lower_bound + "$");
                    user.Noti = "LOWER";
                    start = DateTime.Now;
                    Console.WriteLine("Lower notification");
                }

            }
            else if (price > user.Upper_bound)
            {
                if (user.Noti == "" || user.Noti == "LOWER" || (user.Noti == "UPPER" && time >= (time_wait * 1000)))
                {
                    sendMessage(" ↗️ Price of BTC is UPPER than " + user.Upper_bound + "$");
                    user.Noti = "UPPER";
                    start = DateTime.Now;
                    Console.WriteLine("Upper notification");
                }
            }
            else
            {
                if (user.Noti == "UPPER" || user.Noti == "LOWER")
                {
                    sendMessage("Price of BTC is stable now");
                    user.Noti = "";
                    start = DateTime.Now;
                    Console.WriteLine("Stable notification");
                }
            }
        }

        /// Thông báo các thông tin liên quan về giá của Bitcoin
        void price_callBack(string source)
        {
            double price = 0, high = 0, low = 0, volume = 0, hr24 = 0, per24hr = 0;
            getFullPrice(ref price, ref high, ref low, ref volume, ref hr24, ref per24hr);

            string optional = "\"reply_markup\": { \"inline_keyboard\": [ [{\"text\":\"Back to menu\", \"callback_data\":\"back_menu\"}] ] }";
            editMessageInline("**Trading on Binance** :\n 💲 Price of BTC: " + price + "$\n 🚀 24hr: " + hr24 + "$  " + per24hr + "%\n 📈 Highest 24h: " + high + "$\n 📉 Lowest 24h: " + low + "$\n 📊 Volume BTC in 24h: " + volume + " BTC", optional);

            getNewUpdatesMess(getLatesUpdateID(source));
            Console.WriteLine("Price callback");
        }

        /// Tính toán lợi nhuận từ số Bitcoin người dùng nhập
        void profit_callBack(string source)
        {
            getNewUpdatesMess(getLatesUpdateID(getUpdates()));
            string optional = "\"reply_markup\": { \"inline_keyboard\": [ [{\"text\":\"Back to menu\", \"callback_data\":\"back_menu\"}] ] }";

            sendMessage("Please send the amount of BTC you have");
            string btc_str = "";
            while (true)
            {
                btc_str = getLatesMessage(getUpdates());
                if (btc_str != "")
                {
                    double tmp = 0;
                    if (Double.TryParse(btc_str, out tmp) == true)
                    {
                        user.Btc = tmp;
                        double price = getPrice();
                        sendMessage("Profit up to now: " + price * user.Btc + "$");
                        startCommand();
                        break;
                    }
                    else
                    {
                        sendMessage("Try again", optional);
                        break;
                    }
                }
            }

            Console.WriteLine("Profit callback");
            getNewUpdatesMess(getLatesUpdateID(source));
        }

        /// In ra menu
        void backMenu_callBack()
        {
            string optional = "\"reply_markup\": { \"inline_keyboard\": [ [{\"text\":\"Get price of BTC\", \"callback_data\":\"price\"}], [{\"text\":\"Calculate Profit\", \"callback_data\":\"profit\"}], [{\"text\":\"Receive new alert notifications\", \"callback_data\":\"get_warning\"}] ] }";

            editMessageInline("This is start message\nPress any buttons bottom to use this bot", optional);
            Console.WriteLine("Back to menu callback");
            getNewUpdatesMess(getLatesUpdateID(getUpdates()));
        }

        /// Cảnh báo về giá của Bitcoin thông qua 2 cận
        /// In ra Keyboard để điều chỉnh thông tin liên quan tới giá BTC, tắt hay bật thông báo hoặc in ra Menu
        void getWarning_callBack(string source)
        {
            Console.WriteLine("Get Warning callback");
            var myIni = new IniFile(user.Path_ini);
            string chat_id = getChatID(source);

            getLowerBound_UpperBound();
            string optional;
            if (!error)
            {
                user.Toggle_warning = true;
                optional = "\"reply_markup\": { \"keyboard\": [ [ { \"text\": \"Toggle warning: " + getState() + "\" } ], [ { \"text\": \"Edit lower bound\" } ], [ { \"text\": \"Edit upper bound\" } ], [ { \"text\": \"Menu\" } ] ] }";
                sendMessage("Success. Select any options bellow ⌨️", optional);
            }
            else
            {
                user.Toggle_warning = false;
                optional = "\"reply_markup\": { \"keyboard\": [ [ { \"text\": \"Toggle warning: OFF\" } ], [ { \"text\": \"Edit lower bound\" } ], [ { \"text\": \"Edit upper bound\" } ], [ { \"text\": \"Menu\" } ] ] }";
                sendMessage("Try again and Select any options bellow ⌨️", optional);
            }
            error = false;
        }

        /// Trả về chat ID
        string getChatID(string source)
        {
            Regex reg = new Regex("\"id\":(?<id>\\d+)");
            Match res = reg.Match(source);
            string id = res.Groups["id"].ToString();
            return id;
        }

        /// Trả về trạng thái của thông báo (Bật hay Tắt)
        string getState()
        {
            if (user.Toggle_warning == true) return "ON";
            else return "OFF";
        }

        /// Kiểm tra tham số message để thực hiện chức năng tương ứng
        void doMessage(string message)
        {
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
                user.Toggle_warning = false;
                string optional = "\"reply_markup\": { \"keyboard\": [ [ { \"text\": \"Toggle warning: " + getState() + "\" } ], [ { \"text\": \"Edit lower bound\" } ], [ { \"text\": \"Edit upper bound\" } ], [ { \"text\": \"Menu\" } ] ] }";
                sendMessage("Toggle Warning: OFF", optional);
                getNewUpdatesMess(getLatesUpdateID(getUpdates()));
            }
            else if (message == "Toggle warning: OFF")
            {
                user.Toggle_warning = true;
                string optional = "\"reply_markup\": { \"keyboard\": [ [ { \"text\": \"Toggle warning: " + getState() + "\" } ], [ { \"text\": \"Edit lower bound\" } ], [ { \"text\": \"Edit upper bound\" } ], [ { \"text\": \"Menu\" } ] ] }";
                sendMessage("Toggle Warning: ON", optional);
                getNewUpdatesMess(getLatesUpdateID(getUpdates()));
            }
        }

        /// Khởi tạo và thực hiện các chức năng của Bot
        void run()
        {
            getNewUpdatesMess(getLatesUpdateID(getUpdates()));

            var myIni = new IniFile(user.Path_ini);
            if (myIni.KeyExists("Lower", user.Chat_id))
            {
                user.Lower_bound = Convert.ToDouble(myIni.Read("Lower", user.Chat_id));
            }
            if (myIni.KeyExists("Upper", user.Chat_id))
            {
                user.Upper_bound = Convert.ToDouble(myIni.Read("Upper", user.Chat_id));
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
                doMessage(message);
                callBack_Func(source);
                if (user.Toggle_warning == true)
                {
                    getWarning();
                }
                Thread.Sleep(800);
            }
        }
        public void test()
        {
            run();
        }
    }
    class userObj // Class quản lý các thông tin của người dùng
    {
        string chat_id = "";
        bool toggle_warning = false;
        double lower_bound = 0;
        double upper_bound = 0;
        string path_ini = "setting.ini";
        string noti = "";
        double btc = 0;
        public string Chat_id
        {
            get { return chat_id; }
            set { chat_id = value; }
        }
        public bool Toggle_warning
        {
            get { return toggle_warning; }
            set { toggle_warning = value; }
        }
        public double Lower_bound
        {
            get { return lower_bound; }
            set { lower_bound = value; }
        }
        public double Upper_bound
        {
            get { return upper_bound; }
            set { upper_bound = value; }
        }
        public string Noti
        {
            get { return noti; }
            set { noti = value; }
        }
        public string Path_ini
        {
            get { return path_ini; }
            set { path_ini = value; }
        }
        public double Btc
        {
            get { return btc; }
            set { btc = value; }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            botObj bot = new botObj();
            bot.test();
            Console.ReadKey();
        }
    }
}