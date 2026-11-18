```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                                    | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;LeastConnections.AcquireReplica (lease)&#39; | DefaultJob | Default        | Default     | Default     | 209.918 ns | 0.2689 ns | 0.2383 ns | 36.72 |    0.56 |    4 | 0.0002 |      32 B |          NA |
| LeastConnections.SelectReplica            | DefaultJob | Default        | Default     | Default     | 183.600 ns | 0.3206 ns | 0.2677 ns | 32.12 |    0.49 |    3 | 0.0002 |      32 B |          NA |
| Random.SelectReplica                      | DefaultJob | Default        | Default     | Default     |  10.424 ns | 0.0073 ns | 0.0068 ns |  1.82 |    0.03 |    2 |      - |         - |          NA |
| RoundRobin.SelectReplica                  | DefaultJob | Default        | Default     | Default     |   5.717 ns | 0.0958 ns | 0.0896 ns |  1.00 |    0.02 |    1 |      - |         - |          NA |
|                                           |            |                |             |             |            |           |           |       |         |      |        |           |             |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | ShortRun   | 3              | 1           | 3           | 210.090 ns | 3.6977 ns | 0.2027 ns | 34.12 |    0.14 |    3 | 0.0002 |      32 B |          NA |
| LeastConnections.SelectReplica            | ShortRun   | 3              | 1           | 3           | 189.584 ns | 7.7982 ns | 0.4274 ns | 30.79 |    0.14 |    3 | 0.0002 |      32 B |          NA |
| Random.SelectReplica                      | ShortRun   | 3              | 1           | 3           |  10.017 ns | 0.3399 ns | 0.0186 ns |  1.63 |    0.01 |    2 |      - |         - |          NA |
| RoundRobin.SelectReplica                  | ShortRun   | 3              | 1           | 3           |   6.157 ns | 0.5081 ns | 0.0279 ns |  1.00 |    0.01 |    1 |      - |         - |          NA |
