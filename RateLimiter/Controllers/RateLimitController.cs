using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Timers;

namespace RateLimiter.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RateLimitController : ControllerBase
    {
       
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _accessor;
        private string IpAddress;
        private string endpoint;
        private int request;
        private int time;
        private static int BANNED_REQUESTS=5;
        private static int REDUCTION_INTERVAL=45; //
        private const int RELEASE_INTERVAL = 5 * 60 * 1000; // 5 minutes

        private static Dictionary<string, short> _IpAdresses = new Dictionary<string, short>();
        private static Stack<string> _Banned = new Stack<string>();
        //private static Timer _Timer = CreateTimer();
        private static Timer _BannedTimer = CreateBanningTimer();

        public RateLimitController(IConfiguration config, IHttpContextAccessor accessor)
        {
            _config = config;
            _accessor = accessor;
        }

        [HttpGet]
        public IActionResult Get()
        {
            BANNED_REQUESTS = _config.GetValue<int>("RateLimiting:Limit");
            REDUCTION_INTERVAL = _config.GetValue<int>("RateLimiting:Period");


            endpoint = _config.GetValue<string>("RateLimiting:Endpoint");
            time = _config.GetValue<int>("RateLimiting:Period");
            request = _config.GetValue<int>("RateLimiting:Limit");

            var limiter = new RateLimiter(request, TimeSpan.FromSeconds(time));
            var remoteIpAddress = _accessor.HttpContext.Connection.RemoteIpAddress.ToString();

            if (!context_BeginRequest(remoteIpAddress))
                { return StatusCode(403); };
           
            //var t = Task.Run(() =>
            //{
            //    for (var i = 0; i < 100; i++)
            //    {
            //        while (!limiter.Process("foo"))
            //        {
            //            Task.Delay(1).Wait();
            //        }

            //        Console.WriteLine(i);
            //    }
            //});

            //Task.WaitAll(t);
            //Console.ReadKey();
            return StatusCode(200);

        }

        [HttpPost]
        [Route("ratelimit/edit")]
        public IActionResult Update()
        {
            return StatusCode(200);
        }

        [HttpPost]
        [Route("ratelimit/add")]
        public IActionResult Create()
        {
            return StatusCode(200);
        }

        private bool context_BeginRequest(string ip)
        {
            if (_Banned.Contains(ip))
            {
                return false;

            }
            CheckIpAddress(ip);
            return true;
        }

        /// <summary>
        /// Checks the requesting IP address in the collection
        /// and bannes the IP if required.
        /// </summary>
        private static void CheckIpAddress(string ip)
        {
            if (!_IpAdresses.ContainsKey(ip))
            {
                _IpAdresses[ip] = 1;
            }
            else if (_IpAdresses[ip] == BANNED_REQUESTS)
            {
                _Banned.Push(ip);
                _IpAdresses.Remove(ip);
            }
            else
            {
                _IpAdresses[ip]++;
            }
        }

        #region Timers

        /// <summary>
        /// Creates the timer that substract a request
        /// from the _IpAddress dictionary.
        /// </summary>
        //private static Timer CreateTimer()
        //{
        //    Timer timer = GetTimer(REDUCTION_INTERVAL);
        //    timer.Elapsed += new ElapsedEventHandler(TimerElapsed);
        //    return timer;
        //}

        /// <summary>
        /// Creates the timer that removes 1 banned IP address
        /// everytime the timer is elapsed.
        /// </summary>
        /// <returns></returns>
        private static Timer CreateBanningTimer()
        {
            Timer timer = GetTimer(RELEASE_INTERVAL);
            timer.Elapsed += delegate { _Banned.Pop(); };
            return timer;
        }

        /// <summary>
        /// Creates a simple timer instance and starts it.
        /// </summary>
        /// <param name="interval">The interval in milliseconds.</param>
        private static Timer GetTimer(int interval)
        {
            Timer timer = new Timer();
            timer.Interval = interval;
            timer.Start();

            return timer;
        }

        /// <summary>
        /// Substracts a request from each IP address in the collection.
        /// </summary>
        private static void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            foreach (string key in _IpAdresses.Keys)
            {
                _IpAdresses[key]--;
                if (_IpAdresses[key] == 0)
                    _IpAdresses.Remove(key);
            }
        }

        #endregion



    }
}
