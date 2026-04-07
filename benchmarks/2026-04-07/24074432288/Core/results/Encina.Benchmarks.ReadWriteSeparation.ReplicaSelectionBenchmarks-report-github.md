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
| &#39;LeastConnections.AcquireReplica (lease)&#39; | DefaultJob | Default        | Default     | Default     | 286.122 ns | 0.3273 ns | 0.2901 ns | 286.152 ns | 69.48 |    1.93 |    4 | 0.0019 |      32 B |          NA |
| LeastConnections.SelectReplica            | DefaultJob | Default        | Default     | Default     | 243.412 ns | 0.3829 ns | 0.3395 ns | 243.520 ns | 59.11 |    1.64 |    3 | 0.0019 |      32 B |          NA |
| Random.SelectReplica                      | DefaultJob | Default        | Default     | Default     |  11.174 ns | 0.0042 ns | 0.0037 ns |  11.175 ns |  2.71 |    0.08 |    2 |      - |         - |          NA |
| RoundRobin.SelectReplica                  | DefaultJob | Default        | Default     | Default     |   4.121 ns | 0.1269 ns | 0.1187 ns |   4.061 ns |  1.00 |    0.04 |    1 |      - |         - |          NA |
|                                           |            |                |             |             |            |           |           |            |       |         |      |        |           |             |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | MediumRun  | 15             | 2           | 10          | 283.084 ns | 2.1769 ns | 3.1220 ns | 283.395 ns | 69.31 |    2.07 |    4 | 0.0019 |      32 B |          NA |
| LeastConnections.SelectReplica            | MediumRun  | 15             | 2           | 10          | 239.139 ns | 1.6252 ns | 2.1696 ns | 237.916 ns | 58.55 |    1.71 |    3 | 0.0019 |      32 B |          NA |
| Random.SelectReplica                      | MediumRun  | 15             | 2           | 10          |  11.571 ns | 0.0629 ns | 0.0881 ns |  11.503 ns |  2.83 |    0.08 |    2 |      - |         - |          NA |
| RoundRobin.SelectReplica                  | MediumRun  | 15             | 2           | 10          |   4.087 ns | 0.0750 ns | 0.1122 ns |   4.147 ns |  1.00 |    0.04 |    1 |      - |         - |          NA |
