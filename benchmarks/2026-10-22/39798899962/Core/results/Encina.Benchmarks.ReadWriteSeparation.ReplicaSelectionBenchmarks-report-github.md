```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3


```
| Method                                    | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error      | StdDev    | Median     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-----------:|----------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;LeastConnections.AcquireReplica (lease)&#39; | DefaultJob | Default        | Default     | Default     | 286.190 ns |  0.5083 ns | 0.4506 ns | 285.974 ns | 71.00 |    2.20 |    4 | 0.0019 |      32 B |          NA |
| LeastConnections.SelectReplica            | DefaultJob | Default        | Default     | Default     | 241.802 ns |  0.3076 ns | 0.2569 ns | 241.855 ns | 59.98 |    1.86 |    3 | 0.0019 |      32 B |          NA |
| Random.SelectReplica                      | DefaultJob | Default        | Default     | Default     |  10.906 ns |  0.0037 ns | 0.0029 ns |  10.906 ns |  2.71 |    0.08 |    2 |      - |         - |          NA |
| RoundRobin.SelectReplica                  | DefaultJob | Default        | Default     | Default     |   4.035 ns |  0.1374 ns | 0.1285 ns |   4.139 ns |  1.00 |    0.04 |    1 |      - |         - |          NA |
|                                           |            |                |             |             |            |            |           |            |       |         |      |        |           |             |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | ShortRun   | 3              | 1           | 3           | 287.207 ns | 18.6429 ns | 1.0219 ns | 287.414 ns | 73.59 |    0.23 |    4 | 0.0019 |      32 B |          NA |
| LeastConnections.SelectReplica            | ShortRun   | 3              | 1           | 3           | 237.560 ns | 14.1496 ns | 0.7756 ns | 237.148 ns | 60.87 |    0.17 |    3 | 0.0019 |      32 B |          NA |
| Random.SelectReplica                      | ShortRun   | 3              | 1           | 3           |  11.663 ns |  0.1008 ns | 0.0055 ns |  11.664 ns |  2.99 |    0.00 |    2 |      - |         - |          NA |
| RoundRobin.SelectReplica                  | ShortRun   | 3              | 1           | 3           |   3.903 ns |  0.0138 ns | 0.0008 ns |   3.903 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
