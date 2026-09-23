```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Median    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     |  4.749 ns |  0.1451 ns | 0.1425 ns |  4.630 ns |  1.00 |    0.04 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     | 11.375 ns |  0.1603 ns | 0.1500 ns | 11.357 ns |  2.40 |    0.08 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 82.646 ns |  0.1710 ns | 0.1516 ns | 82.590 ns | 17.42 |    0.50 |    4 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     | 83.069 ns |  0.6062 ns | 0.5374 ns | 82.853 ns | 17.51 |    0.52 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     | 56.212 ns |  0.3504 ns | 0.3278 ns | 56.249 ns | 11.85 |    0.35 |    3 |         - |          NA |
|                                |            |                |             |             |           |            |           |           |       |         |      |           |             |
| RoundRobin.SelectReplica       | ShortRun   | 3              | 1           | 3           |  4.270 ns |  2.8680 ns | 0.1572 ns |  4.351 ns |  1.00 |    0.05 |    1 |         - |          NA |
| Random.SelectReplica           | ShortRun   | 3              | 1           | 3           | 10.778 ns |  2.7685 ns | 0.1517 ns | 10.698 ns |  2.53 |    0.09 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | ShortRun   | 3              | 1           | 3           | 83.585 ns | 26.7537 ns | 1.4665 ns | 82.780 ns | 19.59 |    0.70 |    4 |         - |          NA |
| LeastConnections.SelectReplica | ShortRun   | 3              | 1           | 3           | 81.026 ns |  7.9060 ns | 0.4334 ns | 80.847 ns | 18.99 |    0.62 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | ShortRun   | 3              | 1           | 3           | 51.232 ns |  6.6411 ns | 0.3640 ns | 51.033 ns | 12.01 |    0.40 |    3 |         - |          NA |
