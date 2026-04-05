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
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     | 16           | Default     |         5.567 ns | 0.0046 ns | 0.0036 ns |  1.00 |    0.00 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     | 16           | Default     |        13.467 ns | 0.0716 ns | 0.0598 ns |  2.42 |    0.01 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 16           | Default     |       103.108 ns | 0.0997 ns | 0.0832 ns | 18.52 |    0.02 |    5 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     | 16           | Default     |        98.346 ns | 0.2085 ns | 0.1951 ns | 17.67 |    0.04 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     | 16           | Default     |        64.954 ns | 0.1187 ns | 0.1111 ns | 11.67 |    0.02 |    3 |         - |          NA |
|                                |            |                |             |             |              |             |                  |           |           |       |         |      |           |             |
| RoundRobin.SelectReplica       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   551,943.000 ns |        NA | 0.0000 ns |  1.00 |    0.00 |    1 |         - |          NA |
| Random.SelectReplica           | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   657,790.000 ns |        NA | 0.0000 ns |  1.19 |    0.00 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,222,758.000 ns |        NA | 0.0000 ns |  2.22 |    0.00 |    4 |         - |          NA |
| LeastConnections.SelectReplica | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,358,842.000 ns |        NA | 0.0000 ns |  2.46 |    0.00 |    5 |         - |          NA |
| WeightedRandom.SelectReplica   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,118,903.000 ns |        NA | 0.0000 ns |  2.03 |    0.00 |    3 |         - |          NA |
