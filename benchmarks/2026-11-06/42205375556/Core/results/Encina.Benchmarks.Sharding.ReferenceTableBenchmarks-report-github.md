```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean             | Error             | StdDev          | Ratio  | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |----------- |--------------- |------------ |------------ |-----------------:|------------------:|----------------:|-------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Hash 100 rows&#39;                            | DefaultJob | Default        | Default     | Default     |    44,387.340 ns |       417.2763 ns |     369.9046 ns |  1.000 |    0.01 |    5 |  0.8545 |      - |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | DefaultJob | Default        | Default     | Default     |   465,255.094 ns |     4,329.2555 ns |   3,615.1244 ns | 10.482 |    0.11 |    6 |  8.3008 | 0.4883 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 2,580,433.741 ns |    27,637.3412 ns |  23,078.4316 ns | 58.138 |    0.68 |    7 | 39.0625 | 7.8125 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | DefaultJob | Default        | Default     | Default     |         5.405 ns |         0.1441 ns |       0.1874 ns |  0.000 |    0.00 |    2 |  0.0014 |      - |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | DefaultJob | Default        | Default     | Default     |         4.378 ns |         0.0453 ns |       0.0378 ns |  0.000 |    0.00 |    1 |       - |      - |         - |       0.000 |
| Registry.GetConfiguration                  | DefaultJob | Default        | Default     | Default     |         4.579 ns |         0.0811 ns |       0.0677 ns |  0.000 |    0.00 |    1 |       - |      - |         - |       0.000 |
| Registry.GetAllConfigurations              | DefaultJob | Default        | Default     | Default     |        70.440 ns |         0.8234 ns |       0.7299 ns |  0.002 |    0.00 |    4 |  0.0067 |      - |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | DefaultJob | Default        | Default     | Default     |         9.176 ns |         0.1874 ns |       0.1753 ns |  0.000 |    0.00 |    3 |       - |      - |         - |       0.000 |
|                                            |            |                |             |             |                  |                   |                 |        |         |      |         |        |           |             |
| &#39;Hash 100 rows&#39;                            | ShortRun   | 3              | 1           | 3           |    42,943.811 ns |     1,664.3927 ns |      91.2310 ns |  1.000 |    0.00 |    5 |  0.8545 |      - |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | ShortRun   | 3              | 1           | 3           |   459,239.555 ns |    49,817.5662 ns |   2,730.6689 ns | 10.694 |    0.06 |    6 |  8.3008 | 0.4883 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | ShortRun   | 3              | 1           | 3           | 2,546,721.378 ns | 2,362,002.7119 ns | 129,469.3394 ns | 59.304 |    2.61 |    7 | 39.0625 | 7.8125 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | ShortRun   | 3              | 1           | 3           |         5.769 ns |         4.3136 ns |       0.2364 ns |  0.000 |    0.00 |    2 |  0.0014 |      - |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | ShortRun   | 3              | 1           | 3           |         3.812 ns |         0.7910 ns |       0.0434 ns |  0.000 |    0.00 |    1 |       - |      - |         - |       0.000 |
| Registry.GetConfiguration                  | ShortRun   | 3              | 1           | 3           |         4.872 ns |         3.4578 ns |       0.1895 ns |  0.000 |    0.00 |    2 |       - |      - |         - |       0.000 |
| Registry.GetAllConfigurations              | ShortRun   | 3              | 1           | 3           |        73.640 ns |        28.6500 ns |       1.5704 ns |  0.002 |    0.00 |    4 |  0.0067 |      - |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | ShortRun   | 3              | 1           | 3           |         8.962 ns |         4.4700 ns |       0.2450 ns |  0.000 |    0.00 |    3 |       - |      - |         - |       0.000 |
