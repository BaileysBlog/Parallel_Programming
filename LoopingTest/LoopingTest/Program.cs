using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LoopingTest
{
    class Program
    {
        private static Lazy<ZipCode[]> _Codes;
        public static ZipCode[] Codes
        {
            get
            {
                return _Codes.Value;
            }
        }

        static void Main(string[] args)
        {

            // Loop over the collection of data and write 2 queries that gather information about it
            _Codes = new Lazy<ZipCode[]>(()=> GetZipCodes());

            var stopwatch = new System.Diagnostics.Stopwatch();

            stopwatch.Start();

            var areaInfo = Codes
                .Where(x => "17702".CompareTo(x.Code.ToString()) == 0)
                .First()
                .ZipDetails;

            stopwatch.Stop();

            Console.WriteLine($"Time Processing: {stopwatch.ElapsedMilliseconds}ms");


            stopwatch.Restart();

            var areaInfoPar = Codes.AsParallel()
                .Where(x => "17702".CompareTo(x.Code.ToString()) == 0)
                .First()
                .ZipDetails;

            stopwatch.Stop();

            Console.WriteLine($"Time Processing: {stopwatch.ElapsedMilliseconds}ms");

            Console.WriteLine($"Data Sets are equal: {areaInfo.SequenceEqual(areaInfoPar)}");





            stopwatch.Restart();
            var _sum = areaInfo.Select(x => x.EstimatedPopulation).Sum();
            Console.WriteLine($"Estimated Total Population for {areaInfo.First().City} is {_sum}");
            stopwatch.Stop();
            Console.WriteLine($"Time Processing: {stopwatch.ElapsedMilliseconds}ms");
            
            stopwatch.Restart();

            long sum = 0;
            long startingValue = 0;
            Parallel.ForEach(areaInfoPar, () => startingValue,
             (curDes, loopState, localSum) =>
             {
                 localSum += curDes.EstimatedPopulation;
                 return localSum;
             }, (localSum) => Interlocked.Add(ref sum, localSum));

            stopwatch.Stop();

            Console.WriteLine($"Time Processing: {stopwatch.ElapsedMilliseconds}ms");

            Console.WriteLine($"Estimated Total Population for {areaInfoPar.First().City} is {sum}");
            
            Console.WriteLine("Press any key to exit..");
            Console.ReadKey();

        }
        

        private static ZipCode[] GetZipCodes()
        {
            var codes = File.ReadAllLines(Path.Combine(Environment.CurrentDirectory, "free-zipcode-database.csv"))
                                .Skip(1)
                                .Select(x=> x.Replace("\"", ""));

            var query = from line in codes
                        let fields = line.Split(',')
                        select new ZipCode(fields[1]);

            return query.ToArray();
        }
        
        public class ZipCode
        {
            //1,3,4
            private Lazy<ZipDetail[]> _ZipDetails;

            public readonly string Code;
            
            public ZipCode(string Code)
            {
                this.Code = Code;

                _ZipDetails = new Lazy<ZipDetail[]>(()=> GetZipDetailsForZip(this.Code));
            }
            
            public ZipDetail[] ZipDetails
            {
                get
                {
                    return _ZipDetails.Value;
                }
            }


            private ZipDetail[] GetZipDetailsForZip(string Code)
            {
                var details = File.ReadAllLines(Path.Combine(Environment.CurrentDirectory, "free-zipcode-database.csv"))
                                .Skip(1)
                                .Select(x => x.Replace("\"", ""));


                var detailQuery = from line in details
                                  let fields = line.Split(',')
                                  where fields[1].CompareTo(Code) == 0
                                  select new ZipDetail(int.Parse(fields[1]), fields[3], fields[4], fields[17]);


                return detailQuery.ToArray();
            }
        }
        
        public class ZipDetail
        {
            public readonly int ZipCode;
            public readonly String City;
            public readonly String State;
            public readonly int EstimatedPopulation;

            public ZipDetail(int ZipCode, String City, String State,string EstimatedPopulation)
            {
                this.ZipCode = ZipCode;
                this.City = City;
                this.State = State;
                var didParse = int.TryParse(EstimatedPopulation, out int value);
                this.EstimatedPopulation = value;
            }
        }
    }
}
