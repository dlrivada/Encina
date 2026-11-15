```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean             | Error          | StdDev         | Ratio  | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |----------- |--------------- |------------ |------------ |-----------------:|---------------:|---------------:|-------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Hash 100 rows&#39;                            | DefaultJob | Default        | Default     | Default     |    58,954.070 ns |    357.8844 ns |    317.2552 ns |  1.000 |    0.01 |    6 |  0.8545 |      - |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | DefaultJob | Default        | Default     | Default     |   617,895.407 ns |  1,322.0024 ns |  1,103.9319 ns | 10.481 |    0.06 |    7 |  7.8125 |      - |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 3,332,324.329 ns | 10,578.5593 ns |  9,895.1909 ns | 56.526 |    0.33 |    8 | 39.0625 | 7.8125 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | DefaultJob | Default        | Default     | Default     |         8.986 ns |      0.2321 ns |      0.3018 ns |  0.000 |    0.00 |    3 |  0.0014 |      - |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | DefaultJob | Default        | Default     | Default     |         5.721 ns |      0.0475 ns |      0.0444 ns |  0.000 |    0.00 |    1 |       - |      - |         - |       0.000 |
| Registry.GetConfiguration                  | DefaultJob | Default        | Default     | Default     |         7.394 ns |      0.0586 ns |      0.0548 ns |  0.000 |    0.00 |    2 |       - |      - |         - |       0.000 |
| Registry.GetAllConfigurations              | DefaultJob | Default        | Default     | Default     |       116.622 ns |      0.9079 ns |      0.8492 ns |  0.002 |    0.00 |    5 |  0.0067 |      - |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | DefaultJob | Default        | Default     | Default     |        12.603 ns |      0.0158 ns |      0.0147 ns |  0.000 |    0.00 |    4 |       - |      - |         - |       0.000 |
|                                            |            |                |             |             |                  |                |                |        |         |      |         |        |           |             |
| &#39;Hash 100 rows&#39;                            | MediumRun  | 15             | 2           | 10          |    58,552.916 ns |    162.3475 ns |    216.7293 ns |  1.000 |    0.01 |    6 |  0.8545 |      - |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | MediumRun  | 15             | 2           | 10          |   626,162.261 ns |  3,082.0658 ns |  4,420.2070 ns | 10.694 |    0.08 |    7 |  7.8125 |      - |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | MediumRun  | 15             | 2           | 10          | 3,355,440.372 ns | 19,119.2933 ns | 28,024.8146 ns | 57.307 |    0.51 |    8 | 39.0625 | 7.8125 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | MediumRun  | 15             | 2           | 10          |         7.922 ns |      0.1276 ns |      0.1870 ns |  0.000 |    0.00 |    3 |  0.0014 |      - |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | MediumRun  | 15             | 2           | 10          |         5.747 ns |      0.0319 ns |      0.0458 ns |  0.000 |    0.00 |    1 |       - |      - |         - |       0.000 |
| Registry.GetConfiguration                  | MediumRun  | 15             | 2           | 10          |         7.086 ns |      0.0610 ns |      0.0815 ns |  0.000 |    0.00 |    2 |       - |      - |         - |       0.000 |
| Registry.GetAllConfigurations              | MediumRun  | 15             | 2           | 10          |       107.655 ns |      1.1636 ns |      1.6688 ns |  0.002 |    0.00 |    5 |  0.0067 |      - |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | MediumRun  | 15             | 2           | 10          |        12.627 ns |      0.0422 ns |      0.0591 ns |  0.000 |    0.00 |    4 |       - |      - |         - |       0.000 |
