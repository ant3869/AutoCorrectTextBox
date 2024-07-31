using System;
using System.Collections.Generic;

namespace AutoCorrectLibrary
{
    public static class TextHelper
    {
        private static Dictionary<string, string> _corrections = new Dictionary<string, string>
        {
            { "teh", "the" },
            { "recieve", "receive" },
            { "adress", "address" },
            { "occurance", "occurrence" },
            { "definately", "definitely" },
            { "seperate", "separate" },
            { "untill", "until" },
            { "wich", "which" },
            { "comming", "coming" },
            { "alot", "a lot" },
            { "agian", "again" },
            { "thier", "their" },
            { "acheive", "achieve" },
            { "buisness", "business" },
            { "calender", "calendar" },
            { "concious", "conscious" },
            { "enviroment", "environment" },
            { "existance", "existence" },
            { "goverment", "government" },
            { "happend", "happened" },
            { "harrass", "harass" },
            { "independant", "independent" },
            { "neccessary", "necessary" },
            { "occured", "occurred" },
            { "posession", "possession" },
            { "publically", "publicly" },
            { "reccommend", "recommend" },
            { "suprise", "surprise" },
            { "tommorow", "tomorrow" },
            { "wierd", "weird" },
            { "accomodate", "accommodate" },
            { "acheivement", "achievement" },
            { "arguement", "argument" },
            { "attendence", "attendance" },
            { "catagory", "category" },
            { "collegue", "colleague" },
            { "committment", "commitment" },
            { "corperate", "corporate" },
            { "curiculum", "curriculum" },
            { "develope", "develop" },
            { "emplyee", "employee" },
            { "equiptment", "equipment" },
            { "guarentee", "guarantee" },
            { "heirarchy", "hierarchy" },
            { "intergrate", "integrate" },
            { "maintainance", "maintenance" },
            { "managment", "management" },
            { "oppurtunity", "opportunity" },
            { "prefered", "preferred" },
            { "priviledge", "privilege" },
            { "proffesional", "professional" },
            { "relevent", "relevant" },
            { "responce", "response" },
            { "restaraunt", "restaurant" },
            { "schedual", "schedule" },
            { "secratary", "secretary" },
            { "succesful", "successful" },
            { "supercede", "supersede" },
            { "tendancy", "tendency" },
            { "visable", "visible" },
            { "technicaly", "technically" },
            { "funtion", "function" },
            { "seting", "setting" },
            { "configration", "configuration" },
            { "resoluton", "resolution" },
            { "crucialy", "crucially" },
            { "problematc", "problematic" },
            { "manuever", "maneuver" },
            { "specifc", "specific" },
            { "browswer", "browser" },
            { "instalation", "installation" },
            { "updat", "update" },
            { "utilisation", "utilization" },
            { "developement", "development" },
            { "performence", "performance" },
            { "requirment", "requirement" },
            { "admistrator", "administrator" },
            { "compatibile", "compatible" },
            { "configeration", "configuration" },
            { "credenitals", "credentials" },
            { "deafult", "default" },
            { "defualt", "default" },
            { "destop", "desktop" },
            { "disconect", "disconnect" },
            { "enviornment", "environment" },
            { "excute", "execute" },
            { "exeption", "exception" },
            { "explorerer", "explorer" },
            { "extention", "extension" },
            { "fuction", "function" },
            { "firewal", "firewall" },
            { "instal", "install" },
            { "instll", "install" },
            { "interace", "interface" },
            { "laptoppp", "laptop" },
            { "maintainance", "maintenance" },
            { "netwrk", "network" },
            { "permisson", "permission" },
            { "prefrences", "preferences" },
            { "protocal", "protocol" },
            { "publisch", "publish" },
            { "rebooot", "reboot" },
            { "rechage", "recharge" },
            { "resouces", "resources" },
            { "restablish", "reestablish" },
            { "scrool", "scroll" },
            { "setings", "settings" },
            { "softwere", "software" },
            { "syncronize", "synchronize" },
            { "uninstalll", "uninstall" },
            { "updatte", "update" },
            { "usser", "user" },
            { "utilites", "utilities" },
            { "virrus", "virus" },
            { "voulme", "volume" },
            { "warrning", "warning" },
            { "winows", "windows" },
            { "wirless", "wireless" },
            { "worksttion", "workstation" },
            { "applicaton", "application" },
            { "authenication", "authentication" },
            { "autorization", "authorization" },
            { "avialable", "available" },
            { "calibrateion", "calibration" },
            { "commmand", "command" },
            { "compability", "compatibility" },
            { "compresssion", "compression" },
            { "conectivity", "connectivity" },
            { "contoller", "controller" },
            { "creat", "create" },
            { "deteced", "detected" },
            { "diagnositic", "diagnostic" },
            { "docment", "document" },
            { "emeail", "email" },
            { "encrption", "encryption" },
            { "execcute", "execute" },
            { "firmare", "firmware" },
            { "initiallize", "initialize" },
            { "intgration", "integration" },
            { "intial", "initial" },
            { "loggging", "logging" },
            { "modfied", "modified" },
            { "perfrmance", "performance" },
            { "preformance", "performance" },
            { "proceessor", "processor" },
            { "recieve", "receive" },
            { "registrtion", "registration" },
            { "reliablity", "reliability" },
            { "responce", "response" },
            { "secirty", "security" },
            { "sofware", "software" },
            { "succesfuly", "successfully" },
            { "syncronize", "synchronize" },
            { "temprary", "temporary" },
            { "throughtput", "throughput" },
            { "tranfser", "transfer" },
            { "unresponsivee", "unresponsive" },
            { "utilisation", "utilization" }
        };

        public static int LevenshteinDistance(string a, string b)
        {
            int[,] costs = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++)
                costs[i, 0] = i;
            for (int j = 0; j <= b.Length; j++)
                costs[0, j] = j;
            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;
                    costs[i, j] = Math.Min(Math.Min(costs[i - 1, j] + 1, costs[i, j - 1] + 1), costs[i - 1, j - 1] + cost);
                }
            }
            return costs[a.Length, b.Length];
        }

        public static List<string> GenerateTypoVariations(string word)
        {
            List<string> variations = new List<string>();

            // Swap adjacent characters
            for (int i = 0; i < word.Length - 1; i++)
            {
                char[] chars = word.ToCharArray();
                char temp = chars[i];
                chars[i] = chars[i + 1];
                chars[i + 1] = temp;
                variations.Add(new string(chars));
            }

            // Missing characters
            for (int i = 0; i < word.Length; i++)
            {
                string variation = word.Remove(i, 1);
                variations.Add(variation);
            }

            // Add more variations as needed

            return variations;
        }

        public static string ContextualAutoCorrect(string input)
        {
            string[] words = input.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                if (_corrections.ContainsKey(word.ToLower()))
                {
                    words[i] = _corrections[word.ToLower()];
                }
                else
                {
                    List<string> variations = GenerateTypoVariations(word);
                    string closestMatch = word;
                    int minDistance = int.MaxValue;

                    foreach (string variation in variations)
                    {
                        foreach (string correctWord in _corrections.Values)
                        {
                            int distance = LevenshteinDistance(variation, correctWord);
                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                closestMatch = correctWord;
                            }
                        }
                    }

                    if (minDistance <= 2) // Arbitrary threshold, adjust as needed
                    {
                        words[i] = closestMatch;
                    }
                }

                // Contextual analysis can be added here using n-grams or a language model
                // For simplicity, it's omitted in this example
            }

            return string.Join(" ", words);
        }
    }
}
