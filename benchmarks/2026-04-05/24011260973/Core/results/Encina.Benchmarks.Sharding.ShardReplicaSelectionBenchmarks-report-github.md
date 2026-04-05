```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                         | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean             | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |-----------------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     | 16           | Default     |         5.623 ns | 0.0482 ns | 0.0427 ns |  1.00 |    0.01 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     | 16           | Default     |        13.442 ns | 0.0225 ns | 0.0200 ns |  2.39 |    0.02 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 16           | Default     |       103.100 ns | 0.1862 ns | 0.1742 ns | 18.33 |    0.14 |    5 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     | 16           | Default     |        98.567 ns | 0.2022 ns | 0.1892 ns | 17.53 |    0.13 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     | 16           | Default     |        64.607 ns | 0.1043 ns | 0.0925 ns | 11.49 |    0.09 |    3 |         - |          NA |
|                                |            |                |             |             |              |             |                  |           |           |       |         |      |           |             |
| RoundRobin.SelectReplica       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   521,688.000 ns |        NA | 0.0000 ns |  1.00 |    0.00 |    1 |         - |          NA |
| Random.SelectReplica           | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   660,355.000 ns |        NA | 0.0000 ns |  1.27 |    0.00 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,241,693.000 ns |        NA | 0.0000 ns |  2.38 |    0.00 |    4 |         - |          NA |
| LeastConnections.SelectReplica | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,384,077.000 ns |        NA | 0.0000 ns |  2.65 |    0.00 |    5 |         - |          NA |
| WeightedRandom.SelectReplica   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,093,398.000 ns |        NA | 0.0000 ns |  2.10 |    0.00 |    3 |         - |          NA |
