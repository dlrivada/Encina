```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.17GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     |  5.816 ns |  0.0368 ns | 0.0344 ns |  1.00 |    0.01 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     |  8.482 ns |  0.1651 ns | 0.1463 ns |  1.46 |    0.03 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 58.518 ns |  0.7191 ns | 0.6004 ns | 10.06 |    0.11 |    4 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     | 57.758 ns |  0.4127 ns | 0.3860 ns |  9.93 |    0.09 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     | 43.002 ns |  0.4137 ns | 0.3668 ns |  7.39 |    0.07 |    3 |         - |          NA |
|                                |            |                |             |             |           |            |           |       |         |      |           |             |
| RoundRobin.SelectReplica       | ShortRun   | 3              | 1           | 3           |  5.792 ns |  0.0568 ns | 0.0031 ns |  1.00 |    0.00 |    1 |         - |          NA |
| Random.SelectReplica           | ShortRun   | 3              | 1           | 3           |  8.058 ns |  5.0784 ns | 0.2784 ns |  1.39 |    0.04 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | ShortRun   | 3              | 1           | 3           | 57.578 ns | 11.1554 ns | 0.6115 ns |  9.94 |    0.09 |    4 |         - |          NA |
| LeastConnections.SelectReplica | ShortRun   | 3              | 1           | 3           | 56.462 ns | 30.7476 ns | 1.6854 ns |  9.75 |    0.25 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | ShortRun   | 3              | 1           | 3           | 43.359 ns | 19.1327 ns | 1.0487 ns |  7.49 |    0.16 |    3 |         - |          NA |
