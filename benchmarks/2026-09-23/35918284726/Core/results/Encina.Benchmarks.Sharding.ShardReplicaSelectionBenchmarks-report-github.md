```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-----------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     |   5.567 ns |  0.0062 ns | 0.0055 ns |  1.00 |    0.00 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     |  13.425 ns |  0.0142 ns | 0.0126 ns |  2.41 |    0.00 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 105.944 ns |  0.1420 ns | 0.1186 ns | 19.03 |    0.03 |    5 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     |  98.318 ns |  0.1502 ns | 0.1331 ns | 17.66 |    0.03 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     |  64.775 ns |  0.0689 ns | 0.0575 ns | 11.64 |    0.01 |    3 |         - |          NA |
|                                |            |                |             |             |            |            |           |       |         |      |           |             |
| RoundRobin.SelectReplica       | ShortRun   | 3              | 1           | 3           |   5.965 ns |  0.1282 ns | 0.0070 ns |  1.00 |    0.00 |    1 |         - |          NA |
| Random.SelectReplica           | ShortRun   | 3              | 1           | 3           |  13.976 ns |  0.2215 ns | 0.0121 ns |  2.34 |    0.00 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | ShortRun   | 3              | 1           | 3           | 103.054 ns |  0.4838 ns | 0.0265 ns | 17.28 |    0.02 |    4 |         - |          NA |
| LeastConnections.SelectReplica | ShortRun   | 3              | 1           | 3           |  99.115 ns | 20.1731 ns | 1.1058 ns | 16.62 |    0.16 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | ShortRun   | 3              | 1           | 3           |  70.267 ns |  4.9992 ns | 0.2740 ns | 11.78 |    0.04 |    3 |         - |          NA |
