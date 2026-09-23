```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean             | Error           | StdDev         | Ratio  | RatioSD | Rank | Gen0    | Allocated | Alloc Ratio |
|------------------------------------------- |----------- |--------------- |------------ |------------ |-----------------:|----------------:|---------------:|-------:|--------:|-----:|--------:|----------:|------------:|
| &#39;Hash 100 rows&#39;                            | DefaultJob | Default        | Default     | Default     |    78,115.828 ns |      87.1984 ns |     81.5655 ns |  1.000 |    0.00 |    5 |  0.8545 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | DefaultJob | Default        | Default     | Default     |   862,861.587 ns |   2,005.5480 ns |  1,674.7235 ns | 11.046 |    0.02 |    6 |  7.8125 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 4,454,449.747 ns |  19,325.8162 ns | 18,077.3805 ns | 57.024 |    0.23 |    7 | 39.0625 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | DefaultJob | Default        | Default     | Default     |         8.614 ns |       0.0929 ns |      0.0824 ns |  0.000 |    0.00 |    2 |  0.0014 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | DefaultJob | Default        | Default     | Default     |         7.075 ns |       0.0061 ns |      0.0051 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | DefaultJob | Default        | Default     | Default     |         8.894 ns |       0.0094 ns |      0.0078 ns |  0.000 |    0.00 |    2 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | DefaultJob | Default        | Default     | Default     |       143.178 ns |       0.3424 ns |      0.3203 ns |  0.002 |    0.00 |    4 |  0.0067 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | DefaultJob | Default        | Default     | Default     |        15.821 ns |       0.0129 ns |      0.0101 ns |  0.000 |    0.00 |    3 |       - |         - |       0.000 |
|                                            |            |                |             |             |                  |                 |                |        |         |      |         |           |             |
| &#39;Hash 100 rows&#39;                            | ShortRun   | 3              | 1           | 3           |    80,377.167 ns |  27,149.2773 ns |  1,488.1435 ns |  1.000 |    0.02 |    4 |  0.8545 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | ShortRun   | 3              | 1           | 3           |   827,198.630 ns | 115,819.9382 ns |  6,348.4817 ns | 10.294 |    0.18 |    5 |  7.8125 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | ShortRun   | 3              | 1           | 3           | 4,610,694.544 ns | 103,052.8771 ns |  5,648.6760 ns | 57.376 |    0.91 |    6 | 39.0625 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | ShortRun   | 3              | 1           | 3           |         8.127 ns |       1.2194 ns |      0.0668 ns |  0.000 |    0.00 |    1 |  0.0014 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | ShortRun   | 3              | 1           | 3           |         7.046 ns |       0.4278 ns |      0.0235 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | ShortRun   | 3              | 1           | 3           |         8.896 ns |       0.0366 ns |      0.0020 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | ShortRun   | 3              | 1           | 3           |       142.031 ns |       1.7465 ns |      0.0957 ns |  0.002 |    0.00 |    3 |  0.0067 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | ShortRun   | 3              | 1           | 3           |        16.354 ns |       0.2670 ns |      0.0146 ns |  0.000 |    0.00 |    2 |       - |         - |       0.000 |
