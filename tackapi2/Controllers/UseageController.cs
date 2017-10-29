using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Collections;

namespace tackapi2.Controllers
{
   

    [Produces("application/json")]
    [Route("api/useage")]
    public class UseageController : Controller
    {

        private IMemoryCache _cache;

        public UseageController(IMemoryCache memoryCache)
        {
            _cache = memoryCache;
        }

        [HttpGet]
        [Route("")]
        public CurrentUsage Useage()
        {
            Queue readings;
            if (!_cache.TryGetValue("readingQueue", out readings))
            {
                return new CurrentUsage() { Speed = 0, Usage = 0 };
            }

            var arraOfReadings = readings.ToArray();
            int total = 0;
            foreach (var item in arraOfReadings)
            {
                if (item is Reading)
                {
                    Reading reading = (Reading)item;
                   total = total + reading.Value;
                }
            }


            int average = total / readings.Count;
            Reading currentReading = (Reading)readings.Peek();

            int difference = currentReading.Value - average;

            difference = difference * 25;
            if (difference > 100) difference = 100;
            if (difference < -100) difference = -100;


            return new CurrentUsage() {
                Speed = difference,
                Usage = currentReading.Value,
                Samples = readings.Count
            };
        }

        [HttpPost]
        [Route("")]
        public IActionResult TakeReading([FromBody] Reading reading)
        {
            if (reading == null)
            {
                return BadRequest();
            }

            Queue readings;
            if (!_cache.TryGetValue("readingQueue", out readings))
            {
                readings = new Queue();
            }
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                 .SetSlidingExpiration(TimeSpan.FromDays(3));

            if (readings.Count > 2) readings.Dequeue();
            readings.Enqueue(reading);
            _cache.Set("readingQueue", readings, cacheEntryOptions);


            return Ok();
        }

    }


    public class Reading
    {
        public int Time { get; set; }
        public int Value { get; set; }
}

    public class CurrentUsage
    {
        public int Speed { get; set; }
        public int Usage { get; set; }
        public int Samples { get; set; }
        public CurrentUsage()
        {
        }
    }
}