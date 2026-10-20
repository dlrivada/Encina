```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean             | Error           | StdDev        | Ratio  | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |----------- |--------------- |------------ |------------ |-----------------:|----------------:|--------------:|-------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Hash 100 rows&#39;                            | DefaultJob | Default        | Default     | Default     |    58,823.954 ns |     110.9643 ns |    98.3669 ns |  1.000 |    0.00 |    6 |  0.8545 |      - |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | DefaultJob | Default        | Default     | Default     |   635,529.650 ns |   1,728.1527 ns | 1,531.9624 ns | 10.804 |    0.03 |    7 |  7.8125 |      - |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 3,443,898.669 ns |   7,742.2609 ns | 6,863.3126 ns | 58.546 |    0.15 |    8 | 39.0625 | 7.8125 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | DefaultJob | Default        | Default     | Default     |         8.729 ns |       0.2318 ns |     0.3325 ns |  0.000 |    0.00 |    3 |  0.0014 |      - |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | DefaultJob | Default        | Default     | Default     |         5.738 ns |       0.0080 ns |     0.0067 ns |  0.000 |    0.00 |    1 |       - |      - |         - |       0.000 |
| Registry.GetConfiguration                  | DefaultJob | Default        | Default     | Default     |         7.080 ns |       0.0150 ns |     0.0125 ns |  0.000 |    0.00 |    2 |       - |      - |         - |       0.000 |
| Registry.GetAllConfigurations              | DefaultJob | Default        | Default     | Default     |       109.735 ns |       1.0332 ns |     0.9665 ns |  0.002 |    0.00 |    5 |  0.0067 |      - |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | DefaultJob | Default        | Default     | Default     |        12.579 ns |       0.0092 ns |     0.0077 ns |  0.000 |    0.00 |    4 |       - |      - |         - |       0.000 |
|                                            |            |                |             |             |                  |                 |               |        |         |      |         |        |           |             |
| &#39;Hash 100 rows&#39;                            | ShortRun   | 3              | 1           | 3           |    59,561.600 ns |   2,660.6730 ns |   145.8405 ns |  1.000 |    0.00 |    5 |  0.8545 |      - |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | ShortRun   | 3              | 1           | 3           |   645,081.730 ns |  23,937.6182 ns | 1,312.1016 ns | 10.831 |    0.03 |    6 |  7.8125 |      - |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | ShortRun   | 3              | 1           | 3           | 3,420,550.664 ns | 103,544.8611 ns | 5,675.6433 ns | 57.429 |    0.15 |    7 | 39.0625 | 7.8125 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | ShortRun   | 3              | 1           | 3           |         8.331 ns |       2.5047 ns |     0.1373 ns |  0.000 |    0.00 |    2 |  0.0014 |      - |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | ShortRun   | 3              | 1           | 3           |         5.745 ns |       0.2121 ns |     0.0116 ns |  0.000 |    0.00 |    1 |       - |      - |         - |       0.000 |
| Registry.GetConfiguration                  | ShortRun   | 3              | 1           | 3           |         7.098 ns |       0.1498 ns |     0.0082 ns |  0.000 |    0.00 |    2 |       - |      - |         - |       0.000 |
| Registry.GetAllConfigurations              | ShortRun   | 3              | 1           | 3           |       111.081 ns |      33.0626 ns |     1.8123 ns |  0.002 |    0.00 |    4 |  0.0067 |      - |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | ShortRun   | 3              | 1           | 3           |        12.442 ns |       3.4397 ns |     0.1885 ns |  0.000 |    0.00 |    3 |       - |      - |         - |       0.000 |
