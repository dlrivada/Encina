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
| RoundRobin.SelectReplica                  | DefaultJob | Default        | Default     | Default     | 16           | Default     |         4.057 ns | 0.1364 ns | 0.1276 ns |  1.00 |    0.04 |    1 |      - |         - |          NA |
| Random.SelectReplica                      | DefaultJob | Default        | Default     | Default     | 16           | Default     |        11.183 ns | 0.0076 ns | 0.0071 ns |  2.76 |    0.09 |    2 |      - |         - |          NA |
| LeastConnections.SelectReplica            | DefaultJob | Default        | Default     | Default     | 16           | Default     |       245.119 ns | 0.3721 ns | 0.3107 ns | 60.48 |    1.87 |    3 | 0.0019 |      32 B |          NA |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | DefaultJob | Default        | Default     | Default     | 16           | Default     |       294.224 ns | 0.5592 ns | 0.4670 ns | 72.59 |    2.24 |    4 | 0.0019 |      32 B |          NA |
|                                           |            |                |             |             |              |             |                  |           |           |       |         |      |        |           |             |
| RoundRobin.SelectReplica                  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   520,453.000 ns |        NA | 0.0000 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Random.SelectReplica                      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   620,509.000 ns |        NA | 0.0000 ns |  1.19 |    0.00 |    2 |      - |         - |          NA |
| LeastConnections.SelectReplica            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,875,496.000 ns |        NA | 0.0000 ns |  3.60 |    0.00 |    3 |      - |      32 B |          NA |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 2,247,624.000 ns |        NA | 0.0000 ns |  4.32 |    0.00 |    4 |      - |      32 B |          NA |
