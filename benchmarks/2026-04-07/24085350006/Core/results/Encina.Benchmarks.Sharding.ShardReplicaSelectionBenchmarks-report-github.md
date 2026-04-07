```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-----------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     |   5.571 ns |  0.0053 ns | 0.0047 ns |  1.00 |    0.00 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     |  13.434 ns |  0.0117 ns | 0.0104 ns |  2.41 |    0.00 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 104.606 ns |  0.0809 ns | 0.0676 ns | 18.78 |    0.02 |    5 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     |  98.508 ns |  0.1016 ns | 0.0849 ns | 17.68 |    0.02 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     |  64.984 ns |  0.0597 ns | 0.0499 ns | 11.66 |    0.01 |    3 |         - |          NA |
|                                |            |                |             |             |            |            |           |       |         |      |           |             |
| RoundRobin.SelectReplica       | ShortRun   | 3              | 1           | 3           |   5.980 ns |  0.5240 ns | 0.0287 ns |  1.00 |    0.01 |    1 |         - |          NA |
| Random.SelectReplica           | ShortRun   | 3              | 1           | 3           |  13.971 ns |  0.1906 ns | 0.0104 ns |  2.34 |    0.01 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | ShortRun   | 3              | 1           | 3           | 103.876 ns | 11.4939 ns | 0.6300 ns | 17.37 |    0.12 |    4 |         - |          NA |
| LeastConnections.SelectReplica | ShortRun   | 3              | 1           | 3           |  98.559 ns |  2.4167 ns | 0.1325 ns | 16.48 |    0.07 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | ShortRun   | 3              | 1           | 3           |  67.250 ns |  1.2714 ns | 0.0697 ns | 11.25 |    0.05 |    3 |         - |          NA |
