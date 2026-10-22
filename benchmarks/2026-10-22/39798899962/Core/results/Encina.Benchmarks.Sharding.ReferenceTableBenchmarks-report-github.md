```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean             | Error           | StdDev         | Ratio  | RatioSD | Rank | Gen0    | Allocated | Alloc Ratio |
|------------------------------------------- |----------- |--------------- |------------ |------------ |-----------------:|----------------:|---------------:|-------:|--------:|-----:|--------:|----------:|------------:|
| &#39;Hash 100 rows&#39;                            | DefaultJob | Default        | Default     | Default     |    82,791.265 ns |     262.8288 ns |    232.9909 ns |  1.000 |    0.00 |    6 |  0.4883 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | DefaultJob | Default        | Default     | Default     |   929,173.437 ns |   3,591.5832 ns |  3,183.8449 ns | 11.223 |    0.05 |    7 |  4.8828 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 4,733,412.617 ns |  34,522.5008 ns | 30,603.2978 ns | 57.173 |    0.39 |    8 | 23.4375 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | DefaultJob | Default        | Default     | Default     |        10.476 ns |       0.0332 ns |      0.0310 ns |  0.000 |    0.00 |    3 |  0.0010 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | DefaultJob | Default        | Default     | Default     |         7.285 ns |       0.0251 ns |      0.0235 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | DefaultJob | Default        | Default     | Default     |         8.327 ns |       0.0108 ns |      0.0096 ns |  0.000 |    0.00 |    2 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | DefaultJob | Default        | Default     | Default     |       142.031 ns |       0.1500 ns |      0.1253 ns |  0.002 |    0.00 |    5 |  0.0043 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | DefaultJob | Default        | Default     | Default     |        14.247 ns |       0.0207 ns |      0.0172 ns |  0.000 |    0.00 |    4 |       - |         - |       0.000 |
|                                            |            |                |             |             |                  |                 |                |        |         |      |         |           |             |
| &#39;Hash 100 rows&#39;                            | ShortRun   | 3              | 1           | 3           |    83,801.007 ns |   6,267.9837 ns |    343.5693 ns |  1.000 |    0.01 |    5 |  0.4883 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | ShortRun   | 3              | 1           | 3           |   890,251.095 ns |  12,735.9203 ns |    698.0988 ns | 10.624 |    0.04 |    6 |  4.8828 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | ShortRun   | 3              | 1           | 3           | 4,747,712.344 ns | 167,058.2366 ns |  9,157.0257 ns | 56.655 |    0.22 |    7 | 23.4375 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | ShortRun   | 3              | 1           | 3           |        11.272 ns |       0.3801 ns |      0.0208 ns |  0.000 |    0.00 |    2 |  0.0010 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | ShortRun   | 3              | 1           | 3           |         6.377 ns |       0.0317 ns |      0.0017 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | ShortRun   | 3              | 1           | 3           |        10.061 ns |       1.2564 ns |      0.0689 ns |  0.000 |    0.00 |    2 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | ShortRun   | 3              | 1           | 3           |       137.746 ns |       6.0299 ns |      0.3305 ns |  0.002 |    0.00 |    4 |  0.0043 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | ShortRun   | 3              | 1           | 3           |        14.405 ns |       0.1186 ns |      0.0065 ns |  0.000 |    0.00 |    3 |       - |         - |       0.000 |
