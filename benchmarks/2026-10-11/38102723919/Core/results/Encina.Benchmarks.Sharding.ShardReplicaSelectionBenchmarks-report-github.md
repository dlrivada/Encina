```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-----------:|----------:|----------:|-----------:|------:|--------:|-----:|----------:|------------:|
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     |   6.176 ns | 0.1951 ns | 0.1825 ns |   6.307 ns |  1.00 |    0.04 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     |  14.819 ns | 0.0148 ns | 0.0139 ns |  14.817 ns |  2.40 |    0.07 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 107.426 ns | 0.0507 ns | 0.0396 ns | 107.435 ns | 17.41 |    0.50 |    5 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     | 104.940 ns | 0.0711 ns | 0.0631 ns | 104.939 ns | 17.01 |    0.49 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     |  74.243 ns | 1.5271 ns | 1.5682 ns |  75.051 ns | 12.03 |    0.43 |    3 |         - |          NA |
|                                |            |                |             |             |            |           |           |            |       |         |      |           |             |
| RoundRobin.SelectReplica       | MediumRun  | 15             | 2           | 10          |   6.764 ns | 0.1472 ns | 0.2158 ns |   6.711 ns |  1.00 |    0.04 |    1 |         - |          NA |
| Random.SelectReplica           | MediumRun  | 15             | 2           | 10          |  15.259 ns | 0.1301 ns | 0.1737 ns |  15.127 ns |  2.26 |    0.07 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | MediumRun  | 15             | 2           | 10          | 110.054 ns | 2.7600 ns | 3.9584 ns | 108.277 ns | 16.29 |    0.77 |    5 |         - |          NA |
| LeastConnections.SelectReplica | MediumRun  | 15             | 2           | 10          | 104.700 ns | 1.3551 ns | 1.9863 ns | 103.968 ns | 15.49 |    0.56 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | MediumRun  | 15             | 2           | 10          |  73.874 ns | 1.6733 ns | 2.3998 ns |  72.942 ns | 10.93 |    0.49 |    3 |         - |          NA |
