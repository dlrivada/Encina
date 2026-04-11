```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     |   9.529 ns | 0.0085 ns | 0.0071 ns |  1.00 |    0.00 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     |  13.634 ns | 0.0126 ns | 0.0105 ns |  1.43 |    0.00 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 100.370 ns | 0.5364 ns | 0.4479 ns | 10.53 |    0.05 |    4 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     |  98.978 ns | 0.1812 ns | 0.1695 ns | 10.39 |    0.02 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     |  67.563 ns | 0.0387 ns | 0.0323 ns |  7.09 |    0.01 |    3 |         - |          NA |
|                                |            |                |             |             |            |           |           |       |         |      |           |             |
| RoundRobin.SelectReplica       | MediumRun  | 15             | 2           | 10          |   9.458 ns | 0.0043 ns | 0.0060 ns |  1.00 |    0.00 |    1 |         - |          NA |
| Random.SelectReplica           | MediumRun  | 15             | 2           | 10          |  14.029 ns | 0.0269 ns | 0.0402 ns |  1.48 |    0.00 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | MediumRun  | 15             | 2           | 10          |  99.753 ns | 0.0954 ns | 0.1306 ns | 10.55 |    0.02 |    4 |         - |          NA |
| LeastConnections.SelectReplica | MediumRun  | 15             | 2           | 10          |  99.140 ns | 0.0950 ns | 0.1422 ns | 10.48 |    0.02 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | MediumRun  | 15             | 2           | 10          |  65.090 ns | 0.1051 ns | 0.1507 ns |  6.88 |    0.02 |    3 |         - |          NA |
