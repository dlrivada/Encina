```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                                    | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;LeastConnections.AcquireReplica (lease)&#39; | DefaultJob | Default        | Default     | Default     | 155.040 ns |  1.3110 ns | 1.2263 ns | 28.87 |    0.30 |    4 | 0.0019 |      32 B |          NA |
| LeastConnections.SelectReplica            | DefaultJob | Default        | Default     | Default     | 133.095 ns |  0.8780 ns | 0.8212 ns | 24.79 |    0.23 |    3 | 0.0019 |      32 B |          NA |
| Random.SelectReplica                      | DefaultJob | Default        | Default     | Default     |   6.914 ns |  0.0746 ns | 0.0662 ns |  1.29 |    0.01 |    2 |      - |         - |          NA |
| RoundRobin.SelectReplica                  | DefaultJob | Default        | Default     | Default     |   5.370 ns |  0.0470 ns | 0.0393 ns |  1.00 |    0.01 |    1 |      - |         - |          NA |
|                                           |            |                |             |             |            |            |           |       |         |      |        |           |             |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | ShortRun   | 3              | 1           | 3           | 154.249 ns |  9.0914 ns | 0.4983 ns | 30.37 |    0.32 |    3 | 0.0019 |      32 B |          NA |
| LeastConnections.SelectReplica            | ShortRun   | 3              | 1           | 3           | 133.363 ns | 38.7125 ns | 2.1220 ns | 26.26 |    0.45 |    3 | 0.0019 |      32 B |          NA |
| Random.SelectReplica                      | ShortRun   | 3              | 1           | 3           |   6.840 ns |  2.9666 ns | 0.1626 ns |  1.35 |    0.03 |    2 |      - |         - |          NA |
| RoundRobin.SelectReplica                  | ShortRun   | 3              | 1           | 3           |   5.079 ns |  1.0734 ns | 0.0588 ns |  1.00 |    0.01 |    1 |      - |         - |          NA |
