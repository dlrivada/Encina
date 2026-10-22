```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                                    | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|----------:|----------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;LeastConnections.AcquireReplica (lease)&#39; | DefaultJob | Default        | Default     | Default     | 288.872 ns | 0.6402 ns | 0.5675 ns | 288.993 ns | 33.08 |    0.07 |    4 | 0.0010 |      32 B |          NA |
| LeastConnections.SelectReplica            | DefaultJob | Default        | Default     | Default     | 250.496 ns | 0.3245 ns | 0.3035 ns | 250.464 ns | 28.68 |    0.04 |    3 | 0.0010 |      32 B |          NA |
| Random.SelectReplica                      | DefaultJob | Default        | Default     | Default     |  11.387 ns | 0.0226 ns | 0.0211 ns |  11.391 ns |  1.30 |    0.00 |    2 |      - |         - |          NA |
| RoundRobin.SelectReplica                  | DefaultJob | Default        | Default     | Default     |   8.733 ns | 0.0089 ns | 0.0079 ns |   8.732 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
|                                           |            |                |             |             |            |           |           |            |       |         |      |        |           |             |
| &#39;LeastConnections.AcquireReplica (lease)&#39; | MediumRun  | 15             | 2           | 10          | 277.142 ns | 4.0996 ns | 5.7471 ns | 281.903 ns | 31.32 |    0.64 |    4 | 0.0010 |      32 B |          NA |
| LeastConnections.SelectReplica            | MediumRun  | 15             | 2           | 10          | 238.902 ns | 2.0509 ns | 3.0062 ns | 240.939 ns | 27.00 |    0.34 |    3 | 0.0010 |      32 B |          NA |
| Random.SelectReplica                      | MediumRun  | 15             | 2           | 10          |  11.184 ns | 0.3979 ns | 0.5447 ns |  10.710 ns |  1.26 |    0.06 |    2 |      - |         - |          NA |
| RoundRobin.SelectReplica                  | MediumRun  | 15             | 2           | 10          |   8.848 ns | 0.0189 ns | 0.0277 ns |   8.844 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
