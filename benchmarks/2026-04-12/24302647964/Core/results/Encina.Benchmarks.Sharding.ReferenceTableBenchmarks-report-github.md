```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean             | Error          | StdDev         | Median           | Ratio  | RatioSD | Rank | Gen0    | Allocated | Alloc Ratio |
|------------------------------------------- |----------- |--------------- |------------ |------------ |-----------------:|---------------:|---------------:|-----------------:|-------:|--------:|-----:|--------:|----------:|------------:|
| &#39;Hash 100 rows&#39;                            | DefaultJob | Default        | Default     | Default     |    81,263.092 ns |    136.8568 ns |    128.0160 ns |    81,228.947 ns |  1.000 |    0.00 |    6 |  0.8545 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | DefaultJob | Default        | Default     | Default     |   840,916.819 ns |  1,286.0491 ns |  1,140.0491 ns |   840,634.564 ns | 10.348 |    0.02 |    7 |  7.8125 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 4,578,194.531 ns |  6,433.8158 ns |  5,703.4101 ns | 4,578,442.590 ns | 56.338 |    0.11 |    8 | 39.0625 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | DefaultJob | Default        | Default     | Default     |         8.572 ns |      0.1784 ns |      0.1669 ns |         8.578 ns |  0.000 |    0.00 |    2 |  0.0014 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | DefaultJob | Default        | Default     | Default     |         7.272 ns |      0.0055 ns |      0.0046 ns |         7.272 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | DefaultJob | Default        | Default     | Default     |         9.003 ns |      0.0073 ns |      0.0068 ns |         9.002 ns |  0.000 |    0.00 |    3 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | DefaultJob | Default        | Default     | Default     |       140.540 ns |      0.2266 ns |      0.1892 ns |       140.563 ns |  0.002 |    0.00 |    5 |  0.0067 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | DefaultJob | Default        | Default     | Default     |        16.248 ns |      0.0097 ns |      0.0086 ns |        16.248 ns |  0.000 |    0.00 |    4 |       - |         - |       0.000 |
|                                            |            |                |             |             |                  |                |                |                  |        |         |      |         |           |             |
| &#39;Hash 100 rows&#39;                            | MediumRun  | 15             | 2           | 10          |    83,473.442 ns |  1,504.9569 ns |  2,158.3644 ns |    83,439.566 ns |  1.001 |    0.04 |    6 |  0.8545 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | MediumRun  | 15             | 2           | 10          |   871,153.749 ns |  3,113.9543 ns |  4,564.3942 ns |   868,308.252 ns | 10.443 |    0.27 |    7 |  7.8125 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | MediumRun  | 15             | 2           | 10          | 4,580,038.311 ns | 25,463.5772 ns | 38,112.6866 ns | 4,581,848.594 ns | 54.904 |    1.47 |    8 | 39.0625 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | MediumRun  | 15             | 2           | 10          |         8.254 ns |      0.0537 ns |      0.0804 ns |         8.266 ns |  0.000 |    0.00 |    2 |  0.0014 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | MediumRun  | 15             | 2           | 10          |         7.215 ns |      0.0386 ns |      0.0515 ns |         7.196 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | MediumRun  | 15             | 2           | 10          |         9.007 ns |      0.0044 ns |      0.0060 ns |         9.006 ns |  0.000 |    0.00 |    3 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | MediumRun  | 15             | 2           | 10          |       142.018 ns |      0.6513 ns |      0.9547 ns |       142.117 ns |  0.002 |    0.00 |    5 |  0.0067 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | MediumRun  | 15             | 2           | 10          |        16.247 ns |      0.0166 ns |      0.0238 ns |        16.237 ns |  0.000 |    0.00 |    4 |       - |         - |       0.000 |
