```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3


```
| Method                                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean             | Error          | StdDev         | Ratio  | RatioSD | Rank | Gen0    | Allocated | Alloc Ratio |
|------------------------------------------- |----------- |--------------- |------------ |------------ |-----------------:|---------------:|---------------:|-------:|--------:|-----:|--------:|----------:|------------:|
| &#39;Hash 100 rows&#39;                            | DefaultJob | Default        | Default     | Default     |    79,627.056 ns |    281.7925 ns |    235.3095 ns |  1.000 |    0.00 |    5 |  0.8545 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | DefaultJob | Default        | Default     | Default     |   853,070.879 ns |  1,565.6620 ns |  1,464.5212 ns | 10.713 |    0.04 |    6 |  7.8125 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 4,569,698.142 ns | 15,232.7187 ns | 11,892.7048 ns | 57.389 |    0.22 |    7 | 39.0625 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | DefaultJob | Default        | Default     | Default     |         8.915 ns |      0.2342 ns |      0.2506 ns |  0.000 |    0.00 |    1 |  0.0014 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | DefaultJob | Default        | Default     | Default     |         9.256 ns |      0.0180 ns |      0.0159 ns |  0.000 |    0.00 |    2 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | DefaultJob | Default        | Default     | Default     |         9.028 ns |      0.0205 ns |      0.0171 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | DefaultJob | Default        | Default     | Default     |       142.400 ns |      0.6748 ns |      0.5982 ns |  0.002 |    0.00 |    4 |  0.0067 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | DefaultJob | Default        | Default     | Default     |        15.957 ns |      0.0098 ns |      0.0082 ns |  0.000 |    0.00 |    3 |       - |         - |       0.000 |
|                                            |            |                |             |             |                  |                |                |        |         |      |         |           |             |
| &#39;Hash 100 rows&#39;                            | ShortRun   | 3              | 1           | 3           |    82,568.631 ns | 15,850.2500 ns |    868.8057 ns |  1.000 |    0.01 |    4 |  0.8545 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | ShortRun   | 3              | 1           | 3           |   874,397.030 ns |  9,136.7489 ns |    500.8160 ns | 10.591 |    0.10 |    5 |  7.8125 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | ShortRun   | 3              | 1           | 3           | 4,654,490.911 ns | 76,243.5564 ns |  4,179.1666 ns | 56.375 |    0.51 |    6 | 39.0625 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | ShortRun   | 3              | 1           | 3           |         8.611 ns |      4.0907 ns |      0.2242 ns |  0.000 |    0.00 |    1 |  0.0014 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | ShortRun   | 3              | 1           | 3           |         7.303 ns |      0.2629 ns |      0.0144 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | ShortRun   | 3              | 1           | 3           |         9.040 ns |      0.4108 ns |      0.0225 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | ShortRun   | 3              | 1           | 3           |       140.672 ns |      2.0364 ns |      0.1116 ns |  0.002 |    0.00 |    3 |  0.0067 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | ShortRun   | 3              | 1           | 3           |        15.955 ns |      0.2344 ns |      0.0129 ns |  0.000 |    0.00 |    2 |       - |         - |       0.000 |
