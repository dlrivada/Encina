```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     |   5.582 ns | 0.0385 ns | 0.0322 ns |  1.00 |    0.01 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     |  13.457 ns | 0.0345 ns | 0.0288 ns |  2.41 |    0.01 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 104.529 ns | 1.5858 ns | 1.4834 ns | 18.73 |    0.28 |    5 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     |  98.813 ns | 0.1670 ns | 0.1481 ns | 17.70 |    0.10 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     |  65.641 ns | 0.1026 ns | 0.0960 ns | 11.76 |    0.07 |    3 |         - |          NA |
|                                |            |                |             |             |            |           |           |       |         |      |           |             |
| RoundRobin.SelectReplica       | ShortRun   | 3              | 1           | 3           |   5.964 ns | 0.2533 ns | 0.0139 ns |  1.00 |    0.00 |    1 |         - |          NA |
| Random.SelectReplica           | ShortRun   | 3              | 1           | 3           |  14.315 ns | 8.4718 ns | 0.4644 ns |  2.40 |    0.07 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | ShortRun   | 3              | 1           | 3           | 103.070 ns | 3.0639 ns | 0.1679 ns | 17.28 |    0.04 |    4 |         - |          NA |
| LeastConnections.SelectReplica | ShortRun   | 3              | 1           | 3           |  98.330 ns | 2.0094 ns | 0.1101 ns | 16.49 |    0.04 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | ShortRun   | 3              | 1           | 3           |  67.509 ns | 2.9180 ns | 0.1599 ns | 11.32 |    0.03 |    3 |         - |          NA |
