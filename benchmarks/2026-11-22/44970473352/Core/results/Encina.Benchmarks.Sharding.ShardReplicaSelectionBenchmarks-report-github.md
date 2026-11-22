```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-----------:|----------:|----------:|-----------:|------:|--------:|-----:|----------:|------------:|
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     |   5.582 ns | 0.0312 ns | 0.0260 ns |   5.574 ns |  1.00 |    0.01 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     |  13.532 ns | 0.1520 ns | 0.1422 ns |  13.457 ns |  2.42 |    0.03 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 106.240 ns | 0.6435 ns | 0.6019 ns | 105.873 ns | 19.03 |    0.13 |    5 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     |  98.181 ns | 0.1016 ns | 0.0848 ns |  98.154 ns | 17.59 |    0.08 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     |  65.145 ns | 0.3125 ns | 0.2610 ns |  65.090 ns | 11.67 |    0.07 |    3 |         - |          NA |
|                                |            |                |             |             |            |           |           |            |       |         |      |           |             |
| RoundRobin.SelectReplica       | MediumRun  | 15             | 2           | 10          |   6.016 ns | 0.0525 ns | 0.0769 ns |   5.967 ns |  1.00 |    0.02 |    1 |         - |          NA |
| Random.SelectReplica           | MediumRun  | 15             | 2           | 10          |  13.977 ns | 0.0145 ns | 0.0193 ns |  13.978 ns |  2.32 |    0.03 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | MediumRun  | 15             | 2           | 10          | 104.472 ns | 0.7185 ns | 1.0073 ns | 104.541 ns | 17.37 |    0.27 |    5 |         - |          NA |
| LeastConnections.SelectReplica | MediumRun  | 15             | 2           | 10          |  99.635 ns | 0.5794 ns | 0.8309 ns |  99.883 ns | 16.56 |    0.25 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | MediumRun  | 15             | 2           | 10          |  67.534 ns | 0.1723 ns | 0.2300 ns |  67.476 ns | 11.23 |    0.14 |    3 |         - |          NA |
