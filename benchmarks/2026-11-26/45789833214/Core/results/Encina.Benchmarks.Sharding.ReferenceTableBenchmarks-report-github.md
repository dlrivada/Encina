```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.37GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean             | Error           | StdDev         | Ratio  | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |----------- |--------------- |------------ |------------ |-----------------:|----------------:|---------------:|-------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Hash 100 rows&#39;                            | DefaultJob | Default        | Default     | Default     |    43,794.994 ns |     527.2511 ns |    467.3944 ns |  1.000 |    0.01 |    6 |  0.8545 |      - |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | DefaultJob | Default        | Default     | Default     |   464,523.902 ns |   3,407.6236 ns |  3,187.4932 ns | 10.608 |    0.13 |    7 |  8.3008 | 0.4883 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 2,440,909.710 ns |  17,056.9163 ns | 14,243.2977 ns | 55.741 |    0.65 |    8 | 39.0625 | 7.8125 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | DefaultJob | Default        | Default     | Default     |         5.442 ns |       0.1421 ns |      0.1579 ns |  0.000 |    0.00 |    3 |  0.0014 |      - |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | DefaultJob | Default        | Default     | Default     |         3.957 ns |       0.0948 ns |      0.0887 ns |  0.000 |    0.00 |    1 |       - |      - |         - |       0.000 |
| Registry.GetConfiguration                  | DefaultJob | Default        | Default     | Default     |         4.483 ns |       0.1277 ns |      0.1520 ns |  0.000 |    0.00 |    2 |       - |      - |         - |       0.000 |
| Registry.GetAllConfigurations              | DefaultJob | Default        | Default     | Default     |        73.670 ns |       1.4506 ns |      1.2860 ns |  0.002 |    0.00 |    5 |  0.0067 |      - |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | DefaultJob | Default        | Default     | Default     |         9.324 ns |       0.1625 ns |      0.1441 ns |  0.000 |    0.00 |    4 |       - |      - |         - |       0.000 |
|                                            |            |                |             |             |                  |                 |                |        |         |      |         |        |           |             |
| &#39;Hash 100 rows&#39;                            | ShortRun   | 3              | 1           | 3           |    45,209.250 ns |   8,017.2189 ns |    439.4508 ns |  1.000 |    0.01 |    5 |  0.8545 |      - |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | ShortRun   | 3              | 1           | 3           |   470,187.902 ns | 218,360.5013 ns | 11,969.0759 ns | 10.401 |    0.25 |    6 |  8.3008 | 0.4883 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | ShortRun   | 3              | 1           | 3           | 2,534,672.622 ns | 504,971.1675 ns | 27,679.1737 ns | 56.069 |    0.71 |    7 | 39.0625 | 7.8125 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | ShortRun   | 3              | 1           | 3           |         5.894 ns |       2.4378 ns |      0.1336 ns |  0.000 |    0.00 |    2 |  0.0014 |      - |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | ShortRun   | 3              | 1           | 3           |         3.909 ns |       2.1084 ns |      0.1156 ns |  0.000 |    0.00 |    1 |       - |      - |         - |       0.000 |
| Registry.GetConfiguration                  | ShortRun   | 3              | 1           | 3           |         5.125 ns |       2.2534 ns |      0.1235 ns |  0.000 |    0.00 |    2 |       - |      - |         - |       0.000 |
| Registry.GetAllConfigurations              | ShortRun   | 3              | 1           | 3           |        71.888 ns |      17.0030 ns |      0.9320 ns |  0.002 |    0.00 |    4 |  0.0067 |      - |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | ShortRun   | 3              | 1           | 3           |         9.312 ns |       1.1331 ns |      0.0621 ns |  0.000 |    0.00 |    3 |       - |      - |         - |       0.000 |
