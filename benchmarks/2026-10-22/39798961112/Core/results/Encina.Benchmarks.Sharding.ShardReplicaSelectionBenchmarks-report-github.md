```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-----------:|----------:|----------:|-----------:|------:|--------:|-----:|----------:|------------:|
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     |   5.647 ns | 0.0409 ns | 0.0363 ns |   5.649 ns |  1.00 |    0.01 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     |  13.454 ns | 0.0140 ns | 0.0125 ns |  13.453 ns |  2.38 |    0.01 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 103.267 ns | 0.1611 ns | 0.1428 ns | 103.250 ns | 18.29 |    0.12 |    5 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     |  98.614 ns | 0.4201 ns | 0.3508 ns |  98.592 ns | 17.46 |    0.12 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     |  65.232 ns | 0.0950 ns | 0.0842 ns |  65.217 ns | 11.55 |    0.07 |    3 |         - |          NA |
|                                |            |                |             |             |            |           |           |            |       |         |      |           |             |
| RoundRobin.SelectReplica       | MediumRun  | 15             | 2           | 10          |   6.041 ns | 0.0373 ns | 0.0546 ns |   6.028 ns |  1.00 |    0.01 |    1 |         - |          NA |
| Random.SelectReplica           | MediumRun  | 15             | 2           | 10          |  14.747 ns | 0.5095 ns | 0.7308 ns |  14.124 ns |  2.44 |    0.12 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | MediumRun  | 15             | 2           | 10          | 105.366 ns | 0.7130 ns | 1.0672 ns | 105.340 ns | 17.44 |    0.23 |    4 |         - |          NA |
| LeastConnections.SelectReplica | MediumRun  | 15             | 2           | 10          | 102.412 ns | 0.7102 ns | 1.0630 ns | 102.617 ns | 16.95 |    0.23 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | MediumRun  | 15             | 2           | 10          |  67.428 ns | 0.1653 ns | 0.2370 ns |  67.437 ns | 11.16 |    0.11 |    3 |         - |          NA |
