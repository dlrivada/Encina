```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     |   5.574 ns | 0.0064 ns | 0.0057 ns |  1.00 |    0.00 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     |  13.442 ns | 0.0166 ns | 0.0138 ns |  2.41 |    0.00 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 113.063 ns | 0.0934 ns | 0.0780 ns | 20.28 |    0.02 |    5 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     |  98.745 ns | 0.2747 ns | 0.2294 ns | 17.71 |    0.04 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     |  65.102 ns | 0.0720 ns | 0.0601 ns | 11.68 |    0.02 |    3 |         - |          NA |
|                                |            |                |             |             |            |           |           |       |         |      |           |             |
| RoundRobin.SelectReplica       | MediumRun  | 15             | 2           | 10          |   5.961 ns | 0.0045 ns | 0.0063 ns |  1.00 |    0.00 |    1 |         - |          NA |
| Random.SelectReplica           | MediumRun  | 15             | 2           | 10          |  13.986 ns | 0.0092 ns | 0.0132 ns |  2.35 |    0.00 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | MediumRun  | 15             | 2           | 10          | 103.084 ns | 0.1259 ns | 0.1845 ns | 17.29 |    0.04 |    5 |         - |          NA |
| LeastConnections.SelectReplica | MediumRun  | 15             | 2           | 10          |  98.458 ns | 0.0474 ns | 0.0633 ns | 16.52 |    0.02 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | MediumRun  | 15             | 2           | 10          |  67.313 ns | 0.0413 ns | 0.0593 ns | 11.29 |    0.02 |    3 |         - |          NA |
