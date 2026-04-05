```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                                    | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean             | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |--------------- |------------ |------------ |------------- |------------ |-----------------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| RoundRobin.SelectReplica                  | DefaultJob | Default        | Default     | Default     | 16           | Default     |         4.060 ns | 0.1297 ns | 0.1213 ns |  1.00 |    0.04 |    1 |      - |         - |          NA |
| Random.SelectReplica                      | DefaultJob | Default        | Default     | Default     | 16           | Default     |        11.181 ns | 0.0087 ns | 0.0072 ns |  2.76 |    0.08 |    2 |      - |         - |          NA |
| LeastConnections.SelectReplica            | DefaultJob | Default        | Default     | Default     | 16           | Default     |       239.331 ns | 0.7129 ns | 0.6320 ns | 59.00 |    1.74 |    3 | 0.0019 |      32 B |          NA |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | DefaultJob | Default        | Default     | Default     | 16           | Default     |       329.920 ns | 0.3223 ns | 0.2857 ns | 81.33 |    2.39 |    4 | 0.0019 |      32 B |          NA |
|                                           |            |                |             |             |              |             |                  |           |           |       |         |      |        |           |             |
| RoundRobin.SelectReplica                  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   554,752.000 ns |        NA | 0.0000 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Random.SelectReplica                      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   626,046.000 ns |        NA | 0.0000 ns |  1.13 |    0.00 |    2 |      - |         - |          NA |
| LeastConnections.SelectReplica            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,849,313.000 ns |        NA | 0.0000 ns |  3.33 |    0.00 |    3 |      - |      32 B |          NA |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 2,264,304.000 ns |        NA | 0.0000 ns |  4.08 |    0.00 |    4 |      - |      32 B |          NA |
