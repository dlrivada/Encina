```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                                    | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|----------:|----------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;LeastConnections.AcquireReplica (lease)&#39; | DefaultJob | Default        | Default     | Default     | 290.529 ns | 0.9448 ns | 0.7890 ns | 290.216 ns | 71.93 |    2.25 |    4 | 0.0019 |      32 B |          NA |
| LeastConnections.SelectReplica            | DefaultJob | Default        | Default     | Default     | 246.895 ns | 0.4755 ns | 0.3971 ns | 246.838 ns | 61.13 |    1.90 |    3 | 0.0019 |      32 B |          NA |
| Random.SelectReplica                      | DefaultJob | Default        | Default     | Default     |  11.185 ns | 0.0068 ns | 0.0057 ns |  11.185 ns |  2.77 |    0.09 |    2 |      - |         - |          NA |
| RoundRobin.SelectReplica                  | DefaultJob | Default        | Default     | Default     |   4.043 ns | 0.1384 ns | 0.1294 ns |   4.148 ns |  1.00 |    0.04 |    1 |      - |         - |          NA |
|                                           |            |                |             |             |            |           |           |            |       |         |      |        |           |             |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | MediumRun  | 15             | 2           | 10          | 285.880 ns | 0.5636 ns | 0.8083 ns | 285.711 ns | 69.98 |    1.94 |    4 | 0.0019 |      32 B |          NA |
| LeastConnections.SelectReplica            | MediumRun  | 15             | 2           | 10          | 243.769 ns | 0.9638 ns | 1.3511 ns | 243.541 ns | 59.67 |    1.68 |    3 | 0.0019 |      32 B |          NA |
| Random.SelectReplica                      | MediumRun  | 15             | 2           | 10          |  11.410 ns | 0.0685 ns | 0.0983 ns |  11.487 ns |  2.79 |    0.08 |    2 |      - |         - |          NA |
| RoundRobin.SelectReplica                  | MediumRun  | 15             | 2           | 10          |   4.088 ns | 0.0744 ns | 0.1114 ns |   4.146 ns |  1.00 |    0.04 |    1 |      - |         - |          NA |
