```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.74GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]     : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-----------:|----------:|----------:|-----------:|------:|--------:|-----:|----------:|------------:|
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     |   5.570 ns | 0.0041 ns | 0.0034 ns |   5.570 ns |  1.00 |    0.00 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     |  13.440 ns | 0.0155 ns | 0.0137 ns |  13.439 ns |  2.41 |    0.00 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 103.455 ns | 0.0878 ns | 0.0779 ns | 103.431 ns | 18.57 |    0.02 |    5 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     | 101.129 ns | 0.0618 ns | 0.0578 ns | 101.133 ns | 18.16 |    0.01 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     |  64.931 ns | 0.1000 ns | 0.0886 ns |  64.911 ns | 11.66 |    0.02 |    3 |         - |          NA |
|                                |            |                |             |             |            |           |           |            |       |         |      |           |             |
| RoundRobin.SelectReplica       | MediumRun  | 15             | 2           | 10          |   5.799 ns | 0.1184 ns | 0.1580 ns |   5.669 ns |  1.00 |    0.04 |    1 |         - |          NA |
| Random.SelectReplica           | MediumRun  | 15             | 2           | 10          |  13.992 ns | 0.0184 ns | 0.0239 ns |  13.991 ns |  2.41 |    0.06 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | MediumRun  | 15             | 2           | 10          | 103.506 ns | 0.3976 ns | 0.5703 ns | 103.133 ns | 17.86 |    0.49 |    5 |         - |          NA |
| LeastConnections.SelectReplica | MediumRun  | 15             | 2           | 10          |  98.481 ns | 0.0775 ns | 0.1086 ns |  98.470 ns | 16.99 |    0.45 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | MediumRun  | 15             | 2           | 10          |  67.486 ns | 0.0618 ns | 0.0887 ns |  67.451 ns | 11.65 |    0.31 |    3 |         - |          NA |
