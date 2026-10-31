```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                                    | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;LeastConnections.AcquireReplica (lease)&#39; | DefaultJob | Default        | Default     | Default     | 219.070 ns |  1.7362 ns | 1.5391 ns | 20.41 |    0.43 |    3 | 0.0002 |      32 B |          NA |
| LeastConnections.SelectReplica            | DefaultJob | Default        | Default     | Default     | 196.606 ns |  2.1395 ns | 2.0013 ns | 18.32 |    0.41 |    2 | 0.0002 |      32 B |          NA |
| Random.SelectReplica                      | DefaultJob | Default        | Default     | Default     |  11.262 ns |  0.0719 ns | 0.0601 ns |  1.05 |    0.02 |    1 |      - |         - |          NA |
| RoundRobin.SelectReplica                  | DefaultJob | Default        | Default     | Default     |  10.737 ns |  0.2363 ns | 0.2210 ns |  1.00 |    0.03 |    1 |      - |         - |          NA |
|                                           |            |                |             |             |            |            |           |       |         |      |        |           |             |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | ShortRun   | 3              | 1           | 3           | 220.259 ns | 11.4325 ns | 0.6267 ns | 35.30 |    0.31 |    3 | 0.0002 |      32 B |          NA |
| LeastConnections.SelectReplica            | ShortRun   | 3              | 1           | 3           | 194.506 ns | 26.2970 ns | 1.4414 ns | 31.17 |    0.33 |    3 | 0.0002 |      32 B |          NA |
| Random.SelectReplica                      | ShortRun   | 3              | 1           | 3           |  11.077 ns |  2.8747 ns | 0.1576 ns |  1.78 |    0.03 |    2 |      - |         - |          NA |
| RoundRobin.SelectReplica                  | ShortRun   | 3              | 1           | 3           |   6.240 ns |  1.1249 ns | 0.0617 ns |  1.00 |    0.01 |    1 |      - |         - |          NA |
