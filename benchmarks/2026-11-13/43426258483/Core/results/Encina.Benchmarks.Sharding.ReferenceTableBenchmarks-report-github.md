```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.98GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean             | Error           | StdDev         | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------- |----------- |--------------- |------------ |------------ |-----------------:|----------------:|---------------:|-------:|--------:|-----:|-------:|----------:|------------:|
| &#39;Hash 100 rows&#39;                            | DefaultJob | Default        | Default     | Default     |    68,666.965 ns |   1,122.5640 ns |    995.1238 ns |  1.000 |    0.02 |    5 | 0.1221 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | DefaultJob | Default        | Default     | Default     |   733,485.833 ns |   6,925.2574 ns |  5,782.9035 ns | 10.684 |    0.17 |    6 | 0.9766 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 4,032,497.946 ns |  52,631.7287 ns | 49,231.7517 ns | 58.737 |    1.08 |    7 | 7.8125 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | DefaultJob | Default        | Default     | Default     |         9.312 ns |       0.1970 ns |      0.1746 ns |  0.000 |    0.00 |    2 | 0.0003 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | DefaultJob | Default        | Default     | Default     |         7.915 ns |       0.1037 ns |      0.0970 ns |  0.000 |    0.00 |    1 |      - |         - |       0.000 |
| Registry.GetConfiguration                  | DefaultJob | Default        | Default     | Default     |         9.002 ns |       0.1111 ns |      0.0985 ns |  0.000 |    0.00 |    2 |      - |         - |       0.000 |
| Registry.GetAllConfigurations              | DefaultJob | Default        | Default     | Default     |       137.082 ns |       1.7158 ns |      1.6050 ns |  0.002 |    0.00 |    4 | 0.0012 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | DefaultJob | Default        | Default     | Default     |        17.322 ns |       0.2413 ns |      0.2257 ns |  0.000 |    0.00 |    3 |      - |         - |       0.000 |
|                                            |            |                |             |             |                  |                 |                |        |         |      |        |           |             |
| &#39;Hash 100 rows&#39;                            | ShortRun   | 3              | 1           | 3           |    67,958.551 ns |  12,250.5937 ns |    671.4964 ns |  1.000 |    0.01 |    5 | 0.1221 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | ShortRun   | 3              | 1           | 3           |   749,321.695 ns |  89,587.8152 ns |  4,910.6105 ns | 11.027 |    0.11 |    6 | 0.9766 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | ShortRun   | 3              | 1           | 3           | 4,031,940.794 ns | 556,137.0818 ns | 30,483.7502 ns | 59.333 |    0.64 |    7 | 7.8125 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | ShortRun   | 3              | 1           | 3           |         9.292 ns |       5.3143 ns |      0.2913 ns |  0.000 |    0.00 |    2 | 0.0003 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | ShortRun   | 3              | 1           | 3           |         7.258 ns |       1.2625 ns |      0.0692 ns |  0.000 |    0.00 |    1 |      - |         - |       0.000 |
| Registry.GetConfiguration                  | ShortRun   | 3              | 1           | 3           |         9.081 ns |       4.0468 ns |      0.2218 ns |  0.000 |    0.00 |    2 |      - |         - |       0.000 |
| Registry.GetAllConfigurations              | ShortRun   | 3              | 1           | 3           |       139.432 ns |      21.6732 ns |      1.1880 ns |  0.002 |    0.00 |    4 | 0.0012 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | ShortRun   | 3              | 1           | 3           |        17.242 ns |       2.2685 ns |      0.1243 ns |  0.000 |    0.00 |    3 |      - |         - |       0.000 |
