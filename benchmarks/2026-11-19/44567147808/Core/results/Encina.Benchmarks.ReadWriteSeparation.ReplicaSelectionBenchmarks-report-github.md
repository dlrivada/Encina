```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                                    | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
| &#39;LeastConnections.AcquireReplica (lease)&#39; | DefaultJob | Default        | Default     | Default     | 270.777 ns |  1.2030 ns | 1.0046 ns | 25.20 |    0.20 |    4 |      32 B |          NA |
| LeastConnections.SelectReplica            | DefaultJob | Default        | Default     | Default     | 227.743 ns |  2.6706 ns | 2.3674 ns | 21.19 |    0.26 |    3 |      32 B |          NA |
| Random.SelectReplica                      | DefaultJob | Default        | Default     | Default     |  12.113 ns |  0.1791 ns | 0.1588 ns |  1.13 |    0.02 |    2 |         - |          NA |
| RoundRobin.SelectReplica                  | DefaultJob | Default        | Default     | Default     |  10.747 ns |  0.0832 ns | 0.0778 ns |  1.00 |    0.01 |    1 |         - |          NA |
|                                           |            |                |             |             |            |            |           |       |         |      |           |             |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | ShortRun   | 3              | 1           | 3           | 266.833 ns | 25.4429 ns | 1.3946 ns | 26.90 |    0.15 |    2 |      32 B |          NA |
| LeastConnections.SelectReplica            | ShortRun   | 3              | 1           | 3           | 230.682 ns | 34.0860 ns | 1.8684 ns | 23.26 |    0.18 |    2 |      32 B |          NA |
| Random.SelectReplica                      | ShortRun   | 3              | 1           | 3           |  11.820 ns |  1.7821 ns | 0.0977 ns |  1.19 |    0.01 |    1 |         - |          NA |
| RoundRobin.SelectReplica                  | ShortRun   | 3              | 1           | 3           |   9.919 ns |  0.6614 ns | 0.0363 ns |  1.00 |    0.00 |    1 |         - |          NA |
