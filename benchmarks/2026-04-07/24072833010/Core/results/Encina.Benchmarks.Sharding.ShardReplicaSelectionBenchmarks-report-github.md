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
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     | 16           | Default     |         5.599 ns | 0.0569 ns | 0.0504 ns |  1.00 |    0.01 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     | 16           | Default     |        13.440 ns | 0.0199 ns | 0.0176 ns |  2.40 |    0.02 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 16           | Default     |       103.045 ns | 0.1151 ns | 0.0962 ns | 18.41 |    0.16 |    4 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     | 16           | Default     |       102.421 ns | 0.0834 ns | 0.0740 ns | 18.30 |    0.16 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     | 16           | Default     |        68.084 ns | 0.0743 ns | 0.0695 ns | 12.16 |    0.11 |    3 |         - |          NA |
|                                |            |                |             |             |              |             |                  |           |           |       |         |      |           |             |
| RoundRobin.SelectReplica       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   546,442.000 ns |        NA | 0.0000 ns |  1.00 |    0.00 |    1 |         - |          NA |
| Random.SelectReplica           | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   646,299.000 ns |        NA | 0.0000 ns |  1.18 |    0.00 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,231,296.000 ns |        NA | 0.0000 ns |  2.25 |    0.00 |    4 |         - |          NA |
| LeastConnections.SelectReplica | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,404,880.000 ns |        NA | 0.0000 ns |  2.57 |    0.00 |    5 |         - |          NA |
| WeightedRandom.SelectReplica   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,136,868.000 ns |        NA | 0.0000 ns |  2.08 |    0.00 |    3 |         - |          NA |
