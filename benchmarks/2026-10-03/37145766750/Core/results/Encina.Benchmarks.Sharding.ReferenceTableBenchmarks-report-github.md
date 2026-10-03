```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean             | Error             | StdDev         | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------- |----------- |--------------- |------------ |------------ |-----------------:|------------------:|---------------:|-------:|--------:|-----:|-------:|----------:|------------:|
| &#39;Hash 100 rows&#39;                            | DefaultJob | Default        | Default     | Default     |    47,094.009 ns |       129.5650 ns |    101.1558 ns |  1.000 |    0.00 |    6 | 0.1221 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | DefaultJob | Default        | Default     | Default     |   510,768.002 ns |       661.7022 ns |    516.6136 ns | 10.846 |    0.02 |    7 | 0.9766 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 2,852,545.809 ns |    33,808.1108 ns | 43,960.0975 ns | 60.572 |    0.92 |    8 | 7.8125 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | DefaultJob | Default        | Default     | Default     |         9.374 ns |         0.1057 ns |      0.0825 ns |  0.000 |    0.00 |    3 | 0.0003 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | DefaultJob | Default        | Default     | Default     |         4.743 ns |         0.0880 ns |      0.0687 ns |  0.000 |    0.00 |    1 |      - |         - |       0.000 |
| Registry.GetConfiguration                  | DefaultJob | Default        | Default     | Default     |         6.702 ns |         0.0108 ns |      0.0084 ns |  0.000 |    0.00 |    2 |      - |         - |       0.000 |
| Registry.GetAllConfigurations              | DefaultJob | Default        | Default     | Default     |        94.977 ns |         0.3469 ns |      0.3075 ns |  0.002 |    0.00 |    5 | 0.0013 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | DefaultJob | Default        | Default     | Default     |        11.496 ns |         0.0304 ns |      0.0269 ns |  0.000 |    0.00 |    4 |      - |         - |       0.000 |
|                                            |            |                |             |             |                  |                   |                |        |         |      |        |           |             |
| &#39;Hash 100 rows&#39;                            | ShortRun   | 3              | 1           | 3           |    48,344.787 ns |    24,301.4355 ns |  1,332.0437 ns |  1.000 |    0.03 |    6 | 0.1221 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | ShortRun   | 3              | 1           | 3           |   514,852.406 ns |    58,923.5950 ns |  3,229.8011 ns | 10.655 |    0.26 |    7 | 0.9766 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | ShortRun   | 3              | 1           | 3           | 2,986,313.225 ns | 1,325,050.0128 ns | 72,630.4627 ns | 61.802 |    1.95 |    8 | 7.8125 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | ShortRun   | 3              | 1           | 3           |         9.231 ns |         4.6186 ns |      0.2532 ns |  0.000 |    0.00 |    3 | 0.0003 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | ShortRun   | 3              | 1           | 3           |         4.962 ns |         4.4254 ns |      0.2426 ns |  0.000 |    0.00 |    1 |      - |         - |       0.000 |
| Registry.GetConfiguration                  | ShortRun   | 3              | 1           | 3           |         6.705 ns |         0.1357 ns |      0.0074 ns |  0.000 |    0.00 |    2 |      - |         - |       0.000 |
| Registry.GetAllConfigurations              | ShortRun   | 3              | 1           | 3           |       105.793 ns |       105.5589 ns |      5.7860 ns |  0.002 |    0.00 |    5 | 0.0013 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | ShortRun   | 3              | 1           | 3           |        11.749 ns |         0.5823 ns |      0.0319 ns |  0.000 |    0.00 |    4 |      - |         - |       0.000 |
