```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     |  4.923 ns |  0.0620 ns | 0.0518 ns |  1.00 |    0.01 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     | 11.710 ns |  0.2795 ns | 0.2745 ns |  2.38 |    0.06 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 82.754 ns |  0.3532 ns | 0.3304 ns | 16.81 |    0.18 |    5 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     | 80.352 ns |  0.0790 ns | 0.0700 ns | 16.32 |    0.16 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     | 54.230 ns |  0.2876 ns | 0.2401 ns | 11.02 |    0.12 |    3 |         - |          NA |
|                                |            |                |             |             |           |            |           |       |         |      |           |             |
| RoundRobin.SelectReplica       | ShortRun   | 3              | 1           | 3           |  4.599 ns |  3.5352 ns | 0.1938 ns |  1.00 |    0.05 |    1 |         - |          NA |
| Random.SelectReplica           | ShortRun   | 3              | 1           | 3           | 11.073 ns |  4.8139 ns | 0.2639 ns |  2.41 |    0.10 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | ShortRun   | 3              | 1           | 3           | 83.373 ns |  7.5502 ns | 0.4138 ns | 18.15 |    0.67 |    4 |         - |          NA |
| LeastConnections.SelectReplica | ShortRun   | 3              | 1           | 3           | 80.794 ns |  6.2291 ns | 0.3414 ns | 17.59 |    0.65 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | ShortRun   | 3              | 1           | 3           | 52.881 ns | 16.2779 ns | 0.8922 ns | 11.51 |    0.46 |    3 |         - |          NA |
