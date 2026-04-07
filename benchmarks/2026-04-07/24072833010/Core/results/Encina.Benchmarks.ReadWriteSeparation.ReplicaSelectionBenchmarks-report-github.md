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
| &#39;LeastConnections.AcquireReplica (lease)&#39; | DefaultJob | Default        | Default     | Default     | 16           | Default     |       288.593 ns | 0.9404 ns | 0.8797 ns | 70.25 |    2.20 |    4 | 0.0019 |      32 B |          NA |
| LeastConnections.SelectReplica            | DefaultJob | Default        | Default     | Default     | 16           | Default     |       242.143 ns | 0.4155 ns | 0.3470 ns | 58.94 |    1.84 |    3 | 0.0019 |      32 B |          NA |
| Random.SelectReplica                      | DefaultJob | Default        | Default     | Default     | 16           | Default     |        11.194 ns | 0.0191 ns | 0.0169 ns |  2.72 |    0.09 |    2 |      - |         - |          NA |
| RoundRobin.SelectReplica                  | DefaultJob | Default        | Default     | Default     | 16           | Default     |         4.112 ns | 0.1387 ns | 0.1298 ns |  1.00 |    0.04 |    1 |      - |         - |          NA |
|                                           |            |                |             |             |              |             |                  |           |           |       |         |      |        |           |             |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 2,465,695.000 ns |        NA | 0.0000 ns |  4.04 |    0.00 |    4 |      - |      32 B |          NA |
| LeastConnections.SelectReplica            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,995,364.000 ns |        NA | 0.0000 ns |  3.27 |    0.00 |    3 |      - |      32 B |          NA |
| Random.SelectReplica                      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   660,454.000 ns |        NA | 0.0000 ns |  1.08 |    0.00 |    2 |      - |         - |          NA |
| RoundRobin.SelectReplica                  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   610,290.000 ns |        NA | 0.0000 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
