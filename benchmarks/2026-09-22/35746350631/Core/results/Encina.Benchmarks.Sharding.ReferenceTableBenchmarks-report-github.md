```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 4.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean             | Error           | StdDev         | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------- |----------- |--------------- |------------ |------------ |-----------------:|----------------:|---------------:|-------:|--------:|-----:|-------:|----------:|------------:|
| &#39;Hash 100 rows&#39;                            | DefaultJob | Default        | Default     | Default     |    47,780.018 ns |     930.6922 ns |  1,142.9737 ns |  1.001 |    0.03 |    6 | 0.1221 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | DefaultJob | Default        | Default     | Default     |   510,957.333 ns |   1,303.2237 ns |  1,088.2508 ns | 10.700 |    0.24 |    7 | 0.9766 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 2,849,070.944 ns |  28,722.7528 ns | 23,984.7995 ns | 59.660 |    1.43 |    8 | 7.8125 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | DefaultJob | Default        | Default     | Default     |         8.121 ns |       0.2044 ns |      0.2099 ns |  0.000 |    0.00 |    3 | 0.0003 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | DefaultJob | Default        | Default     | Default     |         4.773 ns |       0.1177 ns |      0.1043 ns |  0.000 |    0.00 |    1 |      - |         - |       0.000 |
| Registry.GetConfiguration                  | DefaultJob | Default        | Default     | Default     |         6.826 ns |       0.1389 ns |      0.1757 ns |  0.000 |    0.00 |    2 |      - |         - |       0.000 |
| Registry.GetAllConfigurations              | DefaultJob | Default        | Default     | Default     |        95.695 ns |       1.3724 ns |      1.1460 ns |  0.002 |    0.00 |    5 | 0.0013 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | DefaultJob | Default        | Default     | Default     |        11.550 ns |       0.0282 ns |      0.0235 ns |  0.000 |    0.00 |    4 |      - |         - |       0.000 |
|                                            |            |                |             |             |                  |                 |                |        |         |      |        |           |             |
| &#39;Hash 100 rows&#39;                            | ShortRun   | 3              | 1           | 3           |    47,326.497 ns |   1,705.6036 ns |     93.4899 ns |  1.000 |    0.00 |    5 | 0.1221 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | ShortRun   | 3              | 1           | 3           |   515,897.818 ns |  83,954.7418 ns |  4,601.8427 ns | 10.901 |    0.09 |    6 | 0.9766 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | ShortRun   | 3              | 1           | 3           | 2,843,682.186 ns | 282,754.8581 ns | 15,498.7479 ns | 60.087 |    0.30 |    7 | 7.8125 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | ShortRun   | 3              | 1           | 3           |        10.089 ns |       1.7425 ns |      0.0955 ns |  0.000 |    0.00 |    3 | 0.0003 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | ShortRun   | 3              | 1           | 3           |         4.861 ns |       3.9255 ns |      0.2152 ns |  0.000 |    0.00 |    1 |      - |         - |       0.000 |
| Registry.GetConfiguration                  | ShortRun   | 3              | 1           | 3           |         6.705 ns |       0.1163 ns |      0.0064 ns |  0.000 |    0.00 |    2 |      - |         - |       0.000 |
| Registry.GetAllConfigurations              | ShortRun   | 3              | 1           | 3           |        98.337 ns |      60.7697 ns |      3.3310 ns |  0.002 |    0.00 |    4 | 0.0013 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | ShortRun   | 3              | 1           | 3           |        12.303 ns |      17.6653 ns |      0.9683 ns |  0.000 |    0.00 |    3 |      - |         - |       0.000 |
