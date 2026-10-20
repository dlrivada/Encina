```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3


```
| Method                                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean             | Error           | StdDev        | Ratio  | RatioSD | Rank | Gen0    | Allocated | Alloc Ratio |
|------------------------------------------- |----------- |--------------- |------------ |------------ |-----------------:|----------------:|--------------:|-------:|--------:|-----:|--------:|----------:|------------:|
| &#39;Hash 100 rows&#39;                            | DefaultJob | Default        | Default     | Default     |    81,851.890 ns |     299.4712 ns |   265.4734 ns |  1.000 |    0.00 |    6 |  0.8545 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | DefaultJob | Default        | Default     | Default     |   855,321.813 ns |   1,926.2792 ns | 1,707.5963 ns | 10.450 |    0.04 |    7 |  7.8125 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 4,608,963.661 ns |   5,765.2251 ns | 5,392.7951 ns | 56.309 |    0.19 |    8 | 39.0625 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | DefaultJob | Default        | Default     | Default     |         8.615 ns |       0.0885 ns |     0.0784 ns |  0.000 |    0.00 |    2 |  0.0014 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | DefaultJob | Default        | Default     | Default     |         7.286 ns |       0.0076 ns |     0.0064 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | DefaultJob | Default        | Default     | Default     |         9.022 ns |       0.0121 ns |     0.0101 ns |  0.000 |    0.00 |    3 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | DefaultJob | Default        | Default     | Default     |       136.884 ns |       0.6914 ns |     0.6467 ns |  0.002 |    0.00 |    5 |  0.0067 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | DefaultJob | Default        | Default     | Default     |        15.955 ns |       0.0229 ns |     0.0203 ns |  0.000 |    0.00 |    4 |       - |         - |       0.000 |
|                                            |            |                |             |             |                  |                 |               |        |         |      |         |           |             |
| &#39;Hash 100 rows&#39;                            | ShortRun   | 3              | 1           | 3           |    82,458.029 ns |   2,288.6335 ns |   125.4477 ns |  1.000 |    0.00 |    4 |  0.8545 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | ShortRun   | 3              | 1           | 3           |   876,711.681 ns |  62,810.0860 ns | 3,442.8328 ns | 10.632 |    0.04 |    5 |  7.8125 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | ShortRun   | 3              | 1           | 3           | 4,713,686.154 ns | 103,045.1689 ns | 5,648.2534 ns | 57.165 |    0.10 |    6 | 39.0625 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | ShortRun   | 3              | 1           | 3           |         8.332 ns |       2.0826 ns |     0.1142 ns |  0.000 |    0.00 |    1 |  0.0014 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | ShortRun   | 3              | 1           | 3           |         7.293 ns |       0.3842 ns |     0.0211 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | ShortRun   | 3              | 1           | 3           |         9.004 ns |       0.0749 ns |     0.0041 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | ShortRun   | 3              | 1           | 3           |       138.679 ns |       7.5554 ns |     0.4141 ns |  0.002 |    0.00 |    3 |  0.0067 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | ShortRun   | 3              | 1           | 3           |        15.936 ns |       0.2171 ns |     0.0119 ns |  0.000 |    0.00 |    2 |       - |         - |       0.000 |
