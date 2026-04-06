```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                                    | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean             | Error     | StdDev    | Median           | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |--------------- |------------ |------------ |------------- |------------ |-----------------:|----------:|----------:|-----------------:|------:|--------:|-----:|-------:|----------:|------------:|
| RoundRobin.SelectReplica                  | DefaultJob | Default        | Default     | Default     | 16           | Default     |         4.039 ns | 0.1359 ns | 0.1271 ns |         4.147 ns |  1.00 |    0.04 |    1 |      - |         - |          NA |
| Random.SelectReplica                      | DefaultJob | Default        | Default     | Default     | 16           | Default     |        11.184 ns | 0.0037 ns | 0.0031 ns |        11.184 ns |  2.77 |    0.08 |    2 |      - |         - |          NA |
| LeastConnections.SelectReplica            | DefaultJob | Default        | Default     | Default     | 16           | Default     |       238.208 ns | 0.7786 ns | 0.6902 ns |       238.248 ns | 59.04 |    1.81 |    3 | 0.0019 |      32 B |          NA |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | DefaultJob | Default        | Default     | Default     | 16           | Default     |       284.065 ns | 0.9672 ns | 0.8077 ns |       284.196 ns | 70.40 |    2.16 |    4 | 0.0019 |      32 B |          NA |
|                                           |            |                |             |             |              |             |                  |           |           |                  |       |         |      |        |           |             |
| RoundRobin.SelectReplica                  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   562,418.000 ns |        NA | 0.0000 ns |   562,418.000 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Random.SelectReplica                      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   607,481.000 ns |        NA | 0.0000 ns |   607,481.000 ns |  1.08 |    0.00 |    2 |      - |         - |          NA |
| LeastConnections.SelectReplica            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,821,571.000 ns |        NA | 0.0000 ns | 1,821,571.000 ns |  3.24 |    0.00 |    3 |      - |      32 B |          NA |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 2,293,029.000 ns |        NA | 0.0000 ns | 2,293,029.000 ns |  4.08 |    0.00 |    4 |      - |      32 B |          NA |
