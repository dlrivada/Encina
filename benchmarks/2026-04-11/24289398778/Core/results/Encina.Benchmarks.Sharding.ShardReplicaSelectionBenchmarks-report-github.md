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
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     |   5.570 ns | 0.0087 ns | 0.0081 ns |  1.00 |    0.00 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     |  13.457 ns | 0.0158 ns | 0.0148 ns |  2.42 |    0.00 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 103.102 ns | 0.0923 ns | 0.0771 ns | 18.51 |    0.03 |    5 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     |  98.351 ns | 0.0462 ns | 0.0361 ns | 17.66 |    0.03 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     |  64.969 ns | 0.0839 ns | 0.0744 ns | 11.67 |    0.02 |    3 |         - |          NA |
|                                |            |                |             |             |            |           |           |       |         |      |           |             |
| RoundRobin.SelectReplica       | MediumRun  | 15             | 2           | 10          |   5.971 ns | 0.0056 ns | 0.0077 ns |  1.00 |    0.00 |    1 |         - |          NA |
| Random.SelectReplica           | MediumRun  | 15             | 2           | 10          |  13.994 ns | 0.0085 ns | 0.0123 ns |  2.34 |    0.00 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | MediumRun  | 15             | 2           | 10          | 103.177 ns | 0.1532 ns | 0.2147 ns | 17.28 |    0.04 |    5 |         - |          NA |
| LeastConnections.SelectReplica | MediumRun  | 15             | 2           | 10          |  98.503 ns | 0.0493 ns | 0.0707 ns | 16.50 |    0.02 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | MediumRun  | 15             | 2           | 10          |  67.324 ns | 0.0416 ns | 0.0583 ns | 11.28 |    0.02 |    3 |         - |          NA |
