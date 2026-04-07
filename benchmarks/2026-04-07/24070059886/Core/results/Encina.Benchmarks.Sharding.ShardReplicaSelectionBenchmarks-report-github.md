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
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     | 16           | Default     |         5.570 ns | 0.0086 ns | 0.0067 ns |  1.00 |    0.00 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     | 16           | Default     |        13.438 ns | 0.0083 ns | 0.0069 ns |  2.41 |    0.00 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 16           | Default     |       103.750 ns | 0.0627 ns | 0.0490 ns | 18.63 |    0.02 |    5 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     | 16           | Default     |        98.340 ns | 0.0506 ns | 0.0422 ns | 17.66 |    0.02 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     | 16           | Default     |        64.934 ns | 0.0437 ns | 0.0365 ns | 11.66 |    0.01 |    3 |         - |          NA |
|                                |            |                |             |             |              |             |                  |           |           |       |         |      |           |             |
| RoundRobin.SelectReplica       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   525,067.000 ns |        NA | 0.0000 ns |  1.00 |    0.00 |    1 |         - |          NA |
| Random.SelectReplica           | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   668,824.000 ns |        NA | 0.0000 ns |  1.27 |    0.00 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,297,935.000 ns |        NA | 0.0000 ns |  2.47 |    0.00 |    4 |         - |          NA |
| LeastConnections.SelectReplica | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,508,155.000 ns |        NA | 0.0000 ns |  2.87 |    0.00 |    5 |         - |          NA |
| WeightedRandom.SelectReplica   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,126,936.000 ns |        NA | 0.0000 ns |  2.15 |    0.00 |    3 |         - |          NA |
