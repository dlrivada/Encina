```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean             | Error           | StdDev         | Ratio  | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |----------- |--------------- |------------ |------------ |-----------------:|----------------:|---------------:|-------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Hash 100 rows&#39;                            | DefaultJob | Default        | Default     | Default     |    43,915.108 ns |     858.0768 ns |    881.1817 ns |  1.000 |    0.03 |    5 |  0.8545 |      - |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | DefaultJob | Default        | Default     | Default     |   471,328.497 ns |   8,553.6370 ns |  6,678.1171 ns | 10.737 |    0.25 |    6 |  8.3008 | 0.4883 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 2,463,464.435 ns |  39,589.5651 ns | 35,095.1183 ns | 56.117 |    1.32 |    7 | 39.0625 | 7.8125 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | DefaultJob | Default        | Default     | Default     |         5.034 ns |       0.1412 ns |      0.2436 ns |  0.000 |    0.00 |    2 |  0.0014 |      - |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | DefaultJob | Default        | Default     | Default     |         3.936 ns |       0.0982 ns |      0.1408 ns |  0.000 |    0.00 |    1 |       - |      - |         - |       0.000 |
| Registry.GetConfiguration                  | DefaultJob | Default        | Default     | Default     |         4.905 ns |       0.1367 ns |      0.1574 ns |  0.000 |    0.00 |    2 |       - |      - |         - |       0.000 |
| Registry.GetAllConfigurations              | DefaultJob | Default        | Default     | Default     |        72.438 ns |       0.9776 ns |      0.8667 ns |  0.002 |    0.00 |    4 |  0.0067 |      - |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | DefaultJob | Default        | Default     | Default     |         9.198 ns |       0.2262 ns |      0.2860 ns |  0.000 |    0.00 |    3 |       - |      - |         - |       0.000 |
|                                            |            |                |             |             |                  |                 |                |        |         |      |         |        |           |             |
| &#39;Hash 100 rows&#39;                            | ShortRun   | 3              | 1           | 3           |    44,088.762 ns |  14,085.4747 ns |    772.0724 ns |  1.000 |    0.02 |    5 |  0.8545 |      - |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | ShortRun   | 3              | 1           | 3           |   462,328.144 ns |  87,265.8712 ns |  4,783.3369 ns | 10.488 |    0.19 |    6 |  8.3008 | 0.4883 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | ShortRun   | 3              | 1           | 3           | 2,512,420.159 ns | 587,246.3022 ns | 32,188.9515 ns | 56.997 |    1.07 |    7 | 39.0625 | 7.8125 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | ShortRun   | 3              | 1           | 3           |         5.776 ns |      10.1247 ns |      0.5550 ns |  0.000 |    0.00 |    2 |  0.0014 |      - |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | ShortRun   | 3              | 1           | 3           |         4.180 ns |       1.8081 ns |      0.0991 ns |  0.000 |    0.00 |    1 |       - |      - |         - |       0.000 |
| Registry.GetConfiguration                  | ShortRun   | 3              | 1           | 3           |         4.097 ns |       0.2930 ns |      0.0161 ns |  0.000 |    0.00 |    1 |       - |      - |         - |       0.000 |
| Registry.GetAllConfigurations              | ShortRun   | 3              | 1           | 3           |        68.957 ns |      11.3012 ns |      0.6195 ns |  0.002 |    0.00 |    4 |  0.0067 |      - |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | ShortRun   | 3              | 1           | 3           |         9.648 ns |       2.3659 ns |      0.1297 ns |  0.000 |    0.00 |    3 |       - |      - |         - |       0.000 |
