```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.37GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     |  5.751 ns |  0.1378 ns | 0.1289 ns |  1.00 |    0.03 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     |  8.266 ns |  0.2044 ns | 0.2272 ns |  1.44 |    0.05 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 58.125 ns |  0.7489 ns | 0.6638 ns | 10.11 |    0.25 |    4 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     | 55.922 ns |  0.6816 ns | 0.5692 ns |  9.73 |    0.23 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     | 43.109 ns |  0.8581 ns | 0.8812 ns |  7.50 |    0.22 |    3 |         - |          NA |
|                                |            |                |             |             |           |            |           |       |         |      |           |             |
| RoundRobin.SelectReplica       | ShortRun   | 3              | 1           | 3           |  5.606 ns |  2.0414 ns | 0.1119 ns |  1.00 |    0.02 |    1 |         - |          NA |
| Random.SelectReplica           | ShortRun   | 3              | 1           | 3           |  8.006 ns |  4.1372 ns | 0.2268 ns |  1.43 |    0.04 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | ShortRun   | 3              | 1           | 3           | 57.433 ns | 21.0199 ns | 1.1522 ns | 10.25 |    0.25 |    4 |         - |          NA |
| LeastConnections.SelectReplica | ShortRun   | 3              | 1           | 3           | 54.726 ns |  1.7406 ns | 0.0954 ns |  9.76 |    0.17 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | ShortRun   | 3              | 1           | 3           | 42.469 ns | 18.9565 ns | 1.0391 ns |  7.58 |    0.21 |    3 |         - |          NA |
