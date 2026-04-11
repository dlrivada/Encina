```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean             | Error         | StdDev        | Median           | Ratio  | RatioSD | Rank | Gen0    | Allocated | Alloc Ratio |
|------------------------------------------- |----------- |--------------- |------------ |------------ |-----------------:|--------------:|--------------:|-----------------:|-------:|--------:|-----:|--------:|----------:|------------:|
| &#39;Hash 100 rows&#39;                            | DefaultJob | Default        | Default     | Default     |    75,157.847 ns |   409.6503 ns |   342.0765 ns |    75,083.726 ns |  1.000 |    0.01 |    6 |  0.8545 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | DefaultJob | Default        | Default     | Default     |   805,517.442 ns | 1,951.4693 ns | 1,729.9267 ns |   805,439.666 ns | 10.718 |    0.05 |    7 |  7.8125 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 4,355,641.987 ns | 9,217.0142 ns | 7,196.0384 ns | 4,357,731.125 ns | 57.954 |    0.27 |    8 | 39.0625 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | DefaultJob | Default        | Default     | Default     |        10.266 ns |     0.2588 ns |     0.2421 ns |        10.225 ns |  0.000 |    0.00 |    3 |  0.0014 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | DefaultJob | Default        | Default     | Default     |         7.239 ns |     0.0065 ns |     0.0051 ns |         7.238 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | DefaultJob | Default        | Default     | Default     |         8.613 ns |     0.2368 ns |     0.2432 ns |         8.783 ns |  0.000 |    0.00 |    2 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | DefaultJob | Default        | Default     | Default     |       134.740 ns |     0.9580 ns |     0.8961 ns |       135.056 ns |  0.002 |    0.00 |    5 |  0.0067 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | DefaultJob | Default        | Default     | Default     |        18.327 ns |     0.0286 ns |     0.0254 ns |        18.325 ns |  0.000 |    0.00 |    4 |       - |         - |       0.000 |
|                                            |            |                |             |             |                  |               |               |                  |        |         |      |         |           |             |
| &#39;Hash 100 rows&#39;                            | MediumRun  | 15             | 2           | 10          |    76,432.594 ns |   165.5323 ns |   226.5825 ns |    76,364.314 ns |  1.000 |    0.00 |    6 |  0.8545 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | MediumRun  | 15             | 2           | 10          |   813,396.664 ns | 1,791.9884 ns | 2,570.0164 ns |   812,480.280 ns | 10.642 |    0.05 |    7 |  7.8125 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | MediumRun  | 15             | 2           | 10          | 4,390,481.008 ns | 4,642.5848 ns | 6,805.0412 ns | 4,390,645.055 ns | 57.443 |    0.19 |    8 | 39.0625 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | MediumRun  | 15             | 2           | 10          |        10.130 ns |     0.1773 ns |     0.2542 ns |        10.107 ns |  0.000 |    0.00 |    3 |  0.0014 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | MediumRun  | 15             | 2           | 10          |         7.209 ns |     0.0149 ns |     0.0213 ns |         7.206 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | MediumRun  | 15             | 2           | 10          |         8.494 ns |     0.1912 ns |     0.2802 ns |         8.432 ns |  0.000 |    0.00 |    2 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | MediumRun  | 15             | 2           | 10          |       143.764 ns |     0.9123 ns |     1.3654 ns |       143.206 ns |  0.002 |    0.00 |    5 |  0.0067 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | MediumRun  | 15             | 2           | 10          |        16.356 ns |     0.1488 ns |     0.2134 ns |        16.236 ns |  0.000 |    0.00 |    4 |       - |         - |       0.000 |
