```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                                    | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| RoundRobin.SelectReplica                  | DefaultJob | Default        | Default     | Default     |   4.025 ns |  0.1345 ns | 0.1259 ns |  1.00 |    0.04 |    1 |      - |         - |          NA |
| Random.SelectReplica                      | DefaultJob | Default        | Default     | Default     |  11.187 ns |  0.0134 ns | 0.0112 ns |  2.78 |    0.08 |    2 |      - |         - |          NA |
| LeastConnections.SelectReplica            | DefaultJob | Default        | Default     | Default     | 251.654 ns |  0.5053 ns | 0.4480 ns | 62.58 |    1.89 |    3 | 0.0019 |      32 B |          NA |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | DefaultJob | Default        | Default     | Default     | 285.972 ns |  0.5077 ns | 0.4749 ns | 71.12 |    2.15 |    4 | 0.0019 |      32 B |          NA |
|                                           |            |                |             |             |            |            |           |       |         |      |        |           |             |
| RoundRobin.SelectReplica                  | ShortRun   | 3              | 1           | 3           |   3.907 ns |  0.0415 ns | 0.0023 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Random.SelectReplica                      | ShortRun   | 3              | 1           | 3           |  11.592 ns |  2.9786 ns | 0.1633 ns |  2.97 |    0.04 |    2 |      - |         - |          NA |
| LeastConnections.SelectReplica            | ShortRun   | 3              | 1           | 3           | 241.311 ns |  7.9797 ns | 0.4374 ns | 61.77 |    0.10 |    3 | 0.0019 |      32 B |          NA |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | ShortRun   | 3              | 1           | 3           | 278.923 ns | 11.8581 ns | 0.6500 ns | 71.40 |    0.15 |    3 | 0.0019 |      32 B |          NA |
