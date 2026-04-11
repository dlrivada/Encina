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
| &#39;LeastConnections.AcquireReplica (lease)&#39; | DefaultJob | Default        | Default     | Default     | 288.336 ns | 0.2547 ns | 0.2127 ns | 288.337 ns | 71.74 |    2.30 |    4 | 0.0019 |      32 B |          NA |
| LeastConnections.SelectReplica            | DefaultJob | Default        | Default     | Default     | 243.921 ns | 0.4451 ns | 0.3717 ns | 243.713 ns | 60.69 |    1.95 |    3 | 0.0019 |      32 B |          NA |
| Random.SelectReplica                      | DefaultJob | Default        | Default     | Default     |  11.185 ns | 0.0140 ns | 0.0117 ns |  11.182 ns |  2.78 |    0.09 |    2 |      - |         - |          NA |
| RoundRobin.SelectReplica                  | DefaultJob | Default        | Default     | Default     |   4.023 ns | 0.1432 ns | 0.1340 ns |   3.909 ns |  1.00 |    0.05 |    1 |      - |         - |          NA |
|                                           |            |                |             |             |            |           |           |            |       |         |      |        |           |             |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | MediumRun  | 15             | 2           | 10          | 280.662 ns | 0.4237 ns | 0.6077 ns | 280.642 ns | 68.18 |    1.85 |    4 | 0.0019 |      32 B |          NA |
| LeastConnections.SelectReplica            | MediumRun  | 15             | 2           | 10          | 238.925 ns | 0.3132 ns | 0.4689 ns | 238.878 ns | 58.04 |    1.57 |    3 | 0.0019 |      32 B |          NA |
| Random.SelectReplica                      | MediumRun  | 15             | 2           | 10          |  11.585 ns | 0.0692 ns | 0.0924 ns |  11.659 ns |  2.81 |    0.08 |    2 |      - |         - |          NA |
| RoundRobin.SelectReplica                  | MediumRun  | 15             | 2           | 10          |   4.119 ns | 0.0735 ns | 0.1099 ns |   4.151 ns |  1.00 |    0.04 |    1 |      - |         - |          NA |
