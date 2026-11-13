```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.46GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     |  6.147 ns | 0.0068 ns | 0.0064 ns |  1.00 |    0.00 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     | 11.955 ns | 0.0132 ns | 0.0124 ns |  1.94 |    0.00 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 81.896 ns | 0.1176 ns | 0.1043 ns | 13.32 |    0.02 |    5 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     | 79.955 ns | 0.1354 ns | 0.1200 ns | 13.01 |    0.02 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     | 57.318 ns | 0.0661 ns | 0.0552 ns |  9.32 |    0.01 |    3 |         - |          NA |
|                                |            |                |             |             |           |           |           |       |         |      |           |             |
| RoundRobin.SelectReplica       | ShortRun   | 3              | 1           | 3           |  6.555 ns | 0.2428 ns | 0.0133 ns |  1.00 |    0.00 |    1 |         - |          NA |
| Random.SelectReplica           | ShortRun   | 3              | 1           | 3           | 12.368 ns | 0.5918 ns | 0.0324 ns |  1.89 |    0.01 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | ShortRun   | 3              | 1           | 3           | 84.950 ns | 1.3476 ns | 0.0739 ns | 12.96 |    0.02 |    4 |         - |          NA |
| LeastConnections.SelectReplica | ShortRun   | 3              | 1           | 3           | 84.185 ns | 7.4533 ns | 0.4085 ns | 12.84 |    0.06 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | ShortRun   | 3              | 1           | 3           | 59.575 ns | 0.5443 ns | 0.0298 ns |  9.09 |    0.02 |    3 |         - |          NA |
