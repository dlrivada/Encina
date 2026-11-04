```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.96GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                                    | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |--------------- |------------ |------------ |----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
| &#39;LeastConnections.AcquireReplica (lease)&#39; | DefaultJob | Default        | Default     | Default     | 277.01 ns |  2.069 ns | 1.936 ns | 24.58 |    0.33 |    4 |      32 B |          NA |
| LeastConnections.SelectReplica            | DefaultJob | Default        | Default     | Default     | 243.40 ns |  0.435 ns | 0.406 ns | 21.60 |    0.26 |    3 |      32 B |          NA |
| Random.SelectReplica                      | DefaultJob | Default        | Default     | Default     |  12.22 ns |  0.064 ns | 0.059 ns |  1.08 |    0.01 |    2 |         - |          NA |
| RoundRobin.SelectReplica                  | DefaultJob | Default        | Default     | Default     |  11.27 ns |  0.147 ns | 0.137 ns |  1.00 |    0.02 |    1 |         - |          NA |
|                                           |            |                |             |             |           |           |          |       |         |      |           |             |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | ShortRun   | 3              | 1           | 3           | 276.81 ns | 19.716 ns | 1.081 ns | 26.75 |    0.57 |    3 |      32 B |          NA |
| LeastConnections.SelectReplica            | ShortRun   | 3              | 1           | 3           | 240.22 ns | 59.816 ns | 3.279 ns | 23.22 |    0.56 |    3 |      32 B |          NA |
| Random.SelectReplica                      | ShortRun   | 3              | 1           | 3           |  12.52 ns |  0.566 ns | 0.031 ns |  1.21 |    0.03 |    2 |         - |          NA |
| RoundRobin.SelectReplica                  | ShortRun   | 3              | 1           | 3           |  10.35 ns |  4.688 ns | 0.257 ns |  1.00 |    0.03 |    1 |         - |          NA |
