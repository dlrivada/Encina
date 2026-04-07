```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                                    | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;LeastConnections.AcquireReplica (lease)&#39; | DefaultJob | Default        | Default     | Default     | 304.663 ns |  0.4140 ns | 0.3457 ns | 75.75 |    2.28 |    4 | 0.0019 |      32 B |          NA |
| LeastConnections.SelectReplica            | DefaultJob | Default        | Default     | Default     | 239.342 ns |  0.2244 ns | 0.1752 ns | 59.51 |    1.79 |    3 | 0.0019 |      32 B |          NA |
| Random.SelectReplica                      | DefaultJob | Default        | Default     | Default     |  11.177 ns |  0.0050 ns | 0.0039 ns |  2.78 |    0.08 |    2 |      - |         - |          NA |
| RoundRobin.SelectReplica                  | DefaultJob | Default        | Default     | Default     |   4.025 ns |  0.1341 ns | 0.1255 ns |  1.00 |    0.04 |    1 |      - |         - |          NA |
|                                           |            |                |             |             |            |            |           |       |         |      |        |           |             |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | ShortRun   | 3              | 1           | 3           | 280.242 ns | 14.7591 ns | 0.8090 ns | 71.67 |    0.18 |    3 | 0.0019 |      32 B |          NA |
| LeastConnections.SelectReplica            | ShortRun   | 3              | 1           | 3           | 238.897 ns |  7.9132 ns | 0.4337 ns | 61.10 |    0.10 |    3 | 0.0019 |      32 B |          NA |
| Random.SelectReplica                      | ShortRun   | 3              | 1           | 3           |  11.501 ns |  0.3641 ns | 0.0200 ns |  2.94 |    0.00 |    2 |      - |         - |          NA |
| RoundRobin.SelectReplica                  | ShortRun   | 3              | 1           | 3           |   3.910 ns |  0.0450 ns | 0.0025 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
