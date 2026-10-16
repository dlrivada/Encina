```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean             | Error          | StdDev         | Ratio  | RatioSD | Rank | Gen0    | Allocated | Alloc Ratio |
|------------------------------------------- |----------- |--------------- |------------ |------------ |-----------------:|---------------:|---------------:|-------:|--------:|-----:|--------:|----------:|------------:|
| &#39;Hash 100 rows&#39;                            | DefaultJob | Default        | Default     | Default     |    81,735.559 ns |    372.8444 ns |    330.5168 ns |  1.000 |    0.01 |    6 |  0.4883 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | DefaultJob | Default        | Default     | Default     |   871,502.306 ns |  2,539.7305 ns |  2,251.4049 ns | 10.663 |    0.05 |    7 |  4.8828 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 4,926,337.306 ns | 13,750.2883 ns | 12,189.2724 ns | 60.273 |    0.28 |    8 | 23.4375 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | DefaultJob | Default        | Default     | Default     |        10.534 ns |      0.0420 ns |      0.0393 ns |  0.000 |    0.00 |    3 |  0.0010 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | DefaultJob | Default        | Default     | Default     |         7.103 ns |      0.0123 ns |      0.0115 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | DefaultJob | Default        | Default     | Default     |         8.124 ns |      0.0172 ns |      0.0161 ns |  0.000 |    0.00 |    2 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | DefaultJob | Default        | Default     | Default     |       130.039 ns |      0.2715 ns |      0.2267 ns |  0.002 |    0.00 |    5 |  0.0043 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | DefaultJob | Default        | Default     | Default     |        14.225 ns |      0.0286 ns |      0.0254 ns |  0.000 |    0.00 |    4 |       - |         - |       0.000 |
|                                            |            |                |             |             |                  |                |                |        |         |      |         |           |             |
| &#39;Hash 100 rows&#39;                            | ShortRun   | 3              | 1           | 3           |    83,338.153 ns | 14,484.9506 ns |    793.9690 ns |  1.000 |    0.01 |    6 |  0.4883 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | ShortRun   | 3              | 1           | 3           |   886,584.112 ns | 99,774.4399 ns |  5,468.9737 ns | 10.639 |    0.10 |    7 |  4.8828 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | ShortRun   | 3              | 1           | 3           | 4,718,472.422 ns | 79,271.0617 ns |  4,345.1144 ns | 56.622 |    0.47 |    8 | 23.4375 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | ShortRun   | 3              | 1           | 3           |        10.196 ns |      0.7003 ns |      0.0384 ns |  0.000 |    0.00 |    3 |  0.0010 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | ShortRun   | 3              | 1           | 3           |         6.377 ns |      0.1942 ns |      0.0106 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | ShortRun   | 3              | 1           | 3           |         8.118 ns |      0.4715 ns |      0.0258 ns |  0.000 |    0.00 |    2 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | ShortRun   | 3              | 1           | 3           |       133.236 ns |      1.0010 ns |      0.0549 ns |  0.002 |    0.00 |    5 |  0.0043 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | ShortRun   | 3              | 1           | 3           |        15.448 ns |      0.2636 ns |      0.0144 ns |  0.000 |    0.00 |    4 |       - |         - |       0.000 |
