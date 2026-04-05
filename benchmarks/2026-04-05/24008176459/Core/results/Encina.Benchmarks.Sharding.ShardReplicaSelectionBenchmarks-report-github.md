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
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     | 16           | Default     |         5.565 ns | 0.0053 ns | 0.0044 ns |  1.00 |    0.00 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     | 16           | Default     |        13.439 ns | 0.0107 ns | 0.0090 ns |  2.41 |    0.00 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 16           | Default     |       102.882 ns | 0.0771 ns | 0.0721 ns | 18.49 |    0.02 |    5 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     | 16           | Default     |        98.391 ns | 0.2036 ns | 0.1805 ns | 17.68 |    0.03 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     | 16           | Default     |        64.908 ns | 0.0525 ns | 0.0410 ns | 11.66 |    0.01 |    3 |         - |          NA |
|                                |            |                |             |             |              |             |                  |           |           |       |         |      |           |             |
| RoundRobin.SelectReplica       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   583,316.000 ns |        NA | 0.0000 ns |  1.00 |    0.00 |    1 |         - |          NA |
| Random.SelectReplica           | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   681,791.000 ns |        NA | 0.0000 ns |  1.17 |    0.00 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,524,734.000 ns |        NA | 0.0000 ns |  2.61 |    0.00 |    5 |         - |          NA |
| LeastConnections.SelectReplica | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,396,233.000 ns |        NA | 0.0000 ns |  2.39 |    0.00 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,127,028.000 ns |        NA | 0.0000 ns |  1.93 |    0.00 |    3 |         - |          NA |
