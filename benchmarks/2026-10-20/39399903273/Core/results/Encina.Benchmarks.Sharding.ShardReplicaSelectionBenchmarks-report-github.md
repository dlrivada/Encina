```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.55GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error       | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |----------:|------------:|----------:|------:|--------:|-----:|----------:|------------:|
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     |  5.901 ns |   0.1623 ns | 0.2479 ns |  1.00 |    0.06 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     | 11.215 ns |   0.2458 ns | 0.2052 ns |  1.90 |    0.08 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 75.653 ns |   0.7024 ns | 0.5865 ns | 12.84 |    0.51 |    4 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     | 74.283 ns |   1.3071 ns | 1.2837 ns | 12.61 |    0.54 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     | 54.411 ns |   0.5489 ns | 0.4866 ns |  9.24 |    0.37 |    3 |         - |          NA |
|                                |            |                |             |             |           |             |           |       |         |      |           |             |
| RoundRobin.SelectReplica       | ShortRun   | 3              | 1           | 3           |  6.298 ns |   3.3532 ns | 0.1838 ns |  1.00 |    0.04 |    1 |         - |          NA |
| Random.SelectReplica           | ShortRun   | 3              | 1           | 3           | 12.304 ns |   9.8320 ns | 0.5389 ns |  1.95 |    0.09 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | ShortRun   | 3              | 1           | 3           | 79.937 ns |  88.4057 ns | 4.8458 ns | 12.70 |    0.74 |    4 |         - |          NA |
| LeastConnections.SelectReplica | ShortRun   | 3              | 1           | 3           | 80.193 ns | 128.6859 ns | 7.0537 ns | 12.74 |    1.02 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | ShortRun   | 3              | 1           | 3           | 57.716 ns |  38.4481 ns | 2.1075 ns |  9.17 |    0.37 |    3 |         - |          NA |
