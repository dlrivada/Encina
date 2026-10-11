```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean             | Error         | StdDev         | Median           | Ratio  | RatioSD | Rank | Gen0    | Allocated | Alloc Ratio |
|------------------------------------------- |----------- |--------------- |------------ |------------ |-----------------:|--------------:|---------------:|-----------------:|-------:|--------:|-----:|--------:|----------:|------------:|
| &#39;Hash 100 rows&#39;                            | DefaultJob | Default        | Default     | Default     |    78,205.551 ns |    49.3456 ns |     41.2058 ns |    78,214.530 ns |  1.000 |    0.00 |    5 |  0.8545 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | DefaultJob | Default        | Default     | Default     |   823,564.279 ns |   822.8392 ns |    687.1080 ns |   823,423.062 ns | 10.531 |    0.01 |    6 |  7.8125 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 4,579,095.231 ns | 9,446.7380 ns |  8,836.4846 ns | 4,580,230.836 ns | 58.552 |    0.11 |    7 | 39.0625 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | DefaultJob | Default        | Default     | Default     |         8.726 ns |     0.1735 ns |      0.1623 ns |         8.735 ns |  0.000 |    0.00 |    2 |  0.0014 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | DefaultJob | Default        | Default     | Default     |         7.090 ns |     0.0059 ns |      0.0052 ns |         7.089 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | DefaultJob | Default        | Default     | Default     |         8.894 ns |     0.0063 ns |      0.0049 ns |         8.893 ns |  0.000 |    0.00 |    2 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | DefaultJob | Default        | Default     | Default     |       142.893 ns |     0.4380 ns |      0.4097 ns |       142.971 ns |  0.002 |    0.00 |    4 |  0.0067 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | DefaultJob | Default        | Default     | Default     |        15.837 ns |     0.0495 ns |      0.0414 ns |        15.821 ns |  0.000 |    0.00 |    3 |       - |         - |       0.000 |
|                                            |            |                |             |             |                  |               |                |                  |        |         |      |         |           |             |
| &#39;Hash 100 rows&#39;                            | MediumRun  | 15             | 2           | 10          |    82,329.802 ns |   379.5980 ns |    532.1435 ns |    82,009.690 ns |  1.000 |    0.01 |    5 |  0.8545 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | MediumRun  | 15             | 2           | 10          |   850,205.542 ns | 1,932.5591 ns |  2,771.6188 ns |   850,073.293 ns | 10.327 |    0.07 |    6 |  7.8125 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | MediumRun  | 15             | 2           | 10          | 4,554,396.307 ns | 7,689.9815 ns | 10,265.9053 ns | 4,554,914.320 ns | 55.321 |    0.37 |    7 | 39.0625 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | MediumRun  | 15             | 2           | 10          |         8.692 ns |     0.1079 ns |      0.1615 ns |         8.640 ns |  0.000 |    0.00 |    2 |  0.0014 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | MediumRun  | 15             | 2           | 10          |         7.077 ns |     0.0860 ns |      0.1205 ns |         7.032 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | MediumRun  | 15             | 2           | 10          |         8.897 ns |     0.0070 ns |      0.0102 ns |         8.892 ns |  0.000 |    0.00 |    2 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | MediumRun  | 15             | 2           | 10          |       141.971 ns |     0.4221 ns |      0.6053 ns |       141.852 ns |  0.002 |    0.00 |    4 |  0.0067 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | MediumRun  | 15             | 2           | 10          |        15.842 ns |     0.0404 ns |      0.0553 ns |        15.823 ns |  0.000 |    0.00 |    3 |       - |         - |       0.000 |
