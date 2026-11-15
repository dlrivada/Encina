```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.03GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                                    | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error    | StdDev   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |--------------- |------------ |------------ |----------:|---------:|---------:|------:|--------:|-----:|----------:|------------:|
| &#39;LeastConnections.AcquireReplica (lease)&#39; | DefaultJob | Default        | Default     | Default     | 288.88 ns | 0.387 ns | 0.343 ns | 24.93 |    0.07 |    4 |      32 B |          NA |
| LeastConnections.SelectReplica            | DefaultJob | Default        | Default     | Default     | 247.47 ns | 1.728 ns | 1.443 ns | 21.36 |    0.13 |    3 |      32 B |          NA |
| Random.SelectReplica                      | DefaultJob | Default        | Default     | Default     |  12.44 ns | 0.036 ns | 0.034 ns |  1.07 |    0.00 |    2 |         - |          NA |
| RoundRobin.SelectReplica                  | DefaultJob | Default        | Default     | Default     |  11.59 ns | 0.030 ns | 0.028 ns |  1.00 |    0.00 |    1 |         - |          NA |
|                                           |            |                |             |             |           |          |          |       |         |      |           |             |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | MediumRun  | 15             | 2           | 10          | 284.83 ns | 1.357 ns | 1.990 ns | 27.21 |    0.20 |    4 |      32 B |          NA |
| LeastConnections.SelectReplica            | MediumRun  | 15             | 2           | 10          | 254.30 ns | 0.655 ns | 0.939 ns | 24.29 |    0.11 |    3 |      32 B |          NA |
| Random.SelectReplica                      | MediumRun  | 15             | 2           | 10          |  13.37 ns | 0.048 ns | 0.070 ns |  1.28 |    0.01 |    2 |         - |          NA |
| RoundRobin.SelectReplica                  | MediumRun  | 15             | 2           | 10          |  10.47 ns | 0.017 ns | 0.025 ns |  1.00 |    0.00 |    1 |         - |          NA |
