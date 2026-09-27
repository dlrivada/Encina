```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error      | StdDev    | Median    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |----------:|-----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     |  5.779 ns |  0.1216 ns | 0.1078 ns |  5.723 ns |  1.00 |    0.03 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     | 11.606 ns |  0.2779 ns | 0.5549 ns | 11.337 ns |  2.01 |    0.10 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 77.643 ns |  0.6152 ns | 0.5137 ns | 77.497 ns | 13.44 |    0.25 |    4 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     | 76.949 ns |  1.5284 ns | 1.8770 ns | 76.192 ns | 13.32 |    0.40 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     | 54.055 ns |  1.0732 ns | 1.1483 ns | 53.405 ns |  9.36 |    0.25 |    3 |         - |          NA |
|                                |            |                |             |             |           |            |           |           |       |         |      |           |             |
| RoundRobin.SelectReplica       | ShortRun   | 3              | 1           | 3           |  6.067 ns |  0.0833 ns | 0.0046 ns |  6.068 ns |  1.00 |    0.00 |    1 |         - |          NA |
| Random.SelectReplica           | ShortRun   | 3              | 1           | 3           | 11.659 ns |  5.8924 ns | 0.3230 ns | 11.574 ns |  1.92 |    0.05 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | ShortRun   | 3              | 1           | 3           | 82.834 ns | 48.2162 ns | 2.6429 ns | 84.187 ns | 13.65 |    0.38 |    4 |         - |          NA |
| LeastConnections.SelectReplica | ShortRun   | 3              | 1           | 3           | 81.130 ns | 49.8836 ns | 2.7343 ns | 80.735 ns | 13.37 |    0.39 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | ShortRun   | 3              | 1           | 3           | 55.306 ns |  0.5141 ns | 0.0282 ns | 55.318 ns |  9.12 |    0.01 |    3 |         - |          NA |
