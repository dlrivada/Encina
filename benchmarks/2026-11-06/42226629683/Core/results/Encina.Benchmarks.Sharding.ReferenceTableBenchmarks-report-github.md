```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.90GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3


```
| Method                                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean             | Error           | StdDev        | Ratio  | RatioSD | Rank | Gen0    | Allocated | Alloc Ratio |
|------------------------------------------- |----------- |--------------- |------------ |------------ |-----------------:|----------------:|--------------:|-------:|--------:|-----:|--------:|----------:|------------:|
| &#39;Hash 100 rows&#39;                            | DefaultJob | Default        | Default     | Default     |    83,094.188 ns |     125.9977 ns |   105.2138 ns |  1.000 |    0.00 |    5 |  0.8545 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | DefaultJob | Default        | Default     | Default     |   868,856.166 ns |   2,776.1071 ns | 2,318.1751 ns | 10.456 |    0.03 |    6 |  7.8125 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 4,698,955.580 ns |  10,350.8477 ns | 9,682.1894 ns | 56.550 |    0.13 |    7 | 39.0625 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | DefaultJob | Default        | Default     | Default     |         9.144 ns |       0.0756 ns |     0.0670 ns |  0.000 |    0.00 |    2 |  0.0014 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | DefaultJob | Default        | Default     | Default     |         7.280 ns |       0.0089 ns |     0.0074 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | DefaultJob | Default        | Default     | Default     |         9.005 ns |       0.0098 ns |     0.0082 ns |  0.000 |    0.00 |    2 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | DefaultJob | Default        | Default     | Default     |       147.449 ns |       0.5592 ns |     0.5231 ns |  0.002 |    0.00 |    4 |  0.0067 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | DefaultJob | Default        | Default     | Default     |        15.981 ns |       0.0995 ns |     0.0882 ns |  0.000 |    0.00 |    3 |       - |         - |       0.000 |
|                                            |            |                |             |             |                  |                 |               |        |         |      |         |           |             |
| &#39;Hash 100 rows&#39;                            | ShortRun   | 3              | 1           | 3           |    82,987.472 ns |   3,659.9201 ns |   200.6126 ns |  1.000 |    0.00 |    4 |  0.8545 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | ShortRun   | 3              | 1           | 3           |   879,724.581 ns |  50,329.0593 ns | 2,758.7056 ns | 10.601 |    0.04 |    5 |  7.8125 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | ShortRun   | 3              | 1           | 3           | 4,645,689.440 ns | 136,826.8278 ns | 7,499.9402 ns | 55.981 |    0.14 |    6 | 39.0625 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | ShortRun   | 3              | 1           | 3           |         8.681 ns |       1.2015 ns |     0.0659 ns |  0.000 |    0.00 |    1 |  0.0014 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | ShortRun   | 3              | 1           | 3           |         7.307 ns |       0.8095 ns |     0.0444 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | ShortRun   | 3              | 1           | 3           |         9.030 ns |       0.7673 ns |     0.0421 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | ShortRun   | 3              | 1           | 3           |       140.590 ns |       8.4766 ns |     0.4646 ns |  0.002 |    0.00 |    3 |  0.0067 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | ShortRun   | 3              | 1           | 3           |        15.936 ns |       0.5764 ns |     0.0316 ns |  0.000 |    0.00 |    2 |       - |         - |       0.000 |
