```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.46GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     |  5.551 ns |  0.0335 ns | 0.0279 ns |  1.00 |    0.01 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     |  8.170 ns |  0.1351 ns | 0.1198 ns |  1.47 |    0.02 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 56.787 ns |  0.8113 ns | 0.7192 ns | 10.23 |    0.13 |    5 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     | 54.255 ns |  0.3790 ns | 0.3360 ns |  9.77 |    0.08 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     | 42.392 ns |  0.7595 ns | 0.6733 ns |  7.64 |    0.12 |    3 |         - |          NA |
|                                |            |                |             |             |           |            |           |       |         |      |           |             |
| RoundRobin.SelectReplica       | ShortRun   | 3              | 1           | 3           |  5.339 ns |  1.1942 ns | 0.0655 ns |  1.00 |    0.02 |    1 |         - |          NA |
| Random.SelectReplica           | ShortRun   | 3              | 1           | 3           |  8.096 ns |  3.2209 ns | 0.1765 ns |  1.52 |    0.03 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | ShortRun   | 3              | 1           | 3           | 56.461 ns | 14.7236 ns | 0.8071 ns | 10.58 |    0.17 |    4 |         - |          NA |
| LeastConnections.SelectReplica | ShortRun   | 3              | 1           | 3           | 55.979 ns | 23.9501 ns | 1.3128 ns | 10.49 |    0.24 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | ShortRun   | 3              | 1           | 3           | 40.942 ns |  7.1981 ns | 0.3946 ns |  7.67 |    0.10 |    3 |         - |          NA |
