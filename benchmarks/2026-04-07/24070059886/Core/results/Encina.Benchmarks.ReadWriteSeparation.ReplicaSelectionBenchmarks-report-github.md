```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method                                    | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean             | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |--------------- |------------ |------------ |------------- |------------ |-----------------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;LeastConnections.AcquireReplica (lease)&#39; | DefaultJob | Default        | Default     | Default     | 16           | Default     |       281.916 ns | 0.6714 ns | 0.6280 ns | 30.71 |    0.07 |    4 | 0.0010 |      32 B |          NA |
| LeastConnections.SelectReplica            | DefaultJob | Default        | Default     | Default     | 16           | Default     |       244.650 ns | 1.0777 ns | 0.9554 ns | 26.65 |    0.10 |    3 | 0.0010 |      32 B |          NA |
| Random.SelectReplica                      | DefaultJob | Default        | Default     | Default     | 16           | Default     |        11.407 ns | 0.0140 ns | 0.0117 ns |  1.24 |    0.00 |    2 |      - |         - |          NA |
| RoundRobin.SelectReplica                  | DefaultJob | Default        | Default     | Default     | 16           | Default     |         9.181 ns | 0.0039 ns | 0.0035 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
|                                           |            |                |             |             |              |             |                  |           |           |       |         |      |        |           |             |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 2,283,934.000 ns |        NA | 0.0000 ns |  4.39 |    0.00 |    4 |      - |      32 B |          NA |
| LeastConnections.SelectReplica            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,910,063.000 ns |        NA | 0.0000 ns |  3.67 |    0.00 |    3 |      - |      32 B |          NA |
| Random.SelectReplica                      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   637,419.000 ns |        NA | 0.0000 ns |  1.22 |    0.00 |    2 |      - |         - |          NA |
| RoundRobin.SelectReplica                  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   520,549.000 ns |        NA | 0.0000 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
