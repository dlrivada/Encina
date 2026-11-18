```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     |  4.641 ns |  0.0745 ns | 0.0660 ns |  1.00 |    0.02 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     | 11.528 ns |  0.0480 ns | 0.0401 ns |  2.48 |    0.04 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 86.699 ns |  0.3062 ns | 0.2714 ns | 18.69 |    0.27 |    5 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     | 80.569 ns |  0.0626 ns | 0.0488 ns | 17.37 |    0.24 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     | 55.871 ns |  1.1684 ns | 1.4349 ns | 12.04 |    0.35 |    3 |         - |          NA |
|                                |            |                |             |             |           |            |           |       |         |      |           |             |
| RoundRobin.SelectReplica       | ShortRun   | 3              | 1           | 3           |  4.345 ns |  0.4622 ns | 0.0253 ns |  1.00 |    0.01 |    1 |         - |          NA |
| Random.SelectReplica           | ShortRun   | 3              | 1           | 3           | 11.076 ns |  2.5012 ns | 0.1371 ns |  2.55 |    0.03 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | ShortRun   | 3              | 1           | 3           | 82.961 ns |  0.7691 ns | 0.0422 ns | 19.09 |    0.10 |    4 |         - |          NA |
| LeastConnections.SelectReplica | ShortRun   | 3              | 1           | 3           | 81.661 ns | 24.0624 ns | 1.3189 ns | 18.79 |    0.28 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | ShortRun   | 3              | 1           | 3           | 55.300 ns |  8.1412 ns | 0.4462 ns | 12.73 |    0.11 |    3 |         - |          NA |
