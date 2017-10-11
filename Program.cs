using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToTwitter;
using System.IO;
using System.Data.OleDb;
using System.Data;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;


namespace TwitterAPIIntegration
{
    static class Program
    {
	  
       /*
	* We can create multiple tables in a database, let's combine these two to have the same tables, 
	* so we don't have to create two connections
	*/
        public static string MySQLConnectionString = "SERVER=localhost;DATABASE=srs;UID=root;PASSWORD=password;";
        public static string MySQLConnectionString2 = "SERVER=localhost;DATABASE=companydata;UID=root;PASSWORD=password;";

	/*
	 * Example: 
	 * I want to have a database that stores tweets, my prediction, and various images of cats.
	 * Let's first create this database with the following:
	 * CREATE DATABASE databaseName;
	 * USE databaseName
	 *
	 * The USE statement, allows us to execute all of our following queries using the same database. Eliminating the need for 2 databases.
	 *
	 * Let's say that I want to keep tweets as a table, that has tweets, it's length, and the link to the tweet. We
	 * call the following a Schema for the table:
	 * 
	 * Tweets(varchar(255), int, mediumText)
	 *
	 * We could create the table like this:
	 * 
	 * CREATE TABLE IF NOT EXISTS Tweets(tweet VARCHAR(255), length INT, link MEDIUMTEXT)
	 *
	 * We can create more tables in a similar way. All will be held in the same database
	 * Do you need tables to be able to "communicate" with each other? It's something we can do!
	 */

        
        private static List<string>[] LoadData(string TableName, string column2, string column3, int conn)
        {
	
		// Yikes! A list of lists? What are we going to be using this for?
            List<string>[] list = new List<string>[3];
            list[0] = new List<string>();
            list[1] = new List<string>();
            list[2] = new List<string>();

            MySqlConnection connection = new MySqlConnection();

	    /*
	     * Oh no! Let's combine our databases! Two connections = :( = Slow
	     */

            if (conn == 1)
            {
                connection = new MySqlConnection(MySQLConnectionString);
            }
            else if(conn == 2)
            {
                connection = new MySqlConnection(MySQLConnectionString2);
            }
            connection.Open();
	    /*
	     * How bout that USE statement ;)
	     */

            string query = "SELECT * FROM srs." + TableName;
            MySqlCommand cmd = new MySqlCommand(query, connection);
            MySqlDataReader dataReader = cmd.ExecuteReader();

	    // I imagine if we're reading in a lot of data this will slow us down.

            while(dataReader.Read())
            {
                list[0].Add(dataReader["id"] + "");
                list[1].Add(dataReader[column2] + "");
                if (column3 != "")
                {
                    list[2].Add(dataReader[column3] + "");
                }
            }

	    // Good! Always gotta do this!
            dataReader.Close();
            connection.Close();

            return list;
        }

        private static void AddData(string TableName, string column, string contents, string column2, string contents2, int conn)
        {
		// How often are we refrencing the database? It might not be wise to keep opening the connection if we're accesing it constantly
            MySqlConnection connection = new MySqlConnection();
            string query;
            string database = "";

	    // That USE statement will get rid of these if/elseif statements/
	    // We just have to make a distinction of what table we want to use

            if (conn == 1)
            {
                connection = new MySqlConnection(MySQLConnectionString);
                database = "srs";
            }
            else if (conn == 2)
            {
                connection = new MySqlConnection(MySQLConnectionString2);
                database = "companydata";
            }
            connection.Open();

            if (column2 != "")
            {
                query = "INSERT INTO " + database + "." + TableName + " (" + column + "," + column2 + ") " + "VALUES('" + contents + "','" + contents2 + "')";
            }
            else
            {
                query = "INSERT INTO " + database + "." + TableName + " (" + column + ") " + "VALUES('" + contents + "')";
            }
            MySqlCommand cmd = new MySqlCommand(query, connection);
            cmd.ExecuteNonQuery();

            connection.Close();
        }

        private static void UpdateData(string TableName, string column, string contents, int ID, int conn)
        {
            MySqlConnection connection = new MySqlConnection();
            string database = "";
		// See above comments
            if (conn == 1)
            {
                connection = new MySqlConnection(MySQLConnectionString);
                database = "srs";
            }
            else if (conn == 2)
            {
                connection = new MySqlConnection(MySQLConnectionString2);
                database = "companydata";
            }
            connection.Open();

            string query = "UPDATE " + database + "." + TableName + " SET " + column + "= '" + contents + "' WHERE ID = " + ID;
            MySqlCommand cmd = new MySqlCommand(query, connection);
            cmd.ExecuteNonQuery();

            connection.Close();
        }

        private static void CreateTable(string TableName, string column1, string type1, string column2, string type2, string column3, string type3, int conn)
        {
            MySqlConnection connection = new MySqlConnection();
	    // We might want to do this locally, instead of inside the program each time.... Also 
	    // Since we're not saying "IF NOT EXISTS" you're probably making a lot of tables
	    // Which is probably slowing us down.
	    // So let's set this up the way we want it locally, and then not mess with it in the program/
            
            if (conn == 1)
            {
                connection = new MySqlConnection(MySQLConnectionString);
            }
            else if (conn == 2)
            {
                connection = new MySqlConnection(MySQLConnectionString2);
            }
            connection.Open();

            string query = "CREATE TABLE srs." + TableName + " (" + column1 + " " + type1 + " NOT NULL AUTO_INCREMENT, " + column2 + " " + type2 + " NOT NULL, " + column3 + " " + type3 + " NOT NULL, PRIMARY KEY(" + column1 + "))";
            MySqlCommand cmd = new MySqlCommand(query, connection);
            cmd.ExecuteNonQuery();

            connection.Close();
        }

        
#if true
        
        private static void Main(string[] args)
        {
            Console.WriteLine("I think I can, I think I can, I think I can...");
            var tweetList = GetTwitterFeeds();
            string CompanyVariable;
            string CompanyVariable2;
            string CompanyVariable3;
            bool FileVarSet = false;
            bool FileVar2Set = false;
            bool FileVar3Set = false;
            int ID = 1;

            Console.WriteLine("Tweets Count " + tweetList.Count); 
            
            
            

            foreach (var item in tweetList)
            {
                
                item.Text = item.Text.Replace("'", "");
                AddData("previous_tweets", "tweet", item.Text, "", "", 1);
                Console.WriteLine("tweet added to previous tweets");

                item.Text = ReplaceChars(item.Text);

               for (int i = 0; i < LoadData("all_companies", "symbol", "name", 1)[1].Count; i++)
                {
                    // This is hard to read, perhaps you could break it up into variables and then IF it. 
		    // Boolean logic hard to follow
                    if ((item.Text.Contains(((LoadData("all_companies", "symbol", "name", 1)[1])[i] + " "), StringComparison.OrdinalIgnoreCase) || item.Text.Contains(((LoadData("all_companies", "symbol", "name", 1)[2])[i] + " "), StringComparison.OrdinalIgnoreCase)) && !FileVarSet)
                    {
                        
                        CompanyVariable = (LoadData("all_companies", "symbol", "name", 1)[1])[i];

                        FileVarSet = true;
                        
                        

                        /*item.Text = Regex.Replace(item.Text, @"@\w+", "USERNAME ");/*{{{*/
                        item.Text = Regex.Replace(item.Text, @"https?:
                        item.Text = item.Text.Replace("https:

                        

                        
                        

                        

                        AddData("all_word_arrays", "Sentiment", "+1", "company", CompanyVariable, 1);
                        
                        

                        
                        for (int ii = 1; ii <= LoadData("all_tweet_words", "word", "", 1)[1].Count; ii++)
                        {
                            
                            if (item.Text.Contains(((LoadData("all_tweet_words", "word", "", 1)[1])[ii - 1] + " "), StringComparison.OrdinalIgnoreCase))
                            {
                                
                                UpdateData("all_word_arrays", "word" + ii, ii + ":1 ", ID, 1);               
                            }
                            else
                            {
                                
                                UpdateData("all_word_arrays", "Word" + ii, ii + ":0 ", ID, 1);
                            }
                        }

                        ID++;
                        Console.WriteLine("done with tweet company table 1");
                        
                        
                    }
                    
                    else if ((item.Text.Contains(((LoadData("all_companies", "symbol", "name", 1)[1])[i] + " "), StringComparison.OrdinalIgnoreCase) || item.Text.Contains(((LoadData("all_companies", "symbol", "name", 1)[2])[i] + " "), StringComparison.OrdinalIgnoreCase)) && !FileVar2Set)
                    {
                        
                        CompanyVariable2 = (LoadData("all_companies", "symbol", "name", 1)[1])[i];
                        FileVar2Set = true;
                        
                        

                        /*item.Text = Regex.Replace(item.Text, @"@\w+", "USERNAME");
                        item.Text = Regex.Replace(item.Text, @"https?:
                        item.Text = item.Text.Replace("https:

                        

                        
                        

                        

                        AddData("all_word_arrays", "Sentiment", "+1", "company", CompanyVariable2, 1);
                        
                        for (int ii = 1; ii <= LoadData("all_tweet_words", "word", "", 1)[1].Count; ii++)
                        {
                            
                            if (item.Text.Contains(((LoadData("all_tweet_words", "word", "", 1)[1])[ii - 1] + " "), StringComparison.OrdinalIgnoreCase))
                            {
                                
                                UpdateData("all_word_arrays", "Word" + ii, ii + ":1 ", ID, 1);
                            }
                            else
                            {
                                
                                UpdateData("all_word_arrays", "Word" + ii, ii + ":0 ", ID, 1);
                            }
                        }

                        ID++;
                        Console.WriteLine("done with tweet company table 2");
                        
                        
                    }
                    
                    else if ((item.Text.Contains(((LoadData("all_companies", "symbol", "name", 1)[1])[i] + " "), StringComparison.OrdinalIgnoreCase) || item.Text.Contains(((LoadData("all_companies", "symbol", "name", 1)[2])[i] + " "), StringComparison.OrdinalIgnoreCase)) && !FileVar3Set)
                    {
                        
                        CompanyVariable3 = (LoadData("all_companies", "symbol", "name", 1)[1])[i];
                        FileVar3Set = true;
                        
                        

                        /*item.Text = Regex.Replace(item.Text, @"@\w+", "USERNAME");
                        item.Text = Regex.Replace(item.Text, @"https?:
                        item.Text = item.Text.Replace("https:

                        

                        
                        

                        

                        AddData("all_word_arrays", "Sentiment", "+1", "company", CompanyVariable3, 1);
                        
                        for (int ii = 1; ii <= LoadData("all_tweet_words", "word", "", 1)[1].Count; ii++)
                        {
                            
                            if (item.Text.Contains(((LoadData("all_tweet_words", "word", "", 1)[1])[ii - 1] + " "), StringComparison.OrdinalIgnoreCase))
                            {
                                
                                UpdateData("all_word_arrays", "Word" + ii, ii + ":1 ", ID, 1);
                            }
                            else
                            {
                                
                                UpdateData("all_word_arrays", "Word" + ii, ii + ":0 ", ID, 1);
                            }
                        }

                        ID++;
                        Console.WriteLine("done with company table 3");
                        
                        
                    }
                }
                FileVarSet = false;
                FileVar2Set = false;
                FileVar3Set = false;

                
                AddData("edited_tweets", "tweet", item.Text, "", "", 1);
            }
            

            
            Console.WriteLine("I made it!!! Phew.");
            Console.ReadLine();
        }

        
        public static List<Status> GetTwitterFeeds()
        {
            
            string screenname = "WSJ";

            var auth = new SingleUserAuthorizer
            {
                CredentialStore = new InMemoryCredentialStore()
                {
                    
                    ConsumerKey = "qGuAmepeXNmKBX94XdFkHa8Pz",
                    ConsumerSecret = "wNcdbn3jYSITazILqOG33Lj1RY2k6kUlKaG2cjKnEuWZ4YWIZP",
                    OAuthToken = "901596402383544320-V4szsa3jD5vfW7L3HtZpcZ7jVMl67ek",
                    OAuthTokenSecret = "SYhKAQKm8jC9YTaP5as4KXZJIHgzoxSTPiXGJPehAyCc1"
                }
            };
            var twitterCtx = new TwitterContext(auth);

            var Tweets = new List<Status>();

            ulong maxId = 0;
            bool flag = true;
            var statusResponse = new List<Status>();
            
            
            
            statusResponse = (from tweet in twitterCtx.Status
                              where tweet.Type == StatusType.Home
                                    
                                    && tweet.Count == 200
                                    && (DateTime)tweet.CreatedAt >= DateTime.Today
                                    && LoadData("all_companies", "symbol", "name", 1)[1].Any(w => tweet.Text.Contains(w)) || LoadData("all_companies", "symbol", "name", 1)[2].Any(w => tweet.Text.Contains(w))
                                    
                              
                                    
                                    
                              
                                    
                                    
                                    
                                    
                                    && !LoadData("previous_tweets", "tweet", "", 1)[1].Any(w => tweet.Text.Contains(w))
                              select tweet).ToList();

            if (statusResponse.Count > 0)
            {
                maxId = ulong.Parse(statusResponse.Last().StatusID.ToString()) - 1;
                Tweets.AddRange(statusResponse);
            }

            while (flag)
            {
                int rateLimitStatus = twitterCtx.RateLimitRemaining;

                if (rateLimitStatus != 0)
                {
                    statusResponse = (from tweet in twitterCtx.Status
                                      where tweet.Type == StatusType.Home
                                            
                                            && tweet.MaxID == maxId
                                            && tweet.Count == 200
                                            && (DateTime)tweet.CreatedAt >= DateTime.Today
                                            && LoadData("all_companies", "symbol", "name", 1)[1].Any(w => tweet.Text.Contains(w)) || LoadData("all_companies", "symbol", "name", 1)[2].Any(w => tweet.Text.Contains(w))
                                            
                                            
                                            
                                            
                                            
                                            && !LoadData("previous_tweets", "tweet", "", 1)[1].Any(w => tweet.Text.Contains(w))
                                      select tweet).ToList();


                    if (statusResponse.Count != 0)
                    {
                        maxId = ulong.Parse(statusResponse.Last().StatusID.ToString()) - 1;
                        Tweets.AddRange(statusResponse);
                    }
                    else
                    {
                        flag = false;
                    }
                }
                else
                {
                    flag = false;
                }
            }
            return Tweets;
        }

        public static bool Contains(this string target, string value, StringComparison comparison)
        {
            return target.IndexOf(value, comparison) >= 0;
        }

        public static string ReplaceChars(string input)
        {
            string output = Regex.Replace(input, @"@\w+", "USERNAME ");
            output = Regex.Replace(output, @"https?:
            output = output.Replace("https:
            output = output.Replace(":", "");
            output = output.Replace("#", "");
            output = output.Replace("…", "");
            output = output.Replace("’s", "");
            output = output.Replace("’", "");
            output = output.Replace("‘", "");
            output = output.Replace("'s", "");
            output = output.Replace("'", "");
            output = output.Replace("RT ", "");
            output = output.Replace(",", "");
            output = output.Replace("?", "");
            output = output.Replace("!", "");
            output = output.Replace(".", "");
            output = output.Replace(";", "");
            output = Regex.Replace(output, "\\s+", " ");

            return output;
        }
#endif

    }
}/*}}}*/
