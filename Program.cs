using System;
using System.Net;
using System.Timers;

namespace PassPortCracker
{
    class Program
    {
        private static System.Timers.Timer timer = new System.Timers.Timer();
        private static CookieContainer cookieContainer = new CookieContainer();
        private static HttpClientHandler Handler = new HttpClientHandler() { CookieContainer = cookieContainer };
        private static HttpClient client = new HttpClient(Handler);


        private static bool timeToCreateNewSession = false;
        private static  Uri BASEURI = new Uri("https://bokapass.nemoq.se");

        private static string BOOKINGNEXT = "https://bokapass.nemoq.se/Booking/Booking/Next/blekinge";//kalmar
        private static string BOOKINGINDEX = "https://bokapass.nemoq.se/Booking/Booking/Index/blekinge";//kalmar

        private static string CitySectionId = "105"; //102 is Kalmar City.
        private static string KommunId = "74"; //69 (Nice) is Kalmar.

        static async Task Main(string[] args)
        {
            await AcquireSession();
            Thread.Sleep(5000);
            await SelectBookNewTime();
            await acceptHandelingPersonalIOnformation();
            await selectSweden();

            var acceptableDateFound = false;
            
            SetTimerToRefreshSession();

            while (!acceptableDateFound)
            {
                Thread.Sleep(10000);
                Console.WriteLine("\nFetching First Available Time...");
                var bookingPageResult = await selectFirstAvailableTime();

                acceptableDateFound = checkIfDateIsAcceptable(bookingPageResult);
                Console.WriteLine("\nAvailable Time found: " + acceptableDateFound);

                if(timeToCreateNewSession)
                {
                    Console.WriteLine("Creating new session at " + DateTime.Now.ToLongTimeString());
                    await AcquireSession();
                    Thread.Sleep(5000);
                    await SelectBookNewTime();
                    await acceptHandelingPersonalIOnformation();
                    await selectSweden();
                    timeToCreateNewSession = false;
                }

            }
            
            playAlertSound();
        }

        private static void SetTimerToRefreshSession()
   {
        // Create a timer with a two second interval.
        timer = new System.Timers.Timer(300000);
        // Hook up the Elapsed event for the timer. 
        timer.Elapsed += SetFlag;
        timer.AutoReset = true;
        timer.Enabled = true;
    }

    private static void SetFlag(Object source, ElapsedEventArgs e)
    {
        timeToCreateNewSession = true;
    }

        static private void RemoveSessionCookies()
        {
            var cookies = cookieContainer.GetCookies(BASEURI);
            foreach (Cookie co in cookies)
            {
              co.Expires = DateTime.Now.Subtract(TimeSpan.FromDays(1));
            }
        }

        static private void playAlertSound()
        {
            while (true)
            {
                Console.Beep();
            }
        }

        static private bool checkIfDateIsAcceptable(string websiteHTML)
        {
            return websiteHTML.Contains("2022-03") ||
            websiteHTML.Contains("2022-04") ||
            websiteHTML.Contains("2022-05") ||
            websiteHTML.Contains("2022-06");

        }

        static private async Task<string> selectFirstAvailableTime()
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("FormId", "1"),
                new KeyValuePair<string, string>("NumberOfPeople", "1"),
                new KeyValuePair<string, string>("RegionId", "0"),
                new KeyValuePair<string, string>("SectionId", CitySectionId),
                new KeyValuePair<string, string>("NQServiceTypeId", "1"),
                new KeyValuePair<string, string>("FromDateString", DateTime.Now.ToShortDateString()),
                new KeyValuePair<string, string>("SearchTimeHour", "12"),
                new KeyValuePair<string, string>("Serv", "2"),
                new KeyValuePair<string, string>("TimeSearchFirstAvailableButton", "Första lediga tid"),
            });

            var responseMessage = await client.PostAsync(BOOKINGNEXT, content);

            var result = await responseMessage.Content.ReadAsStringAsync();

            Console.Write(DateTime.Now.ToShortTimeString());
            return result;
        }

        static private async Task selectSweden() 
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("ServiceCategoryCustomers[0].CustomerIndex", "0"),
                new KeyValuePair<string, string>("ServiceCategoryCustomers[0].ServiceCategoryId", "2"),
                new KeyValuePair<string, string>("Next", "Nästa"),
            });

            var responseMessage = await client.PostAsync(BOOKINGNEXT, content);

            var result = await responseMessage.Content.ReadAsStringAsync();

            Console.Write("ok");
        }

        static private async Task SelectBookNewTime()
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("FormId", "1"),
                new KeyValuePair<string, string>("ServiceGroupId", KommunId),
                new KeyValuePair<string, string>("StartNextButton", "Boka ny tid"),
            });

            var responseMessage = await client.PostAsync(BOOKINGNEXT, content);

            var result = await responseMessage.Content.ReadAsStringAsync();

            Console.Write("ok");
        }

        static private async Task acceptHandelingPersonalIOnformation() 
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("AgreementText", "För att kunna genomföra tidsbokning för ansökan om pass och/eller id-kort krävs att dina personuppgifter behandlas. Det är nödvändigt för att Polismyndigheten ska kunna utföra de uppgifter som följer av passförordningen (1979:664) och förordningen (2006:661) om nationellt identitetskort och som ett led i myndighetsutövning. För att åtgärda eventuellt uppkomna fel kan också systemleverantören komma att nås av personuppgifterna. Samtliga uppgifter raderas ur tidsbokningssystemet dagen efter besöket."),
                new KeyValuePair<string, string>("AcceptInformationStorage", "true"),
                new KeyValuePair<string, string>("AcceptInformationStorage", "false"),
                new KeyValuePair<string, string>("NumberOfPeople", "1"),
                new KeyValuePair<string, string>("Next", "Nästa"),
            });


            var responseMessage = await client.PostAsync(BOOKINGNEXT, content);

            var result = await responseMessage.Content.ReadAsStringAsync();

            Console.Write("Stop");
        }

        static private async Task AcquireSession()
        {
            var stringTask = await client.GetAsync(BOOKINGINDEX);

            var oo = stringTask.Headers.Where(k => k.Key == "Set-Cookie").ToList();

            foreach (var item in oo)
            {
                cookieContainer.Add(BASEURI, new Cookie(item.Key, item.Value.ToString()));
            }
            
            var o = await stringTask.Content.ReadAsStringAsync();
            //client


            Console.Write(stringTask.ToString());
        }

    }
}