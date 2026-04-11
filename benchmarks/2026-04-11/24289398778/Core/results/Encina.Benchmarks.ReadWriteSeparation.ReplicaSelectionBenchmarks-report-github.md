```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                                    | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|----------:|----------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;LeastConnections.AcquireReplica (lease)&#39; | DefaultJob | Default        | Default     | Default     | 278.797 ns | 0.1791 ns | 0.1398 ns | 278.775 ns | 68.18 |    0.08 |    4 | 0.0019 |      32 B |          NA |
| LeastConnections.SelectReplica            | DefaultJob | Default        | Default     | Default     | 219.902 ns | 0.0693 ns | 0.0541 ns | 219.909 ns | 53.78 |    0.06 |    3 | 0.0019 |      32 B |          NA |
| Random.SelectReplica                      | DefaultJob | Default        | Default     | Default     |  12.263 ns | 0.0103 ns | 0.0086 ns |  12.267 ns |  3.00 |    0.00 |    2 |      - |         - |          NA |
| RoundRobin.SelectReplica                  | DefaultJob | Default        | Default     | Default     |   4.089 ns | 0.0049 ns | 0.0043 ns |   4.088 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
|                                           |            |                |             |             |            |           |           |            |       |         |      |        |           |             |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | MediumRun  | 15             | 2           | 10          | 275.698 ns | 0.1678 ns | 0.2296 ns | 275.663 ns | 69.15 |    2.30 |    4 | 0.0019 |      32 B |          NA |
| LeastConnections.SelectReplica            | MediumRun  | 15             | 2           | 10          | 218.901 ns | 0.5732 ns | 0.7846 ns | 219.336 ns | 54.90 |    1.83 |    3 | 0.0019 |      32 B |          NA |
| Random.SelectReplica                      | MediumRun  | 15             | 2           | 10          |  12.442 ns | 0.1603 ns | 0.2247 ns |  12.314 ns |  3.12 |    0.12 |    2 |      - |         - |          NA |
| RoundRobin.SelectReplica                  | MediumRun  | 15             | 2           | 10          |   3.992 ns | 0.0976 ns | 0.1335 ns |   4.034 ns |  1.00 |    0.05 |    1 |      - |         - |          NA |
