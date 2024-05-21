using System;
using System.Collections.Generic;
using System.IO;
using MCGalaxy;
using MCGalaxy.Events.ServerEvents;
using MCGalaxy.Events.PlayerEvents;


// REQUIRES THE LATEST DEVELOPMENT BUILD OF MCGALAXY!!!!!!!!!!!!!!!!!!!!!!!!!!!

// tone indicators were helped by goodly in the other version but this doesnt use the same code in this version but it still helped me thank u!!!

// You will need to install the latest release of .NET
// https://dotnet.microsoft.com/en-us/download/dotnet/8.0


namespace PluginFancyChat
{
    public sealed class FancyChat : Plugin 
    {
        public override string name { get { return "FancyChat"; } }

        public override string MCGalaxy_Version { get { return "1.9.4.9"; } }

        public override string creator { get { return "AllergenX"; } }

        public override void Load(bool startup) {
            OnChatEvent.Register(OnChat, Priority.High);
            
            // - Create the directory for storing different configuration files. -
            string currentDirectory = AppContext.BaseDirectory;
            string parentDirectory = Directory.GetParent(currentDirectory).FullName;
            string fancychatDirectory = Path.Combine(parentDirectory, "fancychat");
            if (!Directory.Exists(fancychatDirectory)) {
                Directory.CreateDirectory(fancychatDirectory);
                Console.WriteLine("Fancychat config directory created.");
            }
            
            // - Create a tones.fcfile to store tone indicators. -
            string tonesFilePath = Path.Combine(fancychatDirectory, "tones.fcfile");
            if (!File.Exists(tonesFilePath)) {
                string defaultJsonContent = "/j | joking";
                File.WriteAllText(tonesFilePath, defaultJsonContent);
                Console.WriteLine("FancyChat tone.fcfile file created.");
            }
        }

        public override void Unload(bool shutdown) {
            OnChatEvent.Unregister(OnChat);
        }
        
        public static string CleanChatMessage(string originalString) {
            string targetString = ": ";
            int startIndex = originalString.IndexOf(targetString) + targetString.Length;
            string result = originalString.Substring(startIndex);
            return result;
        }
        
        public static string GetChatMessageBadge(LevelPermission rank) {
            if (rank == LevelPermission.Owner) { return "&c∙"; }
            else if (rank == LevelPermission.Guest) { return "&7∙"; }
            else if (rank == LevelPermission.Builder) { return "&2∙"; }
            else if (rank == LevelPermission.AdvBuilder) { return "&9∙"; }
            else if (rank == LevelPermission.Operator) { return "&b∙"; }
            else if (rank == LevelPermission.Admin) { return "&e∙"; }
            
            return "&0|";
        }
        
        public static string HandleTone(string tone, Dictionary<string, string> toneDict) {
            foreach (KeyValuePair<string, string> kvp in toneDict) {
                if (tone == kvp.Key) {
                    return " &8[&2" + kvp.Value.ToUpper() + "&8]";
                }
            }
            return "";
        }
        
        public static string GetToneIndicator(string input, Dictionary<string, string> toneDict, Player p) {
            foreach (KeyValuePair<string, string> kvp in toneDict) {
                if (input.Contains(kvp.Key)) {
                    return Convert.ToString(kvp.Key);
                }
            }
            return "";
        }
        
        public static string GetFancyChatConfigFile(string configname) {
            string currentDirectory = Directory.GetCurrentDirectory();
            string fancychatDirectory = Path.Combine(currentDirectory, "fancychat");
            string tonesFilePath = Path.Combine(fancychatDirectory, (configname + ".fcfile"));
            string filePath = Path.GetFullPath(tonesFilePath);
            return filePath;
        }
        
        public static Dictionary<string, string> ReadTonesJson() {
            string filePath = GetFancyChatConfigFile("tones");

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Tones.fcfile file not found.");
            }

            Dictionary<string, string> tonesDictionary = new Dictionary<string, string>();
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(new string[] { " | " }, StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();
                        tonesDictionary[key] = value;
                    }
                }
            }
    
            return tonesDictionary;
        }
        
        static void AppendLineToFile(string filePath, string text) {
            // Append the text to the file with a newline
            File.AppendAllText(filePath, Environment.NewLine + text);
        }
        
        static void RemoveLineFromFile(string filePath, int lineNumber) {
            try
            {
                // Read all lines from the file
                List<string> lines = new List<string>(File.ReadAllLines(filePath));
    
                // Check if the line number is within the range of the list
                if (lineNumber >= 0 && lineNumber < lines.Count)
                {
                    // Remove the line at the specified index
                    lines.RemoveAt(lineNumber);
    
                    // Write the modified list back to the file
                    // Use WriteAllLines which will handle the new line characters correctly
                    File.WriteAllLines(filePath, lines);
                }
                else
                {
                    Console.WriteLine("The specified line number is out of range.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred");
            }
        }

        public static void OnChat(ChatScope scope, Player p, ref string msg, object arg, ref ChatMessageFilter filter, bool relay) {
            if (!msg.Contains(".fc")) {
                string sentMessage = msg;
                string displayName = p.DisplayName;
                string mapName = p.Level.MapName.ToUpper();
                string cleanedMessage = CleanChatMessage(sentMessage);
                Dictionary<string, string> toneIndicators = ReadTonesJson();
                string currentTone = GetToneIndicator(sentMessage, toneIndicators, p);
                msg = "&8[&a" + mapName + "&8]" + HandleTone(currentTone, toneIndicators) + " " + GetChatMessageBadge(p.Rank) + " &3" + displayName + "&7: " + cleanedMessage;
            } else {
                string cleanedMessage = CleanChatMessage(msg).Replace("&f", "");
                
                msg = "";
                
                string[] wordsArray = cleanedMessage.Split(' ');
                List<string> arguments = new List<string>(wordsArray);
                if (arguments.Count == 1) {
                    p.Message("&aYou can use these commands to configure FancyChat:");
                    p.Message("&2Tone Indicators:");
                    p.Message("&7If you want to modify your tone indicators more thoroughly,");
                    p.Message("&7you can go into the config file which is located in");
                    p.Message("&7YourServerDirectory/fancychat/tones.fcfile");
                    p.Message("&2• &7.fc addtone <tone indicator> <tone prefix>");
                    p.Message("&2• &7.fc listtones");
                } else {
                    if (arguments[1] == "addtone") {
                        if (arguments.Count < 4 | arguments.Count > 4) {
                            p.Message("&7.fc addtone <tone indicator> <tone prefix>");
                        } else {
                            string filePath = GetFancyChatConfigFile("tones");
                            string newText = (arguments[2] + " | " + arguments[3]);
                            AppendLineToFile(filePath, newText);
                            p.Message("&aCreated the tone prefix &2" + arguments[3] + "&a from the indicator &2" + arguments[2]);
                        }
                    } else if (arguments[1] == "listtones") {
                        if (arguments.Count > 2) {
                            p.Message("&7.fc listtones");
                        } else {
                            Dictionary<string, string> toneIndicators = ReadTonesJson();
                            p.Message("&aHere are all the tone indicators:");
                            int index = -1;
                            foreach (KeyValuePair<string, string> kvp in toneIndicators) {
                                index++;
                                p.Message("&7" + index + "&2• &7" + kvp.Key + " &8for &7" + kvp.Value);
                            }
                        }
                    }
                }
            }
        }
    }
}
