```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                                    | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;LeastConnections.AcquireReplica (lease)&#39; | DefaultJob | Default        | Default     | Default     | 259.210 ns |  3.4107 ns | 3.1904 ns | 25.19 |    0.39 |    4 |      - |      32 B |          NA |
| LeastConnections.SelectReplica            | DefaultJob | Default        | Default     | Default     | 225.249 ns |  3.2139 ns | 3.0063 ns | 21.89 |    0.36 |    3 |      - |      32 B |          NA |
| Random.SelectReplica                      | DefaultJob | Default        | Default     | Default     |  11.634 ns |  0.2306 ns | 0.1926 ns |  1.13 |    0.02 |    2 |      - |         - |          NA |
| RoundRobin.SelectReplica                  | DefaultJob | Default        | Default     | Default     |  10.292 ns |  0.1152 ns | 0.1078 ns |  1.00 |    0.01 |    1 |      - |         - |          NA |
|                                           |            |                |             |             |            |            |           |       |         |      |        |           |             |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | ShortRun   | 3              | 1           | 3           | 257.335 ns | 45.4195 ns | 2.4896 ns | 27.73 |    0.26 |    3 |      - |      32 B |          NA |
| LeastConnections.SelectReplica            | ShortRun   | 3              | 1           | 3           | 217.718 ns | 86.4856 ns | 4.7406 ns | 23.46 |    0.45 |    3 | 0.0002 |      32 B |          NA |
| Random.SelectReplica                      | ShortRun   | 3              | 1           | 3           |  12.055 ns |  5.4003 ns | 0.2960 ns |  1.30 |    0.03 |    2 |      - |         - |          NA |
| RoundRobin.SelectReplica                  | ShortRun   | 3              | 1           | 3           |   9.282 ns |  0.7936 ns | 0.0435 ns |  1.00 |    0.01 |    1 |      - |         - |          NA |
