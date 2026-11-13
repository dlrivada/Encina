```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.02GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                                    | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |--------------- |------------ |------------ |----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
| &#39;LeastConnections.AcquireReplica (lease)&#39; | DefaultJob | Default        | Default     | Default     | 285.61 ns |  0.861 ns | 0.805 ns | 27.88 |    0.09 |    4 |      32 B |          NA |
| LeastConnections.SelectReplica            | DefaultJob | Default        | Default     | Default     | 240.22 ns |  0.566 ns | 0.502 ns | 23.45 |    0.07 |    3 |      32 B |          NA |
| Random.SelectReplica                      | DefaultJob | Default        | Default     | Default     |  12.08 ns |  0.015 ns | 0.012 ns |  1.18 |    0.00 |    2 |         - |          NA |
| RoundRobin.SelectReplica                  | DefaultJob | Default        | Default     | Default     |  10.24 ns |  0.026 ns | 0.021 ns |  1.00 |    0.00 |    1 |         - |          NA |
|                                           |            |                |             |             |           |           |          |       |         |      |           |             |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | ShortRun   | 3              | 1           | 3           | 278.97 ns | 22.055 ns | 1.209 ns | 27.47 |    0.12 |    3 |      32 B |          NA |
| LeastConnections.SelectReplica            | ShortRun   | 3              | 1           | 3           | 247.31 ns | 35.482 ns | 1.945 ns | 24.35 |    0.17 |    3 |      32 B |          NA |
| Random.SelectReplica                      | ShortRun   | 3              | 1           | 3           |  12.95 ns |  0.108 ns | 0.006 ns |  1.28 |    0.00 |    2 |         - |          NA |
| RoundRobin.SelectReplica                  | ShortRun   | 3              | 1           | 3           |  10.16 ns |  0.407 ns | 0.022 ns |  1.00 |    0.00 |    1 |         - |          NA |
