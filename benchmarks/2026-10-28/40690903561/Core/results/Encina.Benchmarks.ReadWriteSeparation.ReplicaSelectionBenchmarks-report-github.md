```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                                    | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;LeastConnections.AcquireReplica (lease)&#39; | DefaultJob | Default        | Default     | Default     | 163.871 ns |  1.8937 ns | 1.6787 ns | 31.58 |    0.66 |    4 | 0.0019 |      32 B |          NA |
| LeastConnections.SelectReplica            | DefaultJob | Default        | Default     | Default     | 138.355 ns |  2.7745 ns | 3.4074 ns | 26.66 |    0.81 |    3 | 0.0019 |      32 B |          NA |
| Random.SelectReplica                      | DefaultJob | Default        | Default     | Default     |   7.151 ns |  0.1785 ns | 0.1670 ns |  1.38 |    0.04 |    2 |      - |         - |          NA |
| RoundRobin.SelectReplica                  | DefaultJob | Default        | Default     | Default     |   5.191 ns |  0.1049 ns | 0.0981 ns |  1.00 |    0.03 |    1 |      - |         - |          NA |
|                                           |            |                |             |             |            |            |           |       |         |      |        |           |             |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | ShortRun   | 3              | 1           | 3           | 166.596 ns | 23.7494 ns | 1.3018 ns | 31.46 |    0.68 |    4 | 0.0019 |      32 B |          NA |
| LeastConnections.SelectReplica            | ShortRun   | 3              | 1           | 3           | 138.663 ns | 52.3326 ns | 2.8685 ns | 26.19 |    0.71 |    3 | 0.0019 |      32 B |          NA |
| Random.SelectReplica                      | ShortRun   | 3              | 1           | 3           |   6.986 ns |  0.9125 ns | 0.0500 ns |  1.32 |    0.03 |    2 |      - |         - |          NA |
| RoundRobin.SelectReplica                  | ShortRun   | 3              | 1           | 3           |   5.297 ns |  2.2538 ns | 0.1235 ns |  1.00 |    0.03 |    1 |      - |         - |          NA |
