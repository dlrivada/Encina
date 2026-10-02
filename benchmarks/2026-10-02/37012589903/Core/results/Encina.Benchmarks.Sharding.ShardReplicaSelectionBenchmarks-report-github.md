```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     |  5.696 ns |  0.1073 ns | 0.1102 ns |  1.00 |    0.03 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     | 11.148 ns |  0.0564 ns | 0.0500 ns |  1.96 |    0.04 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 77.316 ns |  0.2561 ns | 0.2395 ns | 13.58 |    0.25 |    4 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     | 75.710 ns |  0.1513 ns | 0.1264 ns | 13.30 |    0.24 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     | 54.056 ns |  0.4318 ns | 0.3606 ns |  9.49 |    0.18 |    3 |         - |          NA |
|                                |            |                |             |             |           |            |           |       |         |      |           |             |
| RoundRobin.SelectReplica       | ShortRun   | 3              | 1           | 3           |  6.052 ns |  0.6460 ns | 0.0354 ns |  1.00 |    0.01 |    1 |         - |          NA |
| Random.SelectReplica           | ShortRun   | 3              | 1           | 3           | 11.635 ns |  4.1886 ns | 0.2296 ns |  1.92 |    0.03 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | ShortRun   | 3              | 1           | 3           | 80.150 ns | 50.6642 ns | 2.7771 ns | 13.24 |    0.40 |    4 |         - |          NA |
| LeastConnections.SelectReplica | ShortRun   | 3              | 1           | 3           | 79.316 ns |  2.7614 ns | 0.1514 ns | 13.11 |    0.07 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | ShortRun   | 3              | 1           | 3           | 55.427 ns |  3.4541 ns | 0.1893 ns |  9.16 |    0.05 |    3 |         - |          NA |
