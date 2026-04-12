```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-----------:|----------:|----------:|-----------:|------:|--------:|-----:|----------:|------------:|
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     |   6.299 ns | 0.0034 ns | 0.0028 ns |   6.299 ns |  1.00 |    0.00 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     |  14.808 ns | 0.0083 ns | 0.0065 ns |  14.809 ns |  2.35 |    0.00 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 106.748 ns | 0.0829 ns | 0.0693 ns | 106.757 ns | 16.95 |    0.01 |    4 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     | 133.603 ns | 1.0815 ns | 1.0116 ns | 132.959 ns | 21.21 |    0.16 |    5 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     |  72.270 ns | 0.7812 ns | 0.6523 ns |  72.211 ns | 11.47 |    0.10 |    3 |         - |          NA |
|                                |            |                |             |             |            |           |           |            |       |         |      |           |             |
| RoundRobin.SelectReplica       | MediumRun  | 15             | 2           | 10          |   6.719 ns | 0.0090 ns | 0.0118 ns |   6.715 ns |  1.00 |    0.00 |    1 |         - |          NA |
| Random.SelectReplica           | MediumRun  | 15             | 2           | 10          |  15.471 ns | 0.0077 ns | 0.0106 ns |  15.471 ns |  2.30 |    0.00 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | MediumRun  | 15             | 2           | 10          | 106.718 ns | 0.1019 ns | 0.1395 ns | 106.767 ns | 15.88 |    0.03 |    5 |         - |          NA |
| LeastConnections.SelectReplica | MediumRun  | 15             | 2           | 10          | 103.778 ns | 0.1251 ns | 0.1713 ns | 103.811 ns | 15.45 |    0.04 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | MediumRun  | 15             | 2           | 10          |  72.603 ns | 0.8380 ns | 1.2542 ns |  71.950 ns | 10.81 |    0.18 |    3 |         - |          NA |
