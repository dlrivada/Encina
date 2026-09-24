```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                                    | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;LeastConnections.AcquireReplica (lease)&#39; | DefaultJob | Default        | Default     | Default     | 293.878 ns | 0.3015 ns | 0.2820 ns | 33.57 |    0.04 |    4 | 0.0010 |      32 B |          NA |
| LeastConnections.SelectReplica            | DefaultJob | Default        | Default     | Default     | 255.101 ns | 0.2788 ns | 0.2328 ns | 29.14 |    0.03 |    3 | 0.0010 |      32 B |          NA |
| Random.SelectReplica                      | DefaultJob | Default        | Default     | Default     |  10.765 ns | 0.0403 ns | 0.0357 ns |  1.23 |    0.00 |    2 |      - |         - |          NA |
| RoundRobin.SelectReplica                  | DefaultJob | Default        | Default     | Default     |   8.754 ns | 0.0087 ns | 0.0073 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
|                                           |            |                |             |             |            |           |           |       |         |      |        |           |             |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | ShortRun   | 3              | 1           | 3           | 283.655 ns | 7.7892 ns | 0.4270 ns | 31.75 |    0.04 |    3 | 0.0010 |      32 B |          NA |
| LeastConnections.SelectReplica            | ShortRun   | 3              | 1           | 3           | 242.534 ns | 9.8595 ns | 0.5404 ns | 27.14 |    0.05 |    3 | 0.0010 |      32 B |          NA |
| Random.SelectReplica                      | ShortRun   | 3              | 1           | 3           |  11.224 ns | 0.2288 ns | 0.0125 ns |  1.26 |    0.00 |    2 |      - |         - |          NA |
| RoundRobin.SelectReplica                  | ShortRun   | 3              | 1           | 3           |   8.935 ns | 0.0518 ns | 0.0028 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
