```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-----------:|----------:|----------:|-----------:|------:|--------:|-----:|----------:|------------:|
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     |   5.790 ns | 0.1799 ns | 0.1595 ns |   5.740 ns |  1.00 |    0.04 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     |  13.433 ns | 0.0215 ns | 0.0180 ns |  13.433 ns |  2.32 |    0.06 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 103.098 ns | 0.2211 ns | 0.2069 ns | 103.008 ns | 17.82 |    0.47 |    4 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     | 100.776 ns | 0.3314 ns | 0.3100 ns | 100.631 ns | 17.42 |    0.46 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     |  64.898 ns | 0.0493 ns | 0.0412 ns |  64.910 ns | 11.22 |    0.29 |    3 |         - |          NA |
|                                |            |                |             |             |            |           |           |            |       |         |      |           |             |
| RoundRobin.SelectReplica       | MediumRun  | 15             | 2           | 10          |   5.985 ns | 0.0503 ns | 0.0722 ns |   5.956 ns |  1.00 |    0.02 |    1 |         - |          NA |
| Random.SelectReplica           | MediumRun  | 15             | 2           | 10          |  13.975 ns | 0.0100 ns | 0.0140 ns |  13.968 ns |  2.34 |    0.03 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | MediumRun  | 15             | 2           | 10          | 104.148 ns | 0.7262 ns | 1.0181 ns | 105.015 ns | 17.41 |    0.26 |    5 |         - |          NA |
| LeastConnections.SelectReplica | MediumRun  | 15             | 2           | 10          |  98.372 ns | 0.0440 ns | 0.0616 ns |  98.368 ns | 16.44 |    0.19 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | MediumRun  | 15             | 2           | 10          |  67.347 ns | 0.0687 ns | 0.0963 ns |  67.320 ns | 11.25 |    0.13 |    3 |         - |          NA |
