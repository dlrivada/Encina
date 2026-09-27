```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean             | Error           | StdDev        | Median           | Ratio  | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |----------- |--------------- |------------ |------------ |-----------------:|----------------:|--------------:|-----------------:|-------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Hash 100 rows&#39;                            | DefaultJob | Default        | Default     | Default     |    56,761.153 ns |      99.1681 ns |    87.9099 ns |    56,796.569 ns |  1.000 |    0.00 |    6 |  0.8545 |      - |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | DefaultJob | Default        | Default     | Default     |   601,044.345 ns |   1,648.7380 ns | 1,376.7709 ns |   600,871.771 ns | 10.589 |    0.03 |    7 |  7.8125 |      - |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 3,248,125.995 ns |  10,255.5252 ns | 9,593.0246 ns | 3,247,580.176 ns | 57.225 |    0.18 |    8 | 39.0625 | 7.8125 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | DefaultJob | Default        | Default     | Default     |         8.750 ns |       0.2292 ns |     0.2452 ns |         8.718 ns |  0.000 |    0.00 |    3 |  0.0014 |      - |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | DefaultJob | Default        | Default     | Default     |         5.726 ns |       0.0386 ns |     0.0322 ns |         5.735 ns |  0.000 |    0.00 |    1 |       - |      - |         - |       0.000 |
| Registry.GetConfiguration                  | DefaultJob | Default        | Default     | Default     |         6.935 ns |       0.1548 ns |     0.1448 ns |         7.059 ns |  0.000 |    0.00 |    2 |       - |      - |         - |       0.000 |
| Registry.GetAllConfigurations              | DefaultJob | Default        | Default     | Default     |       106.340 ns |       0.4419 ns |     0.3917 ns |       106.377 ns |  0.002 |    0.00 |    5 |  0.0067 |      - |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | DefaultJob | Default        | Default     | Default     |        12.249 ns |       0.1170 ns |     0.1095 ns |        12.299 ns |  0.000 |    0.00 |    4 |       - |      - |         - |       0.000 |
|                                            |            |                |             |             |                  |                 |               |                  |        |         |      |         |        |           |             |
| &#39;Hash 100 rows&#39;                            | ShortRun   | 3              | 1           | 3           |    58,077.558 ns |  13,785.6427 ns |   755.6376 ns |    57,662.261 ns |  1.000 |    0.02 |    5 |  0.8545 |      - |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | ShortRun   | 3              | 1           | 3           |   613,457.918 ns |  18,718.0359 ns | 1,025.9987 ns |   613,915.975 ns | 10.564 |    0.12 |    6 |  7.8125 |      - |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | ShortRun   | 3              | 1           | 3           | 3,336,496.591 ns | 167,940.1716 ns | 9,205.3675 ns | 3,341,091.207 ns | 57.455 |    0.66 |    7 | 39.0625 | 7.8125 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | ShortRun   | 3              | 1           | 3           |         8.371 ns |       6.1282 ns |     0.3359 ns |         8.372 ns |  0.000 |    0.00 |    2 |  0.0014 |      - |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | ShortRun   | 3              | 1           | 3           |         5.739 ns |       0.1967 ns |     0.0108 ns |         5.736 ns |  0.000 |    0.00 |    1 |       - |      - |         - |       0.000 |
| Registry.GetConfiguration                  | ShortRun   | 3              | 1           | 3           |         7.083 ns |       0.0611 ns |     0.0034 ns |         7.083 ns |  0.000 |    0.00 |    2 |       - |      - |         - |       0.000 |
| Registry.GetAllConfigurations              | ShortRun   | 3              | 1           | 3           |       110.267 ns |      25.6176 ns |     1.4042 ns |       110.918 ns |  0.002 |    0.00 |    4 |  0.0067 |      - |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | ShortRun   | 3              | 1           | 3           |        12.300 ns |       0.0759 ns |     0.0042 ns |        12.302 ns |  0.000 |    0.00 |    3 |       - |      - |         - |       0.000 |
