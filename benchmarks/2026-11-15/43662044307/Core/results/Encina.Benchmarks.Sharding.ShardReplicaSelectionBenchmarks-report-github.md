```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     |  5.979 ns | 0.0306 ns | 0.0271 ns |  1.00 |    0.01 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     |  8.803 ns | 0.1135 ns | 0.1061 ns |  1.47 |    0.02 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 61.027 ns | 0.6351 ns | 0.4959 ns | 10.21 |    0.09 |    5 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     | 58.302 ns | 0.2249 ns | 0.2104 ns |  9.75 |    0.05 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     | 46.218 ns | 0.3632 ns | 0.3033 ns |  7.73 |    0.06 |    3 |         - |          NA |
|                                |            |                |             |             |           |           |           |       |         |      |           |             |
| RoundRobin.SelectReplica       | MediumRun  | 15             | 2           | 10          |  6.063 ns | 0.0581 ns | 0.0851 ns |  1.00 |    0.02 |    1 |         - |          NA |
| Random.SelectReplica           | MediumRun  | 15             | 2           | 10          |  8.943 ns | 0.1185 ns | 0.1699 ns |  1.48 |    0.03 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | MediumRun  | 15             | 2           | 10          | 61.536 ns | 0.4244 ns | 0.6222 ns | 10.15 |    0.17 |    4 |         - |          NA |
| LeastConnections.SelectReplica | MediumRun  | 15             | 2           | 10          | 60.110 ns | 1.0824 ns | 1.6201 ns |  9.92 |    0.30 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | MediumRun  | 15             | 2           | 10          | 45.147 ns | 0.4121 ns | 0.6040 ns |  7.45 |    0.14 |    3 |         - |          NA |
