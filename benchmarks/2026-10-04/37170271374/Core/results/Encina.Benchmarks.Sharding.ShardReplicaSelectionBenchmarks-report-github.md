```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-----------:|----------:|----------:|-----------:|------:|--------:|-----:|----------:|------------:|
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     |   5.570 ns | 0.0170 ns | 0.0142 ns |   5.565 ns |  1.00 |    0.00 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     |  13.441 ns | 0.0113 ns | 0.0088 ns |  13.439 ns |  2.41 |    0.01 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 103.001 ns | 0.1949 ns | 0.1627 ns | 102.960 ns | 18.49 |    0.05 |    5 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     |  98.316 ns | 0.1101 ns | 0.0920 ns |  98.309 ns | 17.65 |    0.05 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     |  64.777 ns | 0.0676 ns | 0.0599 ns |  64.774 ns | 11.63 |    0.03 |    3 |         - |          NA |
|                                |            |                |             |             |            |           |           |            |       |         |      |           |             |
| RoundRobin.SelectReplica       | MediumRun  | 15             | 2           | 10          |   5.960 ns | 0.0036 ns | 0.0051 ns |   5.960 ns |  1.00 |    0.00 |    1 |         - |          NA |
| Random.SelectReplica           | MediumRun  | 15             | 2           | 10          |  13.978 ns | 0.0088 ns | 0.0123 ns |  13.976 ns |  2.35 |    0.00 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | MediumRun  | 15             | 2           | 10          | 104.435 ns | 1.2033 ns | 1.6064 ns | 103.056 ns | 17.52 |    0.26 |    5 |         - |          NA |
| LeastConnections.SelectReplica | MediumRun  | 15             | 2           | 10          |  98.716 ns | 0.2431 ns | 0.3486 ns |  98.580 ns | 16.56 |    0.06 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | MediumRun  | 15             | 2           | 10          |  70.134 ns | 0.2389 ns | 0.3349 ns |  70.004 ns | 11.77 |    0.06 |    3 |         - |          NA |
