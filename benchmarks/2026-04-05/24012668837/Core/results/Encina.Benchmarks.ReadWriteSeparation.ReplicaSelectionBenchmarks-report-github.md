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
| RoundRobin.SelectReplica                  | DefaultJob | Default        | Default     | Default     | 16           | Default     |         4.180 ns | 0.0397 ns | 0.0372 ns |  1.00 |    0.01 |    1 |      - |         - |          NA |
| Random.SelectReplica                      | DefaultJob | Default        | Default     | Default     | 16           | Default     |        11.348 ns | 0.0712 ns | 0.0631 ns |  2.72 |    0.03 |    2 |      - |         - |          NA |
| LeastConnections.SelectReplica            | DefaultJob | Default        | Default     | Default     | 16           | Default     |       242.676 ns | 0.4627 ns | 0.4101 ns | 58.06 |    0.51 |    3 | 0.0019 |      32 B |          NA |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | DefaultJob | Default        | Default     | Default     | 16           | Default     |       294.847 ns | 0.3124 ns | 0.2439 ns | 70.54 |    0.62 |    4 | 0.0019 |      32 B |          NA |
|                                           |            |                |             |             |              |             |                  |           |           |       |         |      |        |           |             |
| RoundRobin.SelectReplica                  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   577,941.000 ns |        NA | 0.0000 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Random.SelectReplica                      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   687,816.000 ns |        NA | 0.0000 ns |  1.19 |    0.00 |    2 |      - |         - |          NA |
| LeastConnections.SelectReplica            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,925,752.000 ns |        NA | 0.0000 ns |  3.33 |    0.00 |    3 |      - |      32 B |          NA |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 2,423,213.000 ns |        NA | 0.0000 ns |  4.19 |    0.00 |    4 |      - |      32 B |          NA |
