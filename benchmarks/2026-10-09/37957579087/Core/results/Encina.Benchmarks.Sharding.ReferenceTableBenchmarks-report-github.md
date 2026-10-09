```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.15GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean             | Error          | StdDev         | Ratio  | RatioSD | Rank | Gen0    | Allocated | Alloc Ratio |
|------------------------------------------- |----------- |--------------- |------------ |------------ |-----------------:|---------------:|---------------:|-------:|--------:|-----:|--------:|----------:|------------:|
| &#39;Hash 100 rows&#39;                            | DefaultJob | Default        | Default     | Default     |    78,884.337 ns |    363.7170 ns |    340.2211 ns |  1.000 |    0.01 |    6 |  0.4883 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | DefaultJob | Default        | Default     | Default     |   840,993.803 ns |  1,015.4492 ns |    847.9460 ns | 10.661 |    0.05 |    7 |  4.8828 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 4,515,659.080 ns | 11,339.0165 ns | 10,606.5231 ns | 57.245 |    0.27 |    8 | 23.4375 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | DefaultJob | Default        | Default     | Default     |         9.721 ns |      0.0355 ns |      0.0332 ns |  0.000 |    0.00 |    3 |  0.0010 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | DefaultJob | Default        | Default     | Default     |         6.782 ns |      0.0275 ns |      0.0257 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | DefaultJob | Default        | Default     | Default     |         7.921 ns |      0.0124 ns |      0.0110 ns |  0.000 |    0.00 |    2 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | DefaultJob | Default        | Default     | Default     |       138.687 ns |      0.3146 ns |      0.2789 ns |  0.002 |    0.00 |    5 |  0.0043 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | DefaultJob | Default        | Default     | Default     |        14.096 ns |      0.0174 ns |      0.0154 ns |  0.000 |    0.00 |    4 |       - |         - |       0.000 |
|                                            |            |                |             |             |                  |                |                |        |         |      |         |           |             |
| &#39;Hash 100 rows&#39;                            | ShortRun   | 3              | 1           | 3           |    80,650.471 ns |  4,952.8633 ns |    271.4832 ns |  1.000 |    0.00 |    5 |  0.4883 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | ShortRun   | 3              | 1           | 3           |   839,264.448 ns | 29,757.1094 ns |  1,631.0876 ns | 10.406 |    0.03 |    6 |  4.8828 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | ShortRun   | 3              | 1           | 3           | 4,449,795.659 ns | 94,592.8398 ns |  5,184.9528 ns | 55.174 |    0.17 |    7 | 23.4375 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | ShortRun   | 3              | 1           | 3           |        10.246 ns |      1.2792 ns |      0.0701 ns |  0.000 |    0.00 |    2 |  0.0010 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | ShortRun   | 3              | 1           | 3           |         6.613 ns |      0.1182 ns |      0.0065 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | ShortRun   | 3              | 1           | 3           |         7.873 ns |      1.0220 ns |      0.0560 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | ShortRun   | 3              | 1           | 3           |       134.056 ns |     13.5713 ns |      0.7439 ns |  0.002 |    0.00 |    4 |  0.0043 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | ShortRun   | 3              | 1           | 3           |        14.962 ns |      0.1010 ns |      0.0055 ns |  0.000 |    0.00 |    3 |       - |         - |       0.000 |
