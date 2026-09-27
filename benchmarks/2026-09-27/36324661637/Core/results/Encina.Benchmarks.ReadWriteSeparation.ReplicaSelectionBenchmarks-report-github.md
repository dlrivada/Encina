```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                                    | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |--------------- |------------ |------------ |----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
| &#39;LeastConnections.AcquireReplica (lease)&#39; | DefaultJob | Default        | Default     | Default     | 284.42 ns |  1.351 ns | 1.264 ns | 24.58 |    0.15 |    4 |      32 B |          NA |
| LeastConnections.SelectReplica            | DefaultJob | Default        | Default     | Default     | 254.46 ns |  1.022 ns | 0.906 ns | 21.99 |    0.12 |    3 |      32 B |          NA |
| Random.SelectReplica                      | DefaultJob | Default        | Default     | Default     |  12.95 ns |  0.064 ns | 0.057 ns |  1.12 |    0.01 |    2 |         - |          NA |
| RoundRobin.SelectReplica                  | DefaultJob | Default        | Default     | Default     |  11.57 ns |  0.052 ns | 0.048 ns |  1.00 |    0.01 |    1 |         - |          NA |
|                                           |            |                |             |             |           |           |          |       |         |      |           |             |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | ShortRun   | 3              | 1           | 3           | 288.47 ns | 22.304 ns | 1.223 ns | 28.05 |    0.13 |    4 |      32 B |          NA |
| LeastConnections.SelectReplica            | ShortRun   | 3              | 1           | 3           | 240.38 ns | 20.743 ns | 1.137 ns | 23.37 |    0.11 |    3 |      32 B |          NA |
| Random.SelectReplica                      | ShortRun   | 3              | 1           | 3           |  12.67 ns |  0.765 ns | 0.042 ns |  1.23 |    0.00 |    2 |         - |          NA |
| RoundRobin.SelectReplica                  | ShortRun   | 3              | 1           | 3           |  10.28 ns |  0.555 ns | 0.030 ns |  1.00 |    0.00 |    1 |         - |          NA |
