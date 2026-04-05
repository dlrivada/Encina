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
| RoundRobin.SelectReplica                  | DefaultJob | Default        | Default     | Default     | 16           | Default     |         4.084 ns | 0.1233 ns | 0.1153 ns |  1.00 |    0.04 |    1 |      - |         - |          NA |
| Random.SelectReplica                      | DefaultJob | Default        | Default     | Default     | 16           | Default     |        11.191 ns | 0.0146 ns | 0.0122 ns |  2.74 |    0.08 |    2 |      - |         - |          NA |
| LeastConnections.SelectReplica            | DefaultJob | Default        | Default     | Default     | 16           | Default     |       238.182 ns | 0.2747 ns | 0.2435 ns | 58.37 |    1.64 |    3 | 0.0019 |      32 B |          NA |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | DefaultJob | Default        | Default     | Default     | 16           | Default     |       285.274 ns | 0.5769 ns | 0.5396 ns | 69.91 |    1.97 |    4 | 0.0019 |      32 B |          NA |
|                                           |            |                |             |             |              |             |                  |           |           |       |         |      |        |           |             |
| RoundRobin.SelectReplica                  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   555,921.000 ns |        NA | 0.0000 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Random.SelectReplica                      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   620,110.000 ns |        NA | 0.0000 ns |  1.12 |    0.00 |    2 |      - |         - |          NA |
| LeastConnections.SelectReplica            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,791,172.000 ns |        NA | 0.0000 ns |  3.22 |    0.00 |    3 |      - |      32 B |          NA |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 2,224,476.000 ns |        NA | 0.0000 ns |  4.00 |    0.00 |    4 |      - |      32 B |          NA |
