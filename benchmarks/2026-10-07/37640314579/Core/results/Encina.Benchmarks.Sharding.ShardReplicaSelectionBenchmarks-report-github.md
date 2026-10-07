```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     |  4.759 ns | 0.1396 ns | 0.1306 ns |  1.00 |    0.04 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     | 11.323 ns | 0.1578 ns | 0.1476 ns |  2.38 |    0.07 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 86.633 ns | 0.0735 ns | 0.0614 ns | 18.22 |    0.48 |    5 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     | 80.360 ns | 0.1057 ns | 0.0988 ns | 16.90 |    0.45 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     | 53.306 ns | 0.0276 ns | 0.0231 ns | 11.21 |    0.30 |    3 |         - |          NA |
|                                |            |                |             |             |           |           |           |       |         |      |           |             |
| RoundRobin.SelectReplica       | ShortRun   | 3              | 1           | 3           |  4.442 ns | 2.8867 ns | 0.1582 ns |  1.00 |    0.04 |    1 |         - |          NA |
| Random.SelectReplica           | ShortRun   | 3              | 1           | 3           | 10.864 ns | 2.9069 ns | 0.1593 ns |  2.45 |    0.08 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | ShortRun   | 3              | 1           | 3           | 82.547 ns | 0.6116 ns | 0.0335 ns | 18.60 |    0.56 |    4 |         - |          NA |
| LeastConnections.SelectReplica | ShortRun   | 3              | 1           | 3           | 80.727 ns | 0.6707 ns | 0.0368 ns | 18.19 |    0.55 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | ShortRun   | 3              | 1           | 3           | 51.137 ns | 0.3410 ns | 0.0187 ns | 11.52 |    0.35 |    3 |         - |          NA |
