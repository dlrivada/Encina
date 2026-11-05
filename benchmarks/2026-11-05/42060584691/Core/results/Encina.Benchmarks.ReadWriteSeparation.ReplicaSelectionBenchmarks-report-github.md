```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3


```
| Method                                    | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error      | StdDev    | Median     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-----------:|----------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;LeastConnections.AcquireReplica (lease)&#39; | DefaultJob | Default        | Default     | Default     | 283.690 ns |  0.5651 ns | 0.4412 ns | 283.456 ns | 70.69 |    2.20 |    4 | 0.0019 |      32 B |          NA |
| LeastConnections.SelectReplica            | DefaultJob | Default        | Default     | Default     | 241.250 ns |  0.6804 ns | 0.6032 ns | 241.158 ns | 60.11 |    1.87 |    3 | 0.0019 |      32 B |          NA |
| Random.SelectReplica                      | DefaultJob | Default        | Default     | Default     |  11.171 ns |  0.0093 ns | 0.0082 ns |  11.168 ns |  2.78 |    0.09 |    2 |      - |         - |          NA |
| RoundRobin.SelectReplica                  | DefaultJob | Default        | Default     | Default     |   4.017 ns |  0.1382 ns | 0.1292 ns |   3.904 ns |  1.00 |    0.04 |    1 |      - |         - |          NA |
|                                           |            |                |             |             |            |            |           |            |       |         |      |        |           |             |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | ShortRun   | 3              | 1           | 3           | 286.439 ns | 11.3465 ns | 0.6219 ns | 286.088 ns | 73.38 |    0.19 |    3 | 0.0019 |      32 B |          NA |
| LeastConnections.SelectReplica            | ShortRun   | 3              | 1           | 3           | 238.794 ns | 11.0692 ns | 0.6067 ns | 238.796 ns | 61.17 |    0.17 |    3 | 0.0019 |      32 B |          NA |
| Random.SelectReplica                      | ShortRun   | 3              | 1           | 3           |  11.637 ns |  0.0828 ns | 0.0045 ns |  11.638 ns |  2.98 |    0.01 |    2 |      - |         - |          NA |
| RoundRobin.SelectReplica                  | ShortRun   | 3              | 1           | 3           |   3.904 ns |  0.1473 ns | 0.0081 ns |   3.902 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
