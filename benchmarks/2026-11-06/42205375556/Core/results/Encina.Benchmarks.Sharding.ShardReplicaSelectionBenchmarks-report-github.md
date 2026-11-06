```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-----------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     |   5.564 ns |  0.0069 ns | 0.0061 ns |  1.00 |    0.00 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     |  13.117 ns |  0.0152 ns | 0.0127 ns |  2.36 |    0.00 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 103.386 ns |  0.1418 ns | 0.1327 ns | 18.58 |    0.03 |    5 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     | 100.316 ns |  0.1484 ns | 0.1388 ns | 18.03 |    0.03 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     |  65.192 ns |  0.1335 ns | 0.1184 ns | 11.72 |    0.02 |    3 |         - |          NA |
|                                |            |                |             |             |            |            |           |       |         |      |           |             |
| RoundRobin.SelectReplica       | ShortRun   | 3              | 1           | 3           |   5.953 ns |  0.0535 ns | 0.0029 ns |  1.00 |    0.00 |    1 |         - |          NA |
| Random.SelectReplica           | ShortRun   | 3              | 1           | 3           |  13.967 ns |  0.1378 ns | 0.0076 ns |  2.35 |    0.00 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | ShortRun   | 3              | 1           | 3           | 103.872 ns | 32.5828 ns | 1.7860 ns | 17.45 |    0.26 |    4 |         - |          NA |
| LeastConnections.SelectReplica | ShortRun   | 3              | 1           | 3           |  98.503 ns |  1.8248 ns | 0.1000 ns | 16.55 |    0.02 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | ShortRun   | 3              | 1           | 3           |  67.619 ns |  4.0270 ns | 0.2207 ns | 11.36 |    0.03 |    3 |         - |          NA |
