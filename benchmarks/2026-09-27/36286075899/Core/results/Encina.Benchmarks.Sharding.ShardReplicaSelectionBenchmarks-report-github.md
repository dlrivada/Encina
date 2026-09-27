```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     |  4.769 ns | 0.1511 ns | 0.1617 ns |  4.765 ns |  1.00 |    0.05 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     | 11.565 ns | 0.1208 ns | 0.1130 ns | 11.513 ns |  2.43 |    0.08 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 82.920 ns | 0.0557 ns | 0.0465 ns | 82.916 ns | 17.41 |    0.57 |    4 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     | 81.612 ns | 0.0906 ns | 0.0803 ns | 81.611 ns | 17.13 |    0.56 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     | 54.598 ns | 1.0783 ns | 1.0591 ns | 54.285 ns | 11.46 |    0.43 |    3 |         - |          NA |
|                                |            |                |             |             |           |           |           |           |       |         |      |           |             |
| RoundRobin.SelectReplica       | MediumRun  | 15             | 2           | 10          |  4.092 ns | 0.1120 ns | 0.1677 ns |  4.079 ns |  1.00 |    0.06 |    1 |         - |          NA |
| Random.SelectReplica           | MediumRun  | 15             | 2           | 10          | 11.342 ns | 0.3250 ns | 0.4763 ns | 11.628 ns |  2.78 |    0.16 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | MediumRun  | 15             | 2           | 10          | 86.494 ns | 2.4669 ns | 3.5379 ns | 89.618 ns | 21.17 |    1.21 |    4 |         - |          NA |
| LeastConnections.SelectReplica | MediumRun  | 15             | 2           | 10          | 81.193 ns | 0.5153 ns | 0.7390 ns | 81.207 ns | 19.88 |    0.83 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | MediumRun  | 15             | 2           | 10          | 52.440 ns | 0.7001 ns | 1.0479 ns | 52.184 ns | 12.84 |    0.58 |    3 |         - |          NA |
