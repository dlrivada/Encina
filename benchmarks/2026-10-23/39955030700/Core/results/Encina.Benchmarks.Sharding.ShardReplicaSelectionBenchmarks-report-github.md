```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Median    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     |  5.886 ns |  0.0773 ns | 0.0603 ns |  5.870 ns |  1.00 |    0.01 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     | 11.728 ns |  0.2835 ns | 0.3482 ns | 11.512 ns |  1.99 |    0.06 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 78.069 ns |  1.3569 ns | 1.3934 ns | 77.557 ns | 13.26 |    0.26 |    4 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     | 76.673 ns |  1.5257 ns | 1.6325 ns | 76.016 ns | 13.03 |    0.30 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     | 55.846 ns |  0.7425 ns | 0.6200 ns | 55.641 ns |  9.49 |    0.14 |    3 |         - |          NA |
|                                |            |                |             |             |           |            |           |           |       |         |      |           |             |
| RoundRobin.SelectReplica       | ShortRun   | 3              | 1           | 3           |  6.474 ns |  6.3393 ns | 0.3475 ns |  6.276 ns |  1.00 |    0.06 |    1 |         - |          NA |
| Random.SelectReplica           | ShortRun   | 3              | 1           | 3           | 12.129 ns |  5.4776 ns | 0.3002 ns | 12.129 ns |  1.88 |    0.09 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | ShortRun   | 3              | 1           | 3           | 77.773 ns |  1.3412 ns | 0.0735 ns | 77.812 ns | 12.04 |    0.54 |    4 |         - |          NA |
| LeastConnections.SelectReplica | ShortRun   | 3              | 1           | 3           | 76.233 ns |  1.1459 ns | 0.0628 ns | 76.240 ns | 11.80 |    0.53 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | ShortRun   | 3              | 1           | 3           | 58.632 ns | 36.3771 ns | 1.9940 ns | 57.508 ns |  9.07 |    0.49 |    3 |         - |          NA |
